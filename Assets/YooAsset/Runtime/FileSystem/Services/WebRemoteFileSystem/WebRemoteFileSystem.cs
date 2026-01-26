using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// Web文件系统
    /// </summary>
    internal class WebRemoteFileSystem : IFileSystem
    {
        /// <summary>
        /// 下载后台接口
        /// </summary>
        public IDownloadBackend DownloadBackend { private set; get; }

        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName { private set; get; }

        /// <summary>
        /// 文件根目录
        /// </summary>
        public string FileRoot
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// 文件数量
        /// </summary>
        public int FileCount
        {
            get
            {
                return 0;
            }
        }

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
        /// 自定义参数：远程服务接口的实例类（支持跨域下载）
        /// </summary>
        public IRemoteServices RemoteServices { private set; get; }

        /// <summary>
        /// 自定义参数：加载 AssetBundle 的工厂委托
        /// </summary>
        public LoadWebAssetBundleOperationFactory LoadAssetBundleFactory { private set; get; }

        /// <summary>
        /// 自定义参数：资源清单服务类
        /// </summary>
        public IManifestRestoreServices ManifestRestoreServices { private set; get; }
        #endregion


        public WebRemoteFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new WRFSInitializeOperation(this);
            return operation;
        }
        public virtual FSRequestVersionOperation RequestVersionAsync(RequestVersionOptions options)
        {
            var operation = new WRFSRequestVersionOperation(this, options.AppendTimeTicks, options.Timeout);
            return operation;
        }
        public virtual FSLoadManifestOperation LoadManifestAsync(LoadManifestOptions options)
        {
            var operation = new WRFSLoadManifestOperation(this, options.PackageVersion, options.Timeout);
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
            PackageBundle bundle = options.Bundle;
            if (bundle.BundleType == (int)EBundleType.AssetBundle)
            {
                var operation = new WRFSLoadAssetBundleOperation(this, bundle);
                return operation;
            }
            else
            {
                string error = $"{nameof(WebRemoteFileSystem)} not support load bundle type : {bundle.BundleType}";
                var operation = new FSLoadBundleCompleteOperation(error);
                return operation;
            }
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
            else if (name == FileSystemParametersDefine.REMOTE_SERVICES)
            {
                RemoteServices = (IRemoteServices)value;
            }
            else if (name == FileSystemParametersDefine.LOAD_ASSETBUNDLE_OPERATION_FACTORY)
            {
                LoadAssetBundleFactory = (LoadWebAssetBundleOperationFactory)value;
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

            // 创建默认的下载后台接口
            if (DownloadBackend == null)
                DownloadBackend = new UnityWebRequestBackend(WebRequestCreator);

            // 创建默认的 AssetBundle 加载工厂
            if (LoadAssetBundleFactory == null)
                LoadAssetBundleFactory = DefaultLoadAssetBundleOperationFactory;
        }
        public virtual void OnDestroy()
        {
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
        public virtual bool Exists(PackageBundle bundle)
        {
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
        public virtual string GetBundleFilePath(PackageBundle bundle)
        {
            throw new System.NotImplementedException();
        }

        #region 内部方法
        private LoadWebAssetBundleOperation DefaultLoadAssetBundleOperationFactory(bool bundleEncrypted, LoadWebAssetBundleOptions options)
        {
            if (bundleEncrypted)
            {
                string error = $"{nameof(DefaultLoadWebAssetBundleOperation)} cannot load encrypted bundle. Please provide a custom {nameof(LoadWebAssetBundleOperationFactory)}.";
                return new LoadWebAssetBundleCompleteOperation(error, options);
            }
            else
            {
                return new DefaultLoadWebAssetBundleOperation(options);
            }
        }
        #endregion
    }
}
