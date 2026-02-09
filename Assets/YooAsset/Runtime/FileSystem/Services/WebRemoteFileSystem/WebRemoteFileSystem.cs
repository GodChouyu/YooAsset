using System;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// Web远端文件系统
    /// </summary>
    internal class WebRemoteFileSystem : IFileSystem
    {
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
        public UnityWebRequestCreator WebRequestCreator { get; private set; }

        /// <summary>
        /// 自定义参数：禁用Unity的网络缓存
        /// </summary>
        public bool DisableUnityWebCache { get; private set; } = false;

        /// <summary>
        /// 自定义参数：下载任务的看门狗机制超时时间
        /// </summary>
        public int DownloadWatchdogTimeout { get; private set; } = 0;

        /// <summary>
        /// 自定义参数：下载的资源包数据的校验级别
        /// </summary>
        public EFileVerifyLevel DownloadVerifyLevel { get; private set; } = EFileVerifyLevel.Middle;

        /// <summary>
        /// 自定义参数：远程服务接口的实例类（支持跨域下载）
        /// </summary>
        public IRemoteServices RemoteServices { get; private set; }

        /// <summary>
        /// 自定义参数：AssetBundle 解密器
        /// </summary>
        public IBundleDecryptor AssetBundleDecryptor { get; set; }

        /// <summary>
        /// 自定义参数：资源清单解密器
        /// </summary>
        public IManifestDecryptor ManifestDecryptor { get; private set; }

        /// <summary>
        /// 自定义参数：下载重试判定策略
        /// </summary>
        public IDownloadRetryPolicy DownloadRetryPolicy { get; private set; }

        /// <summary>
        /// 自定义参数：URL 选择策略
        /// </summary>
        public IDownloadURLPolicy DownloadURLPolicy { get; private set; }
        #endregion


        public WebRemoteFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new WRFSInitializeOperation(this);
            return operation;
        }
        public virtual FSRequestPackageVersionOperation RequestPackageVersionAsync(FSRequestPackageVersionOptions options)
        {
            var operation = new WRFSRequestPackageVersionOperation(this, options.AppendTimeTicks, options.Timeout);
            return operation;
        }
        public virtual FSLoadPackageManifestOperation LoadPackageManifestAsync(FSLoadPackageManifestOptions options)
        {
            var operation = new WRFSLoadPackageManifestOperation(this, options.PackageVersion, options.Timeout);
            return operation;
        }
        public virtual FSLoadPackageBundleOperation LoadPackageBundleAsync(FSLoadPackageBundleOptions options)
        {
            var operation = new WRFSLoadPackageBundleOperation(this, options);
            return operation;
        }
        public virtual FSDownloadFileOperation DownloadFileAsync(FSDownloadFileOptions options)
        {
            throw new System.NotImplementedException();
        }
        public virtual FSClearCacheOperation ClearCacheAsync(FSClearCacheOptions options)
        {
            var operation = new FSClearCacheCompleteOperation();
            return operation;
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
            else if (name == FileSystemConsts.DISABLE_UNITY_WEB_CACHE)
            {
                DisableUnityWebCache = Convert.ToBoolean(value);
            }
            else if (name == FileSystemConsts.DOWNLOAD_WATCHDOG_TIMEOUT)
            {
                int convertValue = Convert.ToInt32(value);
                DownloadWatchdogTimeout = Mathf.Clamp(convertValue, 0, int.MaxValue);
            }
            else if (name == FileSystemConsts.FILE_VERIFY_LEVEL)
            {
                DownloadVerifyLevel = (EFileVerifyLevel)value;
            }
            else if (name == FileSystemConsts.REMOTE_SERVICES)
            {
                RemoteServices = (IRemoteServices)value;
            }
            else if (name == FileSystemConsts.ASSETBUNDLE_DECRYPTOR)
            {
                AssetBundleDecryptor = (IBundleDecryptor)value;
            }
            else if (name == FileSystemConsts.MANIFEST_DECRYPTOR)
            {
                ManifestDecryptor = (IManifestDecryptor)value;
            }
            else if (name == FileSystemConsts.DOWNLOAD_RETRY_POLICY)
            {
                DownloadRetryPolicy = (IDownloadRetryPolicy)value;
            }
            else if (name == FileSystemConsts.DOWNLOAD_URL_POLICY)
            {
                DownloadURLPolicy = (IDownloadURLPolicy)value;
            }
            else
            {
                YooLogger.Warning($"Invalid parameter: {name}");
            }
        }
        public virtual void OnCreate(string packageName, string packageRoot)
        {
            PackageName = packageName;

            // 创建默认的下载后台接口
            if (DownloadBackend == null)
                DownloadBackend = new UnityWebRequestBackend(WebRequestCreator);

            // 创建默认的下载重试策略
            if (DownloadRetryPolicy == null)
                DownloadRetryPolicy = new DefaultDownloadRetryPolicy();

            // 创建默认的 URL 选择策略
            if (DownloadURLPolicy == null)
                DownloadURLPolicy = new DefaultDownloadURLPolicy();

            // 创建Web文件缓存系统
            var cacheConfig = new WebRemoteFileCache.CacheConfig();
            cacheConfig.WatchdogTimeout = DownloadWatchdogTimeout;
            cacheConfig.DisableUnityWebCache = DisableUnityWebCache;
            cacheConfig.DownloadVerifyLevel = DownloadVerifyLevel;
            cacheConfig.AssetBundleDecryptor = AssetBundleDecryptor;
            cacheConfig.RemoteServices = RemoteServices;
            cacheConfig.DownloadBackend = DownloadBackend;
            cacheConfig.RetryPolicy = DownloadRetryPolicy;
            cacheConfig.URLPolicy = DownloadURLPolicy;
            FileCache = new WebRemoteFileCache(packageName, packageRoot, cacheConfig);
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
            // 注意：保底加载！
            return true;
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
    }
}
