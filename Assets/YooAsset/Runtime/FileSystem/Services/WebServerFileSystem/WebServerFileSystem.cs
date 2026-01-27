using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// Web文件系统
    /// </summary>
    internal class WebServerFileSystem : IFileSystem
    {
        protected readonly Dictionary<string, string> _webFilePathMapping = new Dictionary<string, string>(10000);
        protected string _packageRoot = string.Empty;

        /// <summary>
        /// Web文件缓存系统
        /// </summary>
        public IFileCache FileCache { get; private set; }

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
        public UnityWebRequestCreator WebRequestCreator { private set; get; }

        /// <summary>
        /// 禁用Unity的网络缓存
        /// </summary>
        public bool DisableUnityWebCache { private set; get; } = false;

        /// <summary>
        /// 自定义参数：下载任务的看门狗机制超时时间
        /// </summary>
        public int DownloadWatchDogTimeout { private set; get; } = 0;

        /// <summary>
        /// 自定义参数：资源清单服务类
        /// </summary>
        public IManifestRestoreServices ManifestRestoreServices { private set; get; }
        #endregion


        public WebServerFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new WSFSInitializeOperation(this);
            return operation;
        }
        public virtual FSRequestVersionOperation RequestVersionAsync(RequestVersionOptions options)
        {
            var operation = new WSFSRequestVersionOperation(this, options.Timeout);
            return operation;
        }
        public virtual FSLoadManifestOperation LoadManifestAsync(LoadManifestOptions options)
        {
            var operation = new WSFSLoadManifestOperation(this, options.PackageVersion, options.Timeout);
            return operation;
        }
        public virtual FSClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            var operation = new FSClearCacheCompleteOperation();
            return operation;
        }
        public virtual FSDownloadFileOperation DownloadFileAsync(DownloadFileOptions options)
        {
            throw new System.NotImplementedException();
        }
        public virtual FSLoadBundleOperation LoadBundleAsync(LoadBundleOptions options)
        {
            var operation = new WSFSLoadAssetBundleOperation(this, options);
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
            else if (name == FileSystemParametersDefine.DISABLE_UNITY_WEB_CACHE)
            {
                DisableUnityWebCache = Convert.ToBoolean(value);
            }
            else if (name == FileSystemParametersDefine.DOWNLOAD_WATCH_DOG_TIME)
            {
                int convertValue = Convert.ToInt32(value);
                DownloadWatchDogTimeout = Mathf.Clamp(convertValue, 0, int.MaxValue);
            }
            else if (name == FileSystemParametersDefine.MANIFEST_RESTORE_SERVICES)
            {
                ManifestRestoreServices = (IManifestRestoreServices)value;
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
                _packageRoot = GetDefaultWebPackageRoot(packageName);
            else
                _packageRoot = packageRoot;

            // 创建默认的下载后台接口
            if (DownloadBackend == null)
                DownloadBackend = new UnityWebRequestBackend(WebRequestCreator);

            // 创建Web文件缓存系统
            var cacheConfig = new WebServerFileCache.CacheConfig();
            cacheConfig.DisableUnityWebCache = DisableUnityWebCache;
            cacheConfig.DownloadBackend = DownloadBackend;
            cacheConfig.WatchdogTimeout = DownloadWatchDogTimeout;
            cacheConfig.RetryCount = int.MaxValue;
            FileCache = new WebServerFileCache(packageName, _packageRoot, cacheConfig);
        }
        public virtual void OnDestroy()
        {
            if (FileCache != null)
            {
                FileCache.Dispose();
                FileCache = null;
            }

            if (DownloadBackend != null)
            {
                DownloadBackend.Dispose();
                DownloadBackend = null;
            }
        }

        public virtual bool Belong(PackageBundle bundle)
        {
            return FileCache.IsCached(bundle.BundleGUID);
        }
        public virtual bool NeedDownload(PackageBundle bundle)
        {
            return false;
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
        protected string GetDefaultWebPackageRoot(string packageName)
        {
            string rootDirectory = YooAssetSettingsData.GetYooDefaultBuildinRoot();
            return PathUtility.Combine(rootDirectory, packageName);
        }
        public string GetWebPackageVersionFilePath()
        {
            string fileName = YooAssetSettingsData.GetPackageVersionFileName(PackageName);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        public string GetWebPackageHashFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetPackageHashFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        public string GetWebPackageManifestFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetManifestBinaryFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        #endregion
    }
}
