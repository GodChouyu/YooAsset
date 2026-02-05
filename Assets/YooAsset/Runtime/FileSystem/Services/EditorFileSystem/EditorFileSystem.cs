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
        public IFileCache FileCache { private set; get; }

        /// <summary>
        /// 解压调度器
        /// </summary>
        public DownloadSchedulerOperation DownloadScheduler { get; set; }

        /// <summary>
        /// 下载后台接口
        /// </summary>
        public IDownloadBackend DownloadBackend { private set; get; }

        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName { private set; get; }

        #region 自定义参数
        /// <summary>
        /// 自定义参数：UnityWebRequest 创建委托
        /// </summary>
        public UnityWebRequestCreator WebRequestCreator { private set; get; }

        /// <summary>
        /// 自定义参数：模拟WebGL平台模式
        /// </summary>
        public bool VirtualWebGLMode { private set; get; } = false;

        /// <summary>
        /// 自定义参数：模拟虚拟下载模式
        /// </summary>
        public bool VirtualDownloadMode { private set; get; } = false;

        /// <summary>
        /// 自定义参数：模拟虚拟下载的网速（单位：字节）
        /// </summary>
        public int VirtualDownloadSpeed { private set; get; } = 1024;

        /// <summary>
        /// 自定义参数：最大并发连接数
        /// 默认值：8（推荐范围 1-32）
        /// 说明：过大的并发数可能被服务器限流，也会增加本地资源消耗 
        /// </summary>
        public int DownloadMaxConcurrency { private set; get; } = 8;

        /// <summary>
        /// 自定义参数：每帧发起的最大请求数
        /// 默认值：8（推荐范围 1-32） 
        /// 说明：避免单帧发起过多请求导致卡顿 
        /// </summary>
        public int DownloadMaxRequestPerFrame { private set; get; } = 8;

        /// <summary>
        /// 自定义参数：异步模拟加载最小帧数
        /// </summary>
        public int AsyncSimulateMinFrame { private set; get; } = 1;

        /// <summary>
        /// 自定义参数：异步模拟加载最大帧数
        /// </summary>
        public int AsyncSimulateMaxFrame { private set; get; } = 1;
        #endregion

        public EditorFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new EFSInitializeOperation(this);
            return operation;
        }
        public virtual FSRequestVersionOperation RequestVersionAsync(RequestVersionOptions options)
        {
            var operation = new EFSRequestVersionOperation(this);
            return operation;
        }
        public virtual FSLoadManifestOperation LoadManifestAsync(LoadManifestOptions options)
        {
            var operation = new EFSLoadManifestOperation(this, options.PackageVersion);
            return operation;
        }
        public virtual FSClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            var operation = new FSClearCacheCompleteOperation();
            return operation;
        }
        public virtual FSDownloadFileOperation DownloadFileAsync(FSDownloadFileOptions options)
        {
            var downloader = new EFSDownloadFileOperation(this, options);
            return downloader;
        }
        public virtual FSLoadBundleOperation LoadBundleAsync(FCLoadBundleOptions options)
        {
            var operation = new EFSLoadBundleOperation(this, options);
            return operation;
        }

        public virtual void SetParameter(string name, object value)
        {
            if (name == FileSystemParametersDefine.DOWNLOAD_BACKEND)
            {
                DownloadBackend = (IDownloadBackend)value;
            }
            else if (name == FileSystemParametersDefine.UNITY_WEB_REQUEST_CREATOR)
            {
                WebRequestCreator = (UnityWebRequestCreator)value;
            }
            else if (name == FileSystemParametersDefine.VIRTUAL_WEBGL_MODE)
            {
                VirtualWebGLMode = Convert.ToBoolean(value);
            }
            else if (name == FileSystemParametersDefine.VIRTUAL_DOWNLOAD_MODE)
            {
                VirtualDownloadMode = Convert.ToBoolean(value);
            }
            else if (name == FileSystemParametersDefine.VIRTUAL_DOWNLOAD_SPEED)
            {
                VirtualDownloadSpeed = Convert.ToInt32(value);
            }
            else if (name == FileSystemParametersDefine.DOWNLOAD_MAX_CONCURRENCY)
            {
                int convertValue = Convert.ToInt32(value);
                if (convertValue > 32)
                {
                    YooLogger.Warning($"DOWNLOAD_MAX_CONCURRENCY value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32          
                DownloadMaxConcurrency = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemParametersDefine.DOWNLOAD_MAX_REQUEST_PER_FRAME)
            {
                int convertValue = Convert.ToInt32(value);
                if (convertValue > 32)
                {
                    YooLogger.Warning($"DOWNLOAD_MAX_REQUEST_PER_FRAME value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32          
                DownloadMaxRequestPerFrame = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemParametersDefine.ASYNC_SIMULATE_MIN_FRAME)
            {
                AsyncSimulateMinFrame = Convert.ToInt32(value);
            }
            else if (name == FileSystemParametersDefine.ASYNC_SIMULATE_MAX_FRAME)
            {
                AsyncSimulateMaxFrame = Convert.ToInt32(value);
            }
            else
            {
                YooLogger.Warning($"Invalid parameter : {name}");
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
                DownloadScheduler.Dispose();
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
        public string GetEditorPackageVersionFilePath()
        {
            string fileName = YooAssetSettingsData.GetPackageVersionFileName(PackageName);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        public string GetEditorPackageHashFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetPackageHashFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        public string GetEditorPackageManifestFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetManifestBinaryFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        #endregion
    }
}
