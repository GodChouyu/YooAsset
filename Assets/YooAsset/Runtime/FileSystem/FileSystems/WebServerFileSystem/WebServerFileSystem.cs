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
        public class FileWrapper
        {
            public string FileName { private set; get; }

            public FileWrapper(string fileName)
            {
                FileName = fileName;
            }
        }

        protected readonly Dictionary<string, FileWrapper> _wrappers = new Dictionary<string, FileWrapper>(10000);
        protected readonly Dictionary<string, string> _webFilePathMapping = new Dictionary<string, string>(10000);
        protected string _webPackageRoot = string.Empty;

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
                return _webPackageRoot;
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
        /// 自定义参数：加载 AssetBundle 的工厂委托
        /// </summary>
        public LoadWebAssetBundleOperationFactory LoadAssetBundleFactory { private set; get; }

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
            PackageBundle bundle = options.Bundle;
            if (bundle.BundleType == (int)EBundleType.AssetBundle)
            {
                var operation = new WSFSLoadAssetBundleOperation(this, bundle);
                return operation;
            }
            else
            {
                string error = $"{nameof(WebServerFileSystem)} not support load bundle type : {bundle.BundleType}";
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

            if (string.IsNullOrEmpty(packageRoot))
                _webPackageRoot = GetDefaultWebPackageRoot(packageName);
            else
                _webPackageRoot = packageRoot;

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
            return _wrappers.ContainsKey(bundle.BundleGUID);
        }
        public virtual bool Exists(PackageBundle bundle)
        {
            return _wrappers.ContainsKey(bundle.BundleGUID);
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
        protected string GetDefaultWebPackageRoot(string packageName)
        {
            string rootDirectory = YooAssetSettingsData.GetYooDefaultBuildinRoot();
            return PathUtility.Combine(rootDirectory, packageName);
        }
        public string GetWebFileLoadPath(PackageBundle bundle)
        {
            if (_webFilePathMapping.TryGetValue(bundle.BundleGUID, out string filePath) == false)
            {
                filePath = PathUtility.Combine(_webPackageRoot, bundle.FileName);
                _webFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }
        public string GetWebPackageVersionFilePath()
        {
            string fileName = YooAssetSettingsData.GetPackageVersionFileName(PackageName);
            return PathUtility.Combine(FileRoot, fileName);
        }
        public string GetWebPackageHashFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetPackageHashFileName(PackageName, packageVersion);
            return PathUtility.Combine(FileRoot, fileName);
        }
        public string GetWebPackageManifestFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetManifestBinaryFileName(PackageName, packageVersion);
            return PathUtility.Combine(FileRoot, fileName);
        }
        public string GetCatalogBinaryFileLoadPath()
        {
            return PathUtility.Combine(_webPackageRoot, BuiltinFileSystemConstants.BuiltinCatalogBinaryFileName);
        }

        /// <summary>
        /// 记录内置文件信息
        /// </summary>
        public bool RecordCatalogFile(string bundleGUID, FileWrapper wrapper)
        {
            if (_wrappers.ContainsKey(bundleGUID))
            {
                YooLogger.Error($"{nameof(WebServerFileSystem)} has element : {bundleGUID}");
                return false;
            }

            _wrappers.Add(bundleGUID, wrapper);
            return true;
        }
        #endregion
    }
}
