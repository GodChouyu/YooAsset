using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YooAsset
{
    /// <summary>
    /// 资源管理器
    /// 注意：此类不是线程安全的，所有方法必须在Unity主线程调用
    /// </summary>
    internal class ResourceManager
    {
        internal readonly Dictionary<string, ProviderOperation> ProviderDic = new Dictionary<string, ProviderOperation>(5000);
        internal readonly Dictionary<string, LoadBundleOperation> LoaderDic = new Dictionary<string, LoadBundleOperation>(5000);
        internal readonly List<SceneHandle> SceneHandles = new List<SceneHandle>(100);
        private readonly List<SceneHandle> _tempSceneHandles = new List<SceneHandle>(100);
        private FileSystemHost _fileSystemHost;
        private int _bundleLoadingMaxConcurrency;
        private int _bundleLoadingCounter;
        private long _sceneCreateIndex;

        // 开发者配置选项
        public bool AutoUnloadBundleWhenUnused { get; private set; }
        public bool WebGLForceSyncLoadAsset { get; private set; }

        /// <summary>
        /// 所属包裹
        /// </summary>
        public readonly string PackageName;

        /// <summary>
        /// 锁定加载操作
        /// </summary>
        public bool LockLoadOperation = false;

        public ResourceManager(string packageName)
        {
            PackageName = packageName;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Initialize(InitializePackageOptions options, FileSystemHost host)
        {
            _fileSystemHost = host;
            _bundleLoadingMaxConcurrency = options.BundleLoadingMaxConcurrency;
            AutoUnloadBundleWhenUnused = options.AutoUnloadBundleWhenUnused;
            WebGLForceSyncLoadAsset = options.WebGLForceSyncLoadAsset;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        /// <summary>
        /// 销毁管理器
        /// </summary>
        public void Destroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        /// <summary>
        /// 尝试卸载指定资源的资源包（包括依赖资源）
        /// </summary>
        public void TryUnloadUnusedAsset(AssetInfo assetInfo, int loopCount)
        {
            if (assetInfo == null)
            {
                YooLogger.Error($"{nameof(AssetInfo)} is null.");
                return;
            }
            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to unload asset. Error: {assetInfo.Error}");
                return;
            }

            // 多次循环尝试卸载，以处理复杂的依赖链
            // 例如：A依赖B，B依赖C，需要多次循环才能完全卸载
            while (loopCount > 0)
            {
                loopCount--;
                bool hasUnloaded = false;

                // 卸载主资源包加载器
                string mainBundleName = _fileSystemHost.GetMainBundleName(assetInfo.Asset.BundleID);
                var mainLoader = TryGetBundleFileLoader(mainBundleName);
                if (mainLoader != null)
                {
                    mainLoader.TryDestroyProviders();
                    if (mainLoader.CanDestroyLoader())
                    {
                        mainLoader.DestroyLoader();
                        LoaderDic.Remove(mainBundleName);
                        hasUnloaded = true;
                    }
                }

                // 卸载依赖资源包加载器
                foreach (var dependID in assetInfo.Asset.DependBundleIDs)
                {
                    string dependBundleName = _fileSystemHost.GetMainBundleName(dependID);
                    var dependLoader = TryGetBundleFileLoader(dependBundleName);
                    if (dependLoader != null)
                    {
                        if (dependLoader.CanDestroyLoader())
                        {
                            dependLoader.DestroyLoader();
                            LoaderDic.Remove(dependBundleName);
                            hasUnloaded = true;
                        }
                    }
                }

                // 如果本次循环没有卸载任何资源，提前退出
                if (hasUnloaded == false)
                    break;
            }
        }

        /// <summary>
        /// 加载场景对象
        /// 注意：返回的场景句柄是唯一的，每个场景句柄对应自己的场景提供者对象。
        /// 注意：业务逻辑层应该避免同时加载一个子场景。
        /// </summary>
        public SceneHandle LoadSceneAsync(AssetInfo assetInfo, LoadSceneParameters loadSceneParams, bool suspendLoad, uint priority)
        {
            if (LockLoadOperation)
            {
                string error = $"The load operation locked.";
                YooLogger.Error(error);
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(error);
                return completedProvider.CreateHandle<SceneHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load scene. Error: {assetInfo.Error}");
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(assetInfo.Error);
                return completedProvider.CreateHandle<SceneHandle>();
            }

            // 注意：同一个场景的ProviderGUID每次加载都会变化
            string providerGUID = $"{assetInfo.GUID}-{++_sceneCreateIndex}";
            ProviderOperation provider;
            {
                provider = new SceneProvider(this, providerGUID, assetInfo, loadSceneParams, suspendLoad);
                provider.InitProviderDebugInfo();
                ProviderDic.Add(providerGUID, provider);
                OperationSystem.StartOperation(PackageName, provider);
            }

            provider.Priority = priority;
            var handle = provider.CreateHandle<SceneHandle>();
            handle.PackageName = PackageName;
            SceneHandles.Add(handle);
            return handle;
        }

        /// <summary>
        /// 加载资源对象
        /// </summary>
        public AssetHandle LoadAssetAsync(AssetInfo assetInfo, uint priority)
        {
            if (LockLoadOperation)
            {
                string error = $"The load operation locked.";
                YooLogger.Error(error);
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(error);
                return completedProvider.CreateHandle<AssetHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load asset. Error: {assetInfo.Error}");
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(assetInfo.Error);
                return completedProvider.CreateHandle<AssetHandle>();
            }

            string providerGUID = nameof(LoadAssetAsync) + assetInfo.GUID;
            ProviderOperation provider = TryGetAssetProvider(providerGUID);
            if (provider == null)
            {
                provider = new AssetProvider(this, providerGUID, assetInfo);
                provider.InitProviderDebugInfo();
                ProviderDic.Add(providerGUID, provider);
                OperationSystem.StartOperation(PackageName, provider);
            }

            provider.Priority = priority;
            return provider.CreateHandle<AssetHandle>();
        }

        /// <summary>
        /// 加载子资源对象
        /// </summary>
        public SubAssetsHandle LoadSubAssetsAsync(AssetInfo assetInfo, uint priority)
        {
            if (LockLoadOperation)
            {
                string error = $"The load operation locked.";
                YooLogger.Error(error);
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(error);
                return completedProvider.CreateHandle<SubAssetsHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load sub assets. Error: {assetInfo.Error}");
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(assetInfo.Error);
                return completedProvider.CreateHandle<SubAssetsHandle>();
            }

            string providerGUID = nameof(LoadSubAssetsAsync) + assetInfo.GUID;
            ProviderOperation provider = TryGetAssetProvider(providerGUID);
            if (provider == null)
            {
                provider = new SubAssetsProvider(this, providerGUID, assetInfo);
                provider.InitProviderDebugInfo();
                ProviderDic.Add(providerGUID, provider);
                OperationSystem.StartOperation(PackageName, provider);
            }

            provider.Priority = priority;
            return provider.CreateHandle<SubAssetsHandle>();
        }

        /// <summary>
        /// 加载所有资源对象
        /// </summary>
        public AllAssetsHandle LoadAllAssetsAsync(AssetInfo assetInfo, uint priority)
        {
            if (LockLoadOperation)
            {
                string error = $"The load operation locked.";
                YooLogger.Error(error);
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(error);
                return completedProvider.CreateHandle<AllAssetsHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load all assets. Error: {assetInfo.Error}");
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(assetInfo.Error);
                return completedProvider.CreateHandle<AllAssetsHandle>();
            }

            string providerGUID = nameof(LoadAllAssetsAsync) + assetInfo.GUID;
            ProviderOperation provider = TryGetAssetProvider(providerGUID);
            if (provider == null)
            {
                provider = new AllAssetsProvider(this, providerGUID, assetInfo);
                provider.InitProviderDebugInfo();
                ProviderDic.Add(providerGUID, provider);
                OperationSystem.StartOperation(PackageName, provider);
            }

            provider.Priority = priority;
            return provider.CreateHandle<AllAssetsHandle>();
        }

        /// <summary>
        /// 加载原生文件
        /// </summary>
        public RawFileHandle LoadRawFileAsync(AssetInfo assetInfo, uint priority)
        {
            if (LockLoadOperation)
            {
                string error = $"The load operation locked.";
                YooLogger.Error(error);
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(error);
                return completedProvider.CreateHandle<RawFileHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load raw file. Error: {assetInfo.Error}");
                CompletedProvider completedProvider = new CompletedProvider(this, assetInfo);
                completedProvider.SetCompletedWithError(assetInfo.Error);
                return completedProvider.CreateHandle<RawFileHandle>();
            }

            string providerGUID = nameof(LoadRawFileAsync) + assetInfo.GUID;
            ProviderOperation provider = TryGetAssetProvider(providerGUID);
            if (provider == null)
            {
                provider = new RawFileProvider(this, providerGUID, assetInfo);
                provider.InitProviderDebugInfo();
                ProviderDic.Add(providerGUID, provider);
                OperationSystem.StartOperation(PackageName, provider);
            }

            provider.Priority = priority;
            return provider.CreateHandle<RawFileHandle>();
        }

        internal LoadBundleOperation CreateMainBundleFileLoader(AssetInfo assetInfo)
        {
            BundleInfo bundleInfo = _fileSystemHost.GetMainBundleInfo(assetInfo);
            return CreateBundleFileLoaderInternal(bundleInfo);
        }
        internal List<LoadBundleOperation> CreateDependBundleFileLoaders(AssetInfo assetInfo)
        {
            List<BundleInfo> bundleInfos = _fileSystemHost.GetDependBundleInfos(assetInfo);
            List<LoadBundleOperation> result = new List<LoadBundleOperation>(bundleInfos.Count);
            foreach (var bundleInfo in bundleInfos)
            {
                var bundleLoader = CreateBundleFileLoaderInternal(bundleInfo);
                result.Add(bundleLoader);
            }
            return result;
        }
        internal void RemoveBundleProviders(List<ProviderOperation> removeList)
        {
            foreach (var provider in removeList)
            {
                ProviderDic.Remove(provider.ProviderGUID);
            }
        }
        internal bool CheckBundleDestroyed(int bundleID)
        {
            string bundleName = _fileSystemHost.GetMainBundleName(bundleID);
            var bundleFileLoader = TryGetBundleFileLoader(bundleName);
            if (bundleFileLoader == null)
                return true;
            return bundleFileLoader.IsDestroyed;
        }
        internal bool CheckBundleReleasable(int bundleID)
        {
            string bundleName = _fileSystemHost.GetMainBundleName(bundleID);
            var bundleFileLoader = TryGetBundleFileLoader(bundleName);
            if (bundleFileLoader == null)
                return true;
            return bundleFileLoader.CanReleasableLoader();
        }
        internal bool HasAnyLoader()
        {
            return LoaderDic.Count > 0;
        }
        internal void IncrementBundleLoadingCounter()
        {
            _bundleLoadingCounter++;
        }
        internal void DecrementBundleLoadingCounter()
        {
            _bundleLoadingCounter--;
            if (_bundleLoadingCounter < 0)
            {
                YooLogger.Error("BundleLoadingCounter is negative.");
                _bundleLoadingCounter = 0;
            }
        }
        internal int GetBundleLoadingCounter()
        {
            return _bundleLoadingCounter;
        }
        internal bool BundleLoadingIsBusy()
        {
            return _bundleLoadingCounter >= _bundleLoadingMaxConcurrency;
        }

        private LoadBundleOperation CreateBundleFileLoaderInternal(BundleInfo bundleInfo)
        {
            // 如果加载器已经存在
            string bundleName = bundleInfo.Bundle.BundleName;
            LoadBundleOperation loaderOperation = TryGetBundleFileLoader(bundleName);
            if (loaderOperation != null)
                return loaderOperation;

            // 新增下载需求
            loaderOperation = new LoadBundleOperation(this, bundleInfo);
            LoaderDic.Add(bundleName, loaderOperation);
            return loaderOperation;
        }
        private LoadBundleOperation TryGetBundleFileLoader(string bundleName)
        {
            if (LoaderDic.TryGetValue(bundleName, out LoadBundleOperation value))
                return value;
            else
                return null;
        }
        private ProviderOperation TryGetAssetProvider(string providerGUID)
        {
            if (ProviderDic.TryGetValue(providerGUID, out ProviderOperation value))
                return value;
            else
                return null;
        }
        private void OnSceneUnloaded(Scene scene)
        {
            _tempSceneHandles.Clear(); //复用列表
            foreach (var sceneHandle in SceneHandles)
            {
                if (sceneHandle.IsValid)
                {
                    if (sceneHandle.SceneObject == scene)
                    {
                        sceneHandle.Release();
                        _tempSceneHandles.Add(sceneHandle);
                    }
                }
            }
            foreach (var sceneHandle in _tempSceneHandles)
            {
                SceneHandles.Remove(sceneHandle);
            }
        }

        #region 调试信息
        internal List<DiagnosticProviderInfo> GetDebugProviderInfos()
        {
            List<DiagnosticProviderInfo> result = new List<DiagnosticProviderInfo>(ProviderDic.Count);
            foreach (var provider in ProviderDic.Values)
            {
                DiagnosticProviderInfo providerInfo = new DiagnosticProviderInfo();
                providerInfo.AssetPath = provider.MainAssetInfo.AssetPath;
                providerInfo.OriginScene = provider.OriginScene;
                providerInfo.StartTime = provider.StartTime;
                providerInfo.ElapsedMS = provider.ElapsedMS;
                providerInfo.ReferenceCount = provider.RefCount;
                providerInfo.Status = provider.Status.ToString();
                providerInfo.DependentBundles = provider.GetDebugDependBundles();
                result.Add(providerInfo);
            }
            return result;
        }
        internal List<DiagnosticBundleInfo> GetDebugBundleInfos()
        {
            List<DiagnosticBundleInfo> result = new List<DiagnosticBundleInfo>(LoaderDic.Values.Count);
            foreach (var bundleLoader in LoaderDic.Values)
            {
                var packageBundle = bundleLoader.LoadBundleInfo.Bundle;
                var bundleInfo = new DiagnosticBundleInfo();
                bundleInfo.BundleName = packageBundle.BundleName;
                bundleInfo.ReferenceCount = bundleLoader.RefCount;
                bundleInfo.Status = bundleLoader.Status.ToString();
                bundleInfo.ReferencedByBundles = FilterReferenceBundles(packageBundle);
                result.Add(bundleInfo);
            }
            return result;
        }
        internal List<string> FilterReferenceBundles(PackageBundle packageBundle)
        {
            // 注意：引用的资源包不一定在内存中，所以需要过滤
            var referenceBundles = packageBundle.GetDebugReferenceBundles();
            List<string> result = new List<string>(referenceBundles.Count);
            foreach (var bundleName in referenceBundles)
            {
                if (LoaderDic.ContainsKey(bundleName))
                    result.Add(bundleName);
            }
            return result;
        }
        #endregion
    }
}