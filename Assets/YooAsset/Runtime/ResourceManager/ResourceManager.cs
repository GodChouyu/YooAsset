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
        /// <summary>
        /// 资源提供者字典，Key 为 ProviderGUID
        /// </summary>
        internal readonly Dictionary<string, ProviderBase> ProviderDict = new Dictionary<string, ProviderBase>(5000);

        /// <summary>
        /// 资源包加载器字典，Key 为 BundleName
        /// </summary>
        internal readonly Dictionary<string, LoadBundleOperation> BundleLoaderDict = new Dictionary<string, LoadBundleOperation>(5000);

        /// <summary>
        /// 已加载的场景句柄列表
        /// </summary>
        internal readonly List<SceneHandle> SceneHandles = new List<SceneHandle>(100);

        private readonly List<SceneHandle> _tempSceneHandles = new List<SceneHandle>(100);
        private FileSystemHost _fileSystemHost;
        private int _bundleLoadingMaxConcurrency;
        private int _bundleLoadingCounter;
        private long _sceneInstanceCounter;

        /// <summary>
        /// 当资源句柄引用计数为零时，是否自动卸载对应的资源包
        /// </summary>
        public bool AutoUnloadBundleWhenUnused { get; private set; }

        /// <summary>
        /// WebGL 平台是否强制同步加载资源
        /// </summary>
        public bool WebGLForceSyncLoadAsset { get; private set; }

        /// <summary>
        /// 所属包裹
        /// </summary>
        public readonly string PackageName;

        /// <summary>
        /// 锁定加载操作
        /// </summary>
        public bool LockLoadOperation = false;


        /// <summary>
        /// 创建资源管理器实例
        /// </summary>
        /// <param name="packageName">资源包名称</param>
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
                var mainLoader = TryGetBundleLoader(mainBundleName);
                if (mainLoader != null)
                {
                    mainLoader.TryDestroyProviders();
                    if (mainLoader.CanDestroyLoader())
                    {
                        mainLoader.DestroyLoader();
                        BundleLoaderDict.Remove(mainBundleName);
                        hasUnloaded = true;
                    }
                }

                // 卸载依赖资源包加载器
                foreach (var dependID in assetInfo.Asset.DependentBundleIDs)
                {
                    string dependBundleName = _fileSystemHost.GetMainBundleName(dependID);
                    var dependLoader = TryGetBundleLoader(dependBundleName);
                    if (dependLoader != null)
                    {
                        if (dependLoader.CanDestroyLoader())
                        {
                            dependLoader.DestroyLoader();
                            BundleLoaderDict.Remove(dependBundleName);
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
                string error = $"The load operation is locked.";
                YooLogger.Error(error);
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(error);
                return errorProvider.CreateHandle<SceneHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load scene. Error: {assetInfo.Error}");
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(assetInfo.Error);
                return errorProvider.CreateHandle<SceneHandle>();
            }

            // 注意：同一个场景的ProviderGUID每次加载都会变化
            string providerGUID = $"{assetInfo.GUID}-{++_sceneInstanceCounter}";
            ProviderBase provider;
            {
                provider = new SceneProvider(this, providerGUID, assetInfo, loadSceneParams, suspendLoad);
                provider.InitProviderDebugInfo();
                ProviderDict.Add(providerGUID, provider);
                AsyncOperationSystem.StartOperation(PackageName, provider);
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
                string error = $"The load operation is locked.";
                YooLogger.Error(error);
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(error);
                return errorProvider.CreateHandle<AssetHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load asset. Error: {assetInfo.Error}");
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(assetInfo.Error);
                return errorProvider.CreateHandle<AssetHandle>();
            }

            string providerGUID = nameof(LoadAssetAsync) + assetInfo.GUID;
            ProviderBase provider = TryGetAssetProvider(providerGUID);
            if (provider == null)
            {
                provider = new AssetProvider(this, providerGUID, assetInfo);
                provider.InitProviderDebugInfo();
                ProviderDict.Add(providerGUID, provider);
                AsyncOperationSystem.StartOperation(PackageName, provider);
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
                string error = $"The load operation is locked.";
                YooLogger.Error(error);
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(error);
                return errorProvider.CreateHandle<SubAssetsHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load sub assets. Error: {assetInfo.Error}");
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(assetInfo.Error);
                return errorProvider.CreateHandle<SubAssetsHandle>();
            }

            string providerGUID = nameof(LoadSubAssetsAsync) + assetInfo.GUID;
            ProviderBase provider = TryGetAssetProvider(providerGUID);
            if (provider == null)
            {
                provider = new SubAssetsProvider(this, providerGUID, assetInfo);
                provider.InitProviderDebugInfo();
                ProviderDict.Add(providerGUID, provider);
                AsyncOperationSystem.StartOperation(PackageName, provider);
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
                string error = $"The load operation is locked.";
                YooLogger.Error(error);
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(error);
                return errorProvider.CreateHandle<AllAssetsHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load all assets. Error: {assetInfo.Error}");
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(assetInfo.Error);
                return errorProvider.CreateHandle<AllAssetsHandle>();
            }

            string providerGUID = nameof(LoadAllAssetsAsync) + assetInfo.GUID;
            ProviderBase provider = TryGetAssetProvider(providerGUID);
            if (provider == null)
            {
                provider = new AllAssetsProvider(this, providerGUID, assetInfo);
                provider.InitProviderDebugInfo();
                ProviderDict.Add(providerGUID, provider);
                AsyncOperationSystem.StartOperation(PackageName, provider);
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
                string error = $"The load operation is locked.";
                YooLogger.Error(error);
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(error);
                return errorProvider.CreateHandle<RawFileHandle>();
            }

            if (assetInfo.IsInvalid)
            {
                YooLogger.Error($"Failed to load raw file. Error: {assetInfo.Error}");
                ErrorProvider errorProvider = new ErrorProvider(this, assetInfo);
                errorProvider.SetCompletedWithError(assetInfo.Error);
                return errorProvider.CreateHandle<RawFileHandle>();
            }

            string providerGUID = nameof(LoadRawFileAsync) + assetInfo.GUID;
            ProviderBase provider = TryGetAssetProvider(providerGUID);
            if (provider == null)
            {
                provider = new RawFileProvider(this, providerGUID, assetInfo);
                provider.InitProviderDebugInfo();
                ProviderDict.Add(providerGUID, provider);
                AsyncOperationSystem.StartOperation(PackageName, provider);
            }

            provider.Priority = priority;
            return provider.CreateHandle<RawFileHandle>();
        }

        /// <summary>
        /// 获取或创建主资源包加载器
        /// </summary>
        internal LoadBundleOperation GetOrCreateMainBundleLoader(AssetInfo assetInfo)
        {
            BundleInfo bundleInfo = _fileSystemHost.GetMainBundleInfo(assetInfo);
            return GetOrCreateBundleLoader(bundleInfo);
        }

        /// <summary>
        /// 获取或创建依赖资源包加载器列表
        /// </summary>
        internal List<LoadBundleOperation> GetOrCreateDependBundleLoaders(AssetInfo assetInfo)
        {
            List<BundleInfo> bundleInfos = _fileSystemHost.GetDependBundleInfos(assetInfo);
            List<LoadBundleOperation> result = new List<LoadBundleOperation>(bundleInfos.Count);
            foreach (var bundleInfo in bundleInfos)
            {
                var bundleLoader = GetOrCreateBundleLoader(bundleInfo);
                result.Add(bundleLoader);
            }
            return result;
        }

        /// <summary>
        /// 从字典中移除指定的资源提供者
        /// </summary>
        internal void RemoveBundleProviders(List<ProviderBase> removeList)
        {
            foreach (var provider in removeList)
            {
                ProviderDict.Remove(provider.ProviderGUID);
            }
        }

        /// <summary>
        /// 检查指定资源包是否已销毁
        /// </summary>
        internal bool CheckBundleDestroyed(int bundleID)
        {
            string bundleName = _fileSystemHost.GetMainBundleName(bundleID);
            var bundleFileLoader = TryGetBundleLoader(bundleName);
            if (bundleFileLoader == null)
                return true;
            return bundleFileLoader.IsDestroyed;
        }

        /// <summary>
        /// 检查指定资源包是否可以释放
        /// </summary>
        internal bool CheckBundleReleasable(int bundleID)
        {
            string bundleName = _fileSystemHost.GetMainBundleName(bundleID);
            var bundleFileLoader = TryGetBundleLoader(bundleName);
            if (bundleFileLoader == null)
                return true;
            return bundleFileLoader.IsReleasable();
        }

        /// <summary>
        /// 检查是否存在任何资源包加载器
        /// </summary>
        internal bool HasAnyLoader()
        {
            return BundleLoaderDict.Count > 0;
        }

        /// <summary>
        /// 增加资源包加载计数器
        /// </summary>
        internal void IncrementBundleLoadingCounter()
        {
            _bundleLoadingCounter++;
        }

        /// <summary>
        /// 减少资源包加载计数器
        /// </summary>
        internal void DecrementBundleLoadingCounter()
        {
            _bundleLoadingCounter--;
            if (_bundleLoadingCounter < 0)
            {
                YooLogger.Error("BundleLoadingCounter is negative.");
                _bundleLoadingCounter = 0;
            }
        }

        /// <summary>
        /// 获取当前资源包加载计数
        /// </summary>
        internal int GetBundleLoadingCounter()
        {
            return _bundleLoadingCounter;
        }

        /// <summary>
        /// 检查资源包加载是否繁忙（达到并发上限）
        /// </summary>
        internal bool IsBundleLoadingBusy()
        {
            return _bundleLoadingCounter >= _bundleLoadingMaxConcurrency;
        }

        private LoadBundleOperation GetOrCreateBundleLoader(BundleInfo bundleInfo)
        {
            // 如果加载器已经存在
            string bundleName = bundleInfo.Bundle.BundleName;
            LoadBundleOperation loaderOperation = TryGetBundleLoader(bundleName);
            if (loaderOperation != null)
                return loaderOperation;

            // 新增下载需求
            loaderOperation = new LoadBundleOperation(this, bundleInfo);
            BundleLoaderDict.Add(bundleName, loaderOperation);
            return loaderOperation;
        }
        private LoadBundleOperation TryGetBundleLoader(string bundleName)
        {
            if (BundleLoaderDict.TryGetValue(bundleName, out LoadBundleOperation value))
                return value;
            else
                return null;
        }
        private ProviderBase TryGetAssetProvider(string providerGUID)
        {
            if (ProviderDict.TryGetValue(providerGUID, out ProviderBase value))
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
        /// <summary>
        /// 获取所有资源提供者的调试信息
        /// </summary>
        internal List<DiagnosticProviderInfo> GetDebugProviderInfos()
        {
            List<DiagnosticProviderInfo> result = new List<DiagnosticProviderInfo>(ProviderDict.Count);
            foreach (var provider in ProviderDict.Values)
            {
                DiagnosticProviderInfo providerInfo = new DiagnosticProviderInfo();
                providerInfo.AssetPath = provider.MainAssetInfo.AssetPath;
                providerInfo.SpawnScene = provider.SpawnScene;
                providerInfo.StartTime = provider.StartTime;
                providerInfo.ElapsedMilliseconds = provider.ElapsedMilliseconds;
                providerInfo.ReferenceCount = provider.RefCount;
                providerInfo.Status = provider.Status.ToString();
                providerInfo.Dependencies = provider.GetDebugDependBundles();
                result.Add(providerInfo);
            }
            return result;
        }

        /// <summary>
        /// 获取所有资源包加载器的调试信息
        /// </summary>
        internal List<DiagnosticBundleInfo> GetDebugBundleInfos()
        {
            List<DiagnosticBundleInfo> result = new List<DiagnosticBundleInfo>(BundleLoaderDict.Values.Count);
            foreach (var bundleLoader in BundleLoaderDict.Values)
            {
                var packageBundle = bundleLoader.LoadBundleInfo.Bundle;
                var bundleInfo = new DiagnosticBundleInfo();
                bundleInfo.BundleName = packageBundle.BundleName;
                bundleInfo.ReferenceCount = bundleLoader.RefCount;
                bundleInfo.Status = bundleLoader.Status.ToString();
                bundleInfo.Referencers = FilterReferenceBundles(packageBundle);
                result.Add(bundleInfo);
            }
            return result;
        }

        /// <summary>
        /// 过滤出当前已加载的引用资源包
        /// </summary>
        internal List<string> FilterReferenceBundles(PackageBundle packageBundle)
        {
            // 注意：引用的资源包不一定在内存中，所以需要过滤
            var referenceBundles = packageBundle.GetDebugReferenceBundles();
            List<string> result = new List<string>(referenceBundles.Count);
            foreach (var bundleName in referenceBundles)
            {
                if (BundleLoaderDict.ContainsKey(bundleName))
                    result.Add(bundleName);
            }
            return result;
        }
        #endregion
    }
}