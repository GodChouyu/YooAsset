using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace YooAsset
{
    public class ResourcePackage
    {
        private InitializePackageOperation _initializeOp;
        private ResourceManager _resourceManager;
        private FileSystemHost _fileSystemHost;

        /// <summary>
        /// 包裹名
        /// </summary>
        public readonly string PackageName;

        /// <summary>
        /// 初始化状态
        /// </summary>
        public EOperationStatus InitializeStatus
        {
            get
            {
                if (_initializeOp == null)
                    return EOperationStatus.None;
                return _initializeOp.Status;
            }
        }

        /// <summary>
        /// 包裹是否有效
        /// </summary>
        public bool PackageValid
        {
            get
            {
                if (_fileSystemHost == null)
                    return false;
                return _fileSystemHost.ActiveManifest != null;
            }
        }

        /// <summary>
        /// 包裹优先级（值越大越优先更新）
        /// </summary>
        public uint PackagePriority
        {
            get { return OperationSystem.GetSchedulerPriority(PackageName); }
            set { OperationSystem.SetSchedulerPriority(PackageName, value); }
        }

        
        internal ResourcePackage(string packageName)
        {
            PackageName = packageName;
        }
        internal void InternalInitialize(ResourceManager manager, FileSystemHost host)
        {
            _resourceManager = manager;
            _fileSystemHost = host;
        }
        internal void InternalDestroy()
        {
            _initializeOp = null;

            // 销毁资源管理器
            if (_resourceManager != null)
            {
                _resourceManager.Destroy();
                _resourceManager = null;
            }

            // 销毁文件系统中枢
            if (_fileSystemHost != null)
            {
                _fileSystemHost.Destroy();
                _fileSystemHost = null;
            }
        }

        /// <summary>
        /// 异步初始化包裹
        /// </summary>
        public InitializePackageOperation InitializePackageAsync(InitializePackageOptions options)
        {
            // 注意：联机平台因为网络原因可能会初始化失败！
            ResetInitializeAfterFailed();

            // 检测重复初始化
            if (_initializeOp != null)
                throw new YooPackageException(PackageName, $"Resource package '{PackageName}' is already initialized.");

            // 开始初始化操作
            _initializeOp = new InitializePackageOperation(this, options);
            OperationSystem.StartOperation(PackageName, _initializeOp);
            return _initializeOp;
        }
        private void ResetInitializeAfterFailed()
        {
            if (InitializeStatus == EOperationStatus.Failed)
            {
                InternalDestroy();
            }
        }

        /// <summary>
        /// 异步销毁包裹
        /// </summary>
        public DestroyPackageOperation DestroyPackageAsync()
        {
            var options = new UnloadAllAssetsOptions(true, true);
            var operation = new DestroyPackageOperation(this, options);
            OperationSystem.StartOperation(OperationSystem.GlobalSchedulerName, operation);
            return operation;
        }

        /// <summary>
        /// 请求最新的资源版本
        /// 说明：超时时间默认60秒
        /// </summary>
        public RequestVersionOperation RequestVersionAsync()
        {
            int defaultTimeout = 60;
            var options = new RequestVersionOptions(true, defaultTimeout);
            return RequestVersionAsync(options);
        }

        /// <summary>
        /// 请求最新的资源版本
        /// </summary>
        public RequestVersionOperation RequestVersionAsync(RequestVersionOptions options)
        {
            EnsureInitialized(false);
            var operation = new RequestVersionOperation(_fileSystemHost, options);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        /// <summary>
        /// 加载指定版本的资源清单
        /// </summary>
        public LoadManifestOperation LoadManifestAsync(LoadManifestOptions options)
        {
            EnsureInitialized(false);

            // 注意：强烈建议在更新之前保持加载器为空！
            if (_resourceManager.HasAnyLoader())
            {
                YooLogger.Warning($"Found loaded bundle before update manifest. Recommended to call the {nameof(UnloadAllAssetsAsync)} method to release loaded bundle.");
            }

            var operation = new LoadManifestOperation(_fileSystemHost, options);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        /// <summary>
        /// 预下载指定版本的包裹资源
        /// </summary>
        public PreDownloaderOperation PreDownloaderAsync(PreDownloaderOptions options)
        {
            EnsureInitialized(false);
            var operation = new PreDownloaderOperation(_fileSystemHost, options);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }
        
        /// <summary>
        /// 清理缓存文件
        /// </summary>
        public ClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            EnsureInitialized(false);
            options.Manifest = _fileSystemHost.ActiveManifest;
            var operation = new ClearCacheOperation(_fileSystemHost, options);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }


        #region 包裹信息
        /// <summary>
        /// 获取当前加载包裹的版本信息
        /// </summary>
        public string GetPackageVersion()
        {
            EnsureInitialized();
            return _fileSystemHost.ActiveManifest.PackageVersion;
        }

        /// <summary>
        /// 获取当前加载包裹的备注信息
        /// </summary>
        public string GetPackageNote()
        {
            EnsureInitialized();
            return _fileSystemHost.ActiveManifest.PackageNote;
        }

        /// <summary>
        /// 获取当前加载包裹的详细信息
        /// </summary>
        public PackageDetails GetPackageDetails()
        {
            EnsureInitialized();
            return _fileSystemHost.ActiveManifest.GetPackageDetails();
        }
        #endregion

        #region 资源回收
        /// <summary>
        /// 强制回收所有资源
        /// </summary>
        public UnloadAllAssetsOperation UnloadAllAssetsAsync()
        {
            var options = new UnloadAllAssetsOptions(true, true);
            return UnloadAllAssetsAsync(options);
        }

        /// <summary>
        /// 强制回收所有资源
        /// </summary>
        /// <param name="options">卸载选项</param>
        public UnloadAllAssetsOperation UnloadAllAssetsAsync(UnloadAllAssetsOptions options)
        {
            EnsureInitialized();
            var operation = new UnloadAllAssetsOperation(_resourceManager, options);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        /// <summary>
        /// 回收不再使用的资源
        /// 说明：卸载引用计数为零的资源
        /// 说明：默认循环10次
        /// </summary>
        public UnloadUnusedAssetsOperation UnloadUnusedAssetsAsync()
        {
            int defaultLoopCount = 10;
            var options = new UnloadUnusedAssetsOptions(defaultLoopCount);
            return UnloadUnusedAssetsAsync(options);
        }

        /// <summary>
        /// 回收不再使用的资源
        /// 说明：卸载引用计数为零的资源
        /// </summary>
        public UnloadUnusedAssetsOperation UnloadUnusedAssetsAsync(UnloadUnusedAssetsOptions options)
        {
            EnsureInitialized();
            var operation = new UnloadUnusedAssetsOperation(_resourceManager, options);
            OperationSystem.StartOperation(PackageName, operation);
            return operation;
        }

        /// <summary>
        /// 资源回收
        /// 说明：尝试卸载指定的资源
        /// </summary>
        public void TryUnloadUnusedAsset(string location, int loopCount = 10)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
            _resourceManager.TryUnloadUnusedAsset(assetInfo, loopCount);
        }

        /// <summary>
        /// 资源回收
        /// 说明：尝试卸载指定的资源
        /// </summary>
        public void TryUnloadUnusedAsset(AssetInfo assetInfo, int loopCount = 10)
        {
            EnsureInitialized();
            _resourceManager.TryUnloadUnusedAsset(assetInfo, loopCount);
        }
        #endregion

        #region 资源信息
        /// <summary>
        /// 是否需要从远端更新下载
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public bool IsNeedDownloadFromRemote(string location)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
            return _fileSystemHost.IsNeedDownloadFromRemoteInternal(assetInfo);
        }

        /// <summary>
        /// 是否需要从远端更新下载
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public bool IsNeedDownloadFromRemote(AssetInfo assetInfo)
        {
            EnsureInitialized();
            return _fileSystemHost.IsNeedDownloadFromRemoteInternal(assetInfo);
        }

        /// <summary>
        /// 获取所有的资源信息
        /// </summary>
        public AssetInfo[] GetAllAssetInfos()
        {
            EnsureInitialized();
            return _fileSystemHost.ActiveManifest.GetAllAssetInfos();
        }

        /// <summary>
        /// 获取资源信息列表
        /// </summary>
        /// <param name="tag">资源标签</param>
        public AssetInfo[] GetAssetInfos(string tag)
        {
            EnsureInitialized();
            string[] tags = new string[] { tag };
            return _fileSystemHost.ActiveManifest.GetAssetInfosByTags(tags);
        }

        /// <summary>
        /// 获取资源信息列表
        /// </summary>
        /// <param name="tags">资源标签列表</param>
        public AssetInfo[] GetAssetInfos(string[] tags)
        {
            EnsureInitialized();
            return _fileSystemHost.ActiveManifest.GetAssetInfosByTags(tags);
        }

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public AssetInfo GetAssetInfo(string location)
        {
            EnsureInitialized();
            return ConvertLocationToAssetInfo(location, null);
        }

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">资源类型</param>
        public AssetInfo GetAssetInfo(string location, System.Type type)
        {
            EnsureInitialized();
            return ConvertLocationToAssetInfo(location, type);
        }

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetGUID">资源GUID</param>
        public AssetInfo GetAssetInfoByGUID(string assetGUID)
        {
            EnsureInitialized();
            return ConvertAssetGUIDToAssetInfo(assetGUID, null);
        }

        /// <summary>
        /// 获取资源信息
        /// </summary>
        /// <param name="assetGUID">资源GUID</param>
        /// <param name="type">资源类型</param>
        public AssetInfo GetAssetInfoByGUID(string assetGUID, System.Type type)
        {
            EnsureInitialized();
            return ConvertAssetGUIDToAssetInfo(assetGUID, type);
        }

        /// <summary>
        /// 检查资源定位地址是否有效
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public bool CheckLocationValid(string location)
        {
            EnsureInitialized();
            string assetPath = _fileSystemHost.ActiveManifest.TryMappingToAssetPath(location);
            return string.IsNullOrEmpty(assetPath) == false;
        }
        #endregion

        #region 原生文件
        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public RawFileHandle LoadRawFileSync(AssetInfo assetInfo)
        {
            EnsureInitialized();
            return LoadRawFileInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载原生文件
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public RawFileHandle LoadRawFileSync(string location)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
            return LoadRawFileInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <param name="priority">加载的优先级</param>
        public RawFileHandle LoadRawFileAsync(AssetInfo assetInfo, uint priority = 0)
        {
            EnsureInitialized();
            return LoadRawFileInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载原生文件
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="priority">加载的优先级</param>
        public RawFileHandle LoadRawFileAsync(string location, uint priority = 0)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
            return LoadRawFileInternal(assetInfo, false, priority);
        }


        private RawFileHandle LoadRawFileInternal(AssetInfo assetInfo, bool waitForAsyncComplete, uint priority)
        {
            assetInfo.LoadMethod = AssetInfo.ELoadMethod.LoadRawFile;
            var handle = _resourceManager.LoadRawFileAsync(assetInfo, priority);
            if (waitForAsyncComplete)
                handle.WaitForAsyncComplete();
            return handle;
        }
        #endregion

        #region 场景加载
        /// <summary>
        /// 同步加载场景
        /// </summary>
        /// <param name="location">场景的定位地址</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="physicsMode">场景物理模式</param>
        public SceneHandle LoadSceneSync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
            return LoadSceneInternal(assetInfo, true, sceneMode, physicsMode, false, 0);
        }

        /// <summary>
        /// 同步加载场景
        /// </summary>
        /// <param name="assetInfo">场景的资源信息</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="physicsMode">场景物理模式</param>
        public SceneHandle LoadSceneSync(AssetInfo assetInfo, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None)
        {
            EnsureInitialized();
            return LoadSceneInternal(assetInfo, true, sceneMode, physicsMode, false, 0);
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="location">场景的定位地址</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="physicsMode">场景物理模式</param>
        /// <param name="suspendLoad">场景加载到90%自动挂起</param>
        /// <param name="priority">加载的优先级</param>
        public SceneHandle LoadSceneAsync(string location, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool suspendLoad = false, uint priority = 0)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, null);
            return LoadSceneInternal(assetInfo, false, sceneMode, physicsMode, suspendLoad, priority);
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        /// <param name="assetInfo">场景的资源信息</param>
        /// <param name="sceneMode">场景加载模式</param>
        /// <param name="physicsMode">场景物理模式</param>
        /// <param name="suspendLoad">场景加载到90%自动挂起</param>
        /// <param name="priority">加载的优先级</param>
        public SceneHandle LoadSceneAsync(AssetInfo assetInfo, LoadSceneMode sceneMode = LoadSceneMode.Single, LocalPhysicsMode physicsMode = LocalPhysicsMode.None, bool suspendLoad = false, uint priority = 0)
        {
            EnsureInitialized();
            return LoadSceneInternal(assetInfo, false, sceneMode, physicsMode, suspendLoad, priority);
        }

        private SceneHandle LoadSceneInternal(AssetInfo assetInfo, bool waitForAsyncComplete, LoadSceneMode sceneMode, LocalPhysicsMode physicsMode, bool suspendLoad, uint priority)
        {
            DebugEnsureAssetType(assetInfo.AssetType);
            assetInfo.LoadMethod = AssetInfo.ELoadMethod.LoadScene;
            var loadSceneParams = new LoadSceneParameters(sceneMode, physicsMode);
            var handle = _resourceManager.LoadSceneAsync(assetInfo, loadSceneParams, suspendLoad, priority);
            if (waitForAsyncComplete)
                handle.WaitForAsyncComplete();
            return handle;
        }
        #endregion

        #region 资源加载 [主资源]
        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public AssetHandle LoadAssetSync(AssetInfo assetInfo)
        {
            EnsureInitialized();
            return LoadAssetInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <param name="location">资源的定位地址</param>
        public AssetHandle LoadAssetSync<TObject>(string location) where TObject : UnityEngine.Object
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
            return LoadAssetInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">资源类型</param>
        public AssetHandle LoadAssetSync(string location, System.Type type)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
            return LoadAssetInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public AssetHandle LoadAssetSync(string location)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(UnityEngine.Object));
            return LoadAssetInternal(assetInfo, true, 0);
        }


        /// <summary>
        /// 异步加载资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <param name="priority">加载的优先级</param>
        public AssetHandle LoadAssetAsync(AssetInfo assetInfo, uint priority = 0)
        {
            EnsureInitialized();
            return LoadAssetInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载资源对象
        /// </summary>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <param name="location">资源的定位地址</param>
        /// <param name="priority">加载的优先级</param>
        public AssetHandle LoadAssetAsync<TObject>(string location, uint priority = 0) where TObject : UnityEngine.Object
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
            return LoadAssetInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">资源类型</param>
        /// <param name="priority">加载的优先级</param>
        public AssetHandle LoadAssetAsync(string location, System.Type type, uint priority = 0)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
            return LoadAssetInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="priority">加载的优先级</param>
        public AssetHandle LoadAssetAsync(string location, uint priority = 0)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(UnityEngine.Object));
            return LoadAssetInternal(assetInfo, false, priority);
        }


        private AssetHandle LoadAssetInternal(AssetInfo assetInfo, bool waitForAsyncComplete, uint priority)
        {
            DebugEnsureAssetType(assetInfo.AssetType);
            assetInfo.LoadMethod = AssetInfo.ELoadMethod.LoadAsset;
            var handle = _resourceManager.LoadAssetAsync(assetInfo, priority);
            if (waitForAsyncComplete)
                handle.WaitForAsyncComplete();
            return handle;
        }
        #endregion

        #region 资源加载 [子资源]
        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public SubAssetsHandle LoadSubAssetsSync(AssetInfo assetInfo)
        {
            EnsureInitialized();
            return LoadSubAssetsInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <param name="location">资源的定位地址</param>
        public SubAssetsHandle LoadSubAssetsSync<TObject>(string location) where TObject : UnityEngine.Object
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
            return LoadSubAssetsInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">子对象类型</param>
        public SubAssetsHandle LoadSubAssetsSync(string location, System.Type type)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
            return LoadSubAssetsInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载子资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public SubAssetsHandle LoadSubAssetsSync(string location)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(UnityEngine.Object));
            return LoadSubAssetsInternal(assetInfo, true, 0);
        }


        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <param name="priority">加载的优先级</param>
        public SubAssetsHandle LoadSubAssetsAsync(AssetInfo assetInfo, uint priority = 0)
        {
            EnsureInitialized();
            return LoadSubAssetsInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <param name="location">资源的定位地址</param>
        /// <param name="priority">加载的优先级</param>
        public SubAssetsHandle LoadSubAssetsAsync<TObject>(string location, uint priority = 0) where TObject : UnityEngine.Object
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
            return LoadSubAssetsInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">子对象类型</param>
        /// <param name="priority">加载的优先级</param>
        public SubAssetsHandle LoadSubAssetsAsync(string location, System.Type type, uint priority = 0)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
            return LoadSubAssetsInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载子资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="priority">加载的优先级</param>
        public SubAssetsHandle LoadSubAssetsAsync(string location, uint priority = 0)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(UnityEngine.Object));
            return LoadSubAssetsInternal(assetInfo, false, priority);
        }


        private SubAssetsHandle LoadSubAssetsInternal(AssetInfo assetInfo, bool waitForAsyncComplete, uint priority)
        {
            DebugEnsureAssetType(assetInfo.AssetType);
            assetInfo.LoadMethod = AssetInfo.ELoadMethod.LoadSubAssets;
            var handle = _resourceManager.LoadSubAssetsAsync(assetInfo, priority);
            if (waitForAsyncComplete)
                handle.WaitForAsyncComplete();
            return handle;
        }
        #endregion

        #region 资源加载 [全资源]
        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        public AllAssetsHandle LoadAllAssetsSync(AssetInfo assetInfo)
        {
            EnsureInitialized();
            return LoadAllAssetsInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <param name="location">资源的定位地址</param>
        public AllAssetsHandle LoadAllAssetsSync<TObject>(string location) where TObject : UnityEngine.Object
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
            return LoadAllAssetsInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">子对象类型</param>
        public AllAssetsHandle LoadAllAssetsSync(string location, System.Type type)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
            return LoadAllAssetsInternal(assetInfo, true, 0);
        }

        /// <summary>
        /// 同步加载资源包内所有资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        public AllAssetsHandle LoadAllAssetsSync(string location)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(UnityEngine.Object));
            return LoadAllAssetsInternal(assetInfo, true, 0);
        }


        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <param name="priority">加载的优先级</param>
        public AllAssetsHandle LoadAllAssetsAsync(AssetInfo assetInfo, uint priority = 0)
        {
            EnsureInitialized();
            return LoadAllAssetsInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <typeparam name="TObject">资源类型</typeparam>
        /// <param name="location">资源的定位地址</param>
        /// <param name="priority">加载的优先级</param>
        public AllAssetsHandle LoadAllAssetsAsync<TObject>(string location, uint priority = 0) where TObject : UnityEngine.Object
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(TObject));
            return LoadAllAssetsInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="type">子对象类型</param>
        /// <param name="priority">加载的优先级</param>
        public AllAssetsHandle LoadAllAssetsAsync(string location, System.Type type, uint priority = 0)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, type);
            return LoadAllAssetsInternal(assetInfo, false, priority);
        }

        /// <summary>
        /// 异步加载资源包内所有资源对象
        /// </summary>
        /// <param name="location">资源的定位地址</param>
        /// <param name="priority">加载的优先级</param>
        public AllAssetsHandle LoadAllAssetsAsync(string location, uint priority = 0)
        {
            EnsureInitialized();
            AssetInfo assetInfo = ConvertLocationToAssetInfo(location, typeof(UnityEngine.Object));
            return LoadAllAssetsInternal(assetInfo, false, priority);
        }


        private AllAssetsHandle LoadAllAssetsInternal(AssetInfo assetInfo, bool waitForAsyncComplete, uint priority)
        {
            DebugEnsureAssetType(assetInfo.AssetType);
            assetInfo.LoadMethod = AssetInfo.ELoadMethod.LoadAllAssets;
            var handle = _resourceManager.LoadAllAssetsAsync(assetInfo, priority);
            if (waitForAsyncComplete)
                handle.WaitForAsyncComplete();
            return handle;
        }
        #endregion

        #region 资源下载
        /// <summary>
        /// 创建资源下载器，用于下载指定的资源标签列表关联的资源包文件
        /// </summary>
        public ResourceDownloaderOperation CreateResourceDownloader(ResourceDownloaderOptions options)
        {
            EnsureInitialized();
            return _fileSystemHost.CreateResourceDownloader(options);
        }

        /// <summary>
        /// 创建资源下载器，用于下载指定的资源信息列表依赖的资源包文件
        /// </summary>
        public ResourceDownloaderOperation CreateResourceDownloader(BundleDownloaderOptions options)
        {
            EnsureInitialized();
            return _fileSystemHost.CreateResourceDownloader(options);
        }
        #endregion

        #region 资源解压
        /// <summary>
        /// 创建内置资源解压器，用于解压指定的资源标签列表关联的资源包文件
        /// </summary>
        public ResourceUnpackerOperation CreateResourceUnpacker(ResourceUnpackerOptions options)
        {
            EnsureInitialized();
            return _fileSystemHost.CreateResourceUnpacker(options);
        }
        #endregion

        #region 资源导入
        /// <summary>
        /// 创建资源导入器
        /// </summary>
        public ResourceImporterOperation CreateResourceImporter(BundleImporterOptions options)
        {
            EnsureInitialized();
            return _fileSystemHost.CreateResourceImporter(options);
        }
        #endregion

        #region 内部方法
        internal AssetInfo ConvertLocationToAssetInfo(string location, System.Type assetType)
        {
            return _fileSystemHost.ActiveManifest.ConvertLocationToAssetInfo(location, assetType);
        }
        internal AssetInfo ConvertAssetGUIDToAssetInfo(string assetGUID, System.Type assetType)
        {
            return _fileSystemHost.ActiveManifest.ConvertAssetGUIDToAssetInfo(assetGUID, assetType);
        }
        #endregion

        #region 调试方法
        private void EnsureInitialized(bool checkActiveManifest = true)
        {
            if (InitializeStatus != EOperationStatus.Succeed)
            {
                switch (InitializeStatus)
                {
                    case EOperationStatus.None:
                        throw new YooPackageException(PackageName, "Resource package not initialized.");

                    case EOperationStatus.Processing:
                        throw new YooPackageException(PackageName, "Resource package initialization not completed.");

                    case EOperationStatus.Failed:
                        string error = _initializeOp == null ? string.Empty : _initializeOp.Error;
                        throw new YooPackageException(PackageName, $"Resource package initialization failed. Error: {error}");

                    case EOperationStatus.Aborted:
                        throw new YooPackageException(PackageName, "Resource package initialization was aborted.");
                }
            }

            if (checkActiveManifest)
            {
                if (_fileSystemHost.ActiveManifest == null)
                    throw new YooPackageException(PackageName, "Cannot found active package manifest.");
            }
        }

        [Conditional("DEBUG")]
        private void DebugEnsureAssetType(System.Type type)
        {
            if (type == null)
                return;

            if (typeof(UnityEngine.Behaviour).IsAssignableFrom(type))
                throw new YooLoadException($"Load asset type is invalid : {type.FullName}");

            if (typeof(UnityEngine.Object).IsAssignableFrom(type) == false)
                throw new YooLoadException($"Load asset type is invalid : {type.FullName}");
        }
        #endregion

        #region 调试信息
        internal DiagnosticPackageData GetDebugPackageData()
        {
            DiagnosticPackageData data = new DiagnosticPackageData();
            data.PackageName = PackageName;
            data.ProviderInfos = _resourceManager.GetDebugProviderInfos();
            data.BundleInfos = _resourceManager.GetDebugBundleInfos();
            data.OperationInfos = OperationSystem.GetDebugOperationInfos(PackageName);
            return data;
        }
        #endregion
    }
}