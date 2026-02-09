using System;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 模拟文件系统
    /// </summary>
    internal class EditorFileSystem : IFileSystem
    {
        protected string _packageRoot;

        /// <summary>
        /// 虚拟文件缓存系统
        /// </summary>
        public IFileCache FileCache { get; private set; }

        /// <summary>
        /// 下载调度器
        /// </summary>
        public DownloadSchedulerOperation DownloadScheduler { get; set; }

        /// <summary>
        /// 下载后台接口
        /// </summary>
        public IDownloadBackend DownloadBackend { get; private set; }

        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName { get; private set; }

        #region 自定义参数
        /// <summary>
        /// 自定义参数：UnityWebRequest 创建委托
        /// </summary>
        public UnityWebRequestCreator WebRequestCreator { get; private set; }

        /// <summary>
        /// 自定义参数：模拟WebGL平台模式
        /// </summary>
        public bool VirtualWebGLMode { get; private set; } = false;

        /// <summary>
        /// 自定义参数：模拟虚拟下载模式
        /// </summary>
        public bool VirtualDownloadMode { get; private set; } = false;

        /// <summary>
        /// 自定义参数：模拟虚拟下载的网速（单位：字节）
        /// 默认值：1024
        /// </summary>
        public int VirtualDownloadSpeed { get; private set; } = 1024;

        /// <summary>
        /// 自定义参数：最大并发连接数
        /// 默认值：8（推荐范围 1-32）
        /// 说明：过大的并发数可能被服务器限流，也会增加本地资源消耗 
        /// </summary>
        public int DownloadMaxConcurrency { get; private set; } = 8;

        /// <summary>
        /// 自定义参数：每帧发起的最大请求数
        /// 默认值：8（推荐范围 1-32） 
        /// 说明：避免单帧发起过多请求导致卡顿 
        /// </summary>
        public int DownloadMaxRequestPerFrame { get; private set; } = 8;

        /// <summary>
        /// 自定义参数：异步模拟加载最小帧数
        /// 默认值：1
        /// </summary>
        public int AsyncSimulateMinFrame { get; private set; } = 1;

        /// <summary>
        /// 自定义参数：异步模拟加载最大帧数
        /// 默认值：1
        /// </summary>
        public int AsyncSimulateMaxFrame { get; private set; } = 1;
        #endregion

        public EditorFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new EFSInitializeOperation(this);
            return operation;
        }
        public virtual FSRequestPackageVersionOperation RequestPackageVersionAsync(FSRequestPackageVersionOptions options)
        {
            var operation = new EFSRequestPackageVersionOperation(this);
            return operation;
        }
        public virtual FSLoadPackageManifestOperation LoadPackageManifestAsync(FSLoadPackageManifestOptions options)
        {
            var operation = new EFSLoadPackageManifestOperation(this, options.PackageVersion);
            return operation;
        }
        public virtual FSLoadPackageBundleOperation LoadPackageBundleAsync(FSLoadPackageBundleOptions options)
        {
            var operation = new EFSLoadPackageBundleOperation(this, options);
            return operation;
        }
        public virtual FSDownloadFileOperation DownloadFileAsync(FSDownloadFileOptions options)
        {
            var downloader = new EFSDownloadFileOperation(this, options);
            return downloader;
        }
        public virtual FSClearCacheOperation ClearCacheAsync(FSClearCacheOptions options)
        {
            if (options.ClearMode == EManifestClearMode.ClearAllManifestFiles.ToString())
            {
                var operation = new FSClearCacheCompleteOperation();
                return operation;
            }
            else if (options.ClearMode == EManifestClearMode.ClearUnusedManifestFiles.ToString())
            {
                var operation = new FSClearCacheCompleteOperation();
                return operation;
            }
            else
            {
                var operation = new EFSClearCacheOperation(this, options);
                return operation;
            }
        }

        public virtual void SetParameter(string name, object value)
        {
            if (name == FileSystemConsts.DOWNLOAD_BACKEND)
            {
                DownloadBackend = (IDownloadBackend)value;
            }
            else if (name == FileSystemConsts.UNITY_WEB_REQUEST_CREATOR)
            {
                WebRequestCreator = (UnityWebRequestCreator)value;
            }
            else if (name == FileSystemConsts.VIRTUAL_WEBGL_MODE)
            {
                VirtualWebGLMode = Convert.ToBoolean(value);
            }
            else if (name == FileSystemConsts.VIRTUAL_DOWNLOAD_MODE)
            {
                VirtualDownloadMode = Convert.ToBoolean(value);
            }
            else if (name == FileSystemConsts.VIRTUAL_DOWNLOAD_SPEED)
            {
                VirtualDownloadSpeed = Convert.ToInt32(value);
            }
            else if (name == FileSystemConsts.DOWNLOAD_MAX_CONCURRENCY)
            {
                int convertValue = Convert.ToInt32(value);
                if (convertValue > 32)
                {
                    YooLogger.Warning($"DOWNLOAD_MAX_CONCURRENCY value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32          
                DownloadMaxConcurrency = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemConsts.DOWNLOAD_MAX_REQUEST_PER_FRAME)
            {
                int convertValue = Convert.ToInt32(value);
                if (convertValue > 32)
                {
                    YooLogger.Warning($"DOWNLOAD_MAX_REQUEST_PER_FRAME value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32          
                DownloadMaxRequestPerFrame = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemConsts.ASYNC_SIMULATE_MIN_FRAME)
            {
                AsyncSimulateMinFrame = Convert.ToInt32(value);
            }
            else if (name == FileSystemConsts.ASYNC_SIMULATE_MAX_FRAME)
            {
                AsyncSimulateMaxFrame = Convert.ToInt32(value);
            }
            else
            {
                YooLogger.Warning($"Invalid parameter: {name}");
            }
        }
        public virtual void OnCreate(string packageName, string packageRoot)
        {
            PackageName = packageName;

            if (string.IsNullOrEmpty(packageRoot))
                throw new YooFileSystemException($"{nameof(EditorFileSystem)} package root is null or empty.");

            _packageRoot = packageRoot;

            // 创建默认的下载后台接口
            if (DownloadBackend == null)
                DownloadBackend = new UnityWebRequestBackend(WebRequestCreator);

            // 创建编辑器文件缓存系统
            if (AsyncSimulateMinFrame > AsyncSimulateMaxFrame)
                AsyncSimulateMinFrame = AsyncSimulateMaxFrame;
            var cacheConfig = new EditorFileCache.CacheConfig();
            cacheConfig.VirtualDownloadMode = VirtualDownloadMode;
            cacheConfig.VirtualWebGLMode = VirtualWebGLMode;
            cacheConfig.AsyncSimulateMinFrame = AsyncSimulateMinFrame;
            cacheConfig.AsyncSimulateMaxFrame = AsyncSimulateMaxFrame;
            FileCache = new EditorFileCache(packageName, _packageRoot, cacheConfig);
        }
        public virtual void OnDestroy()
        {
            if (FileCache != null)
            {
                FileCache.Dispose();
                FileCache = null;
            }

            if (DownloadScheduler != null)
            {
                DownloadScheduler.AbortOperation();
                DownloadScheduler = null;
            }

            if (DownloadBackend != null)
            {
                DownloadBackend.Dispose();
                DownloadBackend = null;
            }
        }

        public virtual bool Belong(PackageBundle bundle)
        {
            return true;
        }
        public virtual bool NeedDownload(PackageBundle bundle)
        {
            if (Belong(bundle) == false)
                return false;

            return FileCache.IsCached(bundle.BundleGUID) == false;
        }
        public virtual bool NeedUnpack(PackageBundle bundle)
        {
            return false;
        }
        public virtual bool NeedImport(PackageBundle bundle)
        {
            return false;
        }

        #region 内部方法
        /// <summary>
        /// 获取编辑器包裹版本文件路径
        /// </summary>
        public string GetEditorPackageVersionFilePath()
        {
            string fileName = YooAssetSettingsData.GetPackageVersionFileName(PackageName);
            return PathUtility.Combine(_packageRoot, fileName);
        }

        /// <summary>
        /// 获取编辑器包裹哈希文件路径
        /// </summary>
        public string GetEditorPackageHashFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetPackageHashFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }

        /// <summary>
        /// 获取编辑器包裹清单文件路径
        /// </summary>
        public string GetEditorPackageManifestFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetManifestBinaryFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        #endregion
    }
}
