using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 内置文件系统
    /// </summary>
    internal class BuiltinFileSystem : IFileSystem
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
        protected readonly Dictionary<string, string> _builtinFilePathMapping = new Dictionary<string, string>(10000);
        protected IFileSystem _unpackFileSystem;
        protected string _packageRoot;

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
                return _packageRoot;
            }
        }

        /// <summary>
        /// 文件数量
        /// </summary>
        public int FileCount
        {
            get
            {
                return _wrappers.Count;
            }
        }

        #region 自定义参数
        /// <summary>
        /// 自定义参数：UnityWebRequest 创建委托
        /// </summary>
        public UnityWebRequestCreator WebRequestCreator { private set; get; }

        /// <summary>
        /// 自定义参数：覆盖安装缓存清理模式
        /// </summary>
        public EOverwriteInstallClearMode InstallClearMode { private set; get; } = EOverwriteInstallClearMode.ClearAllManifestFiles;

        /// <summary>
        /// 自定义参数：初始化的时候缓存文件校验级别
        /// </summary>
        public EFileVerifyLevel FileVerifyLevel { private set; get; } = EFileVerifyLevel.Middle;

        /// <summary>
        /// 自定义参数：初始化的时候缓存文件校验最大并发数
        /// </summary>
        public int FileVerifyMaxConcurrency { private set; get; } = 32;

        /// <summary>
        /// 自定义参数：数据文件追加文件格式
        /// </summary>
        public bool AppendFileExtension { private set; get; } = false;

        /// <summary>
        /// 自定义参数：禁用Catalog目录查询文件
        /// </summary>
        public bool DisableCatalogFile { private set; get; } = false;

        /// <summary>
        /// 自定义参数：拷贝内置清单
        /// </summary>
        public bool CopyBuildinPackageManifest { private set; get; } = false;

        /// <summary>
        /// 自定义参数：拷贝内置清单的目标目录
        /// 注意：该参数为空的时候，会获取默认的沙盒目录！
        /// </summary>
        public string CopyBuildinPackageManifestDestRoot { private set; get; }

        /// <summary>
        /// 自定义参数：解压文件系统的根目录
        /// </summary>
        public string UnpackFileSystemRoot { private set; get; }

        /// <summary>
        /// 自定义参数：加载 AssetBundle 的工厂委托
        /// </summary>
        public LoadAssetBundleOperationFactory LoadAssetBundleFactory { private set; get; }

        /// <summary>
        /// 自定义参数：加载 RawBundle 的工厂委托
        /// </summary>
        public LoadRawBundleOperationFactory LoadRawBundleFactory { private set; get; }

        /// <summary>
        /// 自定义参数：资源清单服务类
        /// </summary>
        public IManifestRestoreServices ManifestRestoreServices { private set; get; }

        /// <summary>
        /// 自定义参数：拷贝内置文件接口的实例类
        /// </summary>
        public ILocalFileCopyServices CopyLocalFileServices { private set; get; }
        #endregion


        public BuiltinFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new DBFSInitializeOperation(this);
            return operation;
        }
        public virtual FSRequestVersionOperation RequestVersionAsync(RequestVersionOptions options)
        {
            var operation = new BFSRequestVersionOperation(this);
            return operation;
        }
        public virtual FSLoadManifestOperation LoadManifestAsync(LoadManifestOptions options)
        {
            var operation = new BFSLoadManifestOperation(this, options.PackageVersion);
            return operation;
        }
        public virtual FSClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            return _unpackFileSystem.ClearCacheAsync(options);
        }
        public virtual FSDownloadFileOperation DownloadFileAsync(DownloadFileOptions options)
        {
            // 注意：业务层的解压器会依赖该方法
            options.ImportFilePath = GetBuiltinFileLoadPath(options.Bundle);
            return _unpackFileSystem.DownloadFileAsync(options);
        }
        public virtual FSLoadBundleOperation LoadBundleAsync(LoadBundleOptions options)
        {
            PackageBundle bundle = options.Bundle;
            if (IsUnpackBundleFile(bundle))
            {
                return _unpackFileSystem.LoadBundleAsync(options);
            }

            if (bundle.BundleType == (int)EBundleType.AssetBundle)
            {
                var operation = new BFSLoadAssetBundleOperation(this, bundle);
                return operation;
            }
            else if (bundle.BundleType == (int)EBundleType.RawBundle)
            {
                var operation = new BFSLoadRawBundleOperation(this, bundle);
                return operation;
            }
#if TUANJIE_1_7_OR_NEWER
            else if (bundle.BundleType == (int)EBundleType.InstantBundle)
            {
                var operation = new BFSLoadInstantBundleOperation(this, bundle);
                return operation;
            }
#endif
            else
            {
                string error = $"{nameof(BuiltinFileSystem)} not support load bundle type : {bundle.BundleType}";
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
            else if (name == FileSystemParametersDefine.INSTALL_CLEAR_MODE)
            {
                InstallClearMode = (EOverwriteInstallClearMode)value;
            }
            else if (name == FileSystemParametersDefine.FILE_VERIFY_LEVEL)
            {
                FileVerifyLevel = (EFileVerifyLevel)value;
            }
            else if (name == FileSystemParametersDefine.FILE_VERIFY_MAX_CONCURRENCY)
            {
                int convertValue = Convert.ToInt32(value);
                FileVerifyMaxConcurrency = Mathf.Clamp(convertValue, 1, int.MaxValue);
            }
            else if (name == FileSystemParametersDefine.APPEND_FILE_EXTENSION)
            {
                AppendFileExtension = Convert.ToBoolean(value);
            }
            else if (name == FileSystemParametersDefine.DISABLE_CATALOG_FILE)
            {
                DisableCatalogFile = Convert.ToBoolean(value);
            }
            else if (name == FileSystemParametersDefine.COPY_BUILDIN_PACKAGE_MANIFEST)
            {
                CopyBuildinPackageManifest = Convert.ToBoolean(value);
            }
            else if (name == FileSystemParametersDefine.COPY_BUILDIN_PACKAGE_MANIFEST_DEST_ROOT)
            {
                CopyBuildinPackageManifestDestRoot = (string)value;
            }
            else if (name == FileSystemParametersDefine.UNPACK_FILE_SYSTEM_ROOT)
            {
                UnpackFileSystemRoot = (string)value;
            }
            else if (name == FileSystemParametersDefine.LOAD_ASSETBUNDLE_OPERATION_FACTORY)
            {
                LoadAssetBundleFactory = (LoadAssetBundleOperationFactory)value;
            }
            else if (name == FileSystemParametersDefine.LOAD_RAWBUNDLE_OPERATION_FACTORY)
            {
                LoadRawBundleFactory = (LoadRawBundleOperationFactory)value;
            }
            else if (name == FileSystemParametersDefine.MANIFEST_RESTORE_SERVICES)
            {
                ManifestRestoreServices = (IManifestRestoreServices)value;
            }
            else if (name == FileSystemParametersDefine.COPY_LOCAL_FILE_SERVICES)
            {
                CopyLocalFileServices = (ILocalFileCopyServices)value;
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
                _packageRoot = GetDefaultBuiltinPackageRoot(packageName);
            else
                _packageRoot = packageRoot;

            // 创建默认的下载后台接口
            if (DownloadBackend == null)
                DownloadBackend = new UnityWebRequestBackend(WebRequestCreator);

            // 创建默认的 AssetBundle 加载工厂
            if (LoadAssetBundleFactory == null)
                LoadAssetBundleFactory = DefaultLoadAssetBundleOperationFactory;

            // 创建默认的 RawBundle 加载工厂
            if (LoadRawBundleFactory == null)
                LoadRawBundleFactory = DefaultLoadRawBundleOperationFactory;

            // 创建解压文件系统
            var remoteServices = new UnpackRemoteService(_packageRoot);
            _unpackFileSystem = new UnpackFileSystem();
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.REMOTE_SERVICES, remoteServices);
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.DOWNLOAD_BACKEND, DownloadBackend);
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.UNITY_WEB_REQUEST_CREATOR, WebRequestCreator);
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.INSTALL_CLEAR_MODE, InstallClearMode);
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.FILE_VERIFY_LEVEL, FileVerifyLevel);
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.FILE_VERIFY_MAX_CONCURRENCY, FileVerifyMaxConcurrency);
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.APPEND_FILE_EXTENSION, AppendFileExtension);
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.LOAD_ASSETBUNDLE_OPERATION_FACTORY, LoadAssetBundleFactory);
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.LOAD_RAWBUNDLE_OPERATION_FACTORY, LoadRawBundleFactory);
            _unpackFileSystem.SetParameter(FileSystemParametersDefine.COPY_LOCAL_FILE_SERVICES, CopyLocalFileServices);
            _unpackFileSystem.OnCreate(packageName, UnpackFileSystemRoot);
        }
        public virtual void OnDestroy()
        {
            if (_unpackFileSystem != null)
            {
                _unpackFileSystem.OnDestroy();
                _unpackFileSystem = null;
            }

            if (DownloadBackend != null)
            {
                DownloadBackend.Dispose();
                DownloadBackend = null;
            }
        }

        public virtual bool Belong(PackageBundle bundle)
        {
            if (DisableCatalogFile)
                return true;
            return _wrappers.ContainsKey(bundle.BundleGUID);
        }
        public virtual bool Exists(PackageBundle bundle)
        {
            if (DisableCatalogFile)
                return true;
            return _wrappers.ContainsKey(bundle.BundleGUID);
        }
        public virtual bool NeedDownload(PackageBundle bundle)
        {
            return false;
        }
        public virtual bool NeedUnpack(PackageBundle bundle)
        {
            if (IsUnpackBundleFile(bundle))
            {
                return _unpackFileSystem.Exists(bundle) == false;
            }
            else
            {
                return false;
            }
        }
        public virtual bool NeedImport(PackageBundle bundle)
        {
            return false;
        }
        public virtual string GetBundleFilePath(PackageBundle bundle)
        {
            if (IsUnpackBundleFile(bundle))
            {
                return _unpackFileSystem.GetBundleFilePath(bundle);
            }

            return GetBuiltinFileLoadPath(bundle);
        }

        /// <summary>
        /// 是否属于解压资源包文件
        /// </summary>
        protected virtual bool IsUnpackBundleFile(PackageBundle bundle)
        {
            if (Belong(bundle) == false)
                return false;

#if UNITY_ANDROID || UNITY_OPENHARMONY
            if (bundle.Encrypted)
                return true;

            if (bundle.BundleType == (int)EBundleType.RawBundle)
                return true;

            return false;
#else
            return false;
#endif
        }

        #region 内部方法
        private LoadAssetBundleOperation DefaultLoadAssetBundleOperationFactory(bool bundleEncrypted, LoadAssetBundleOptions options)
        {
            if (bundleEncrypted)
            {
                string error = $"{nameof(DefaultLoadAssetBundleOperation)} cannot load encrypted bundle. Please provide a custom {nameof(LoadAssetBundleOperationFactory)}.";
                return new LoadAssetBundleCompleteOperation(error, options);
            }
            else
            {
                return new DefaultLoadAssetBundleOperation(options);
            }
        }
        private LoadRawBundleOperation DefaultLoadRawBundleOperationFactory(bool bundleEncrypted, LoadRawBundleOptions options)
        {
            if (bundleEncrypted)
            {
                string error = $"{nameof(DefaultLoadRawBundleOperation)} cannot load encrypted bundle. Please provide a custom {nameof(LoadRawBundleOperationFactory)}.";
                return new LoadRawBundleCompleteOperation(error, options);
            }
            else
            {
                return new DefaultLoadRawBundleOperation(options);
            }
        }
        protected string GetDefaultBuiltinPackageRoot(string packageName)
        {
            string rootDirectory = YooAssetSettingsData.GetYooDefaultBuildinRoot();
            return PathUtility.Combine(rootDirectory, packageName);
        }
        public string GetBuiltinFileLoadPath(PackageBundle bundle)
        {
            if (_builtinFilePathMapping.TryGetValue(bundle.BundleGUID, out string filePath) == false)
            {
                filePath = PathUtility.Combine(_packageRoot, bundle.FileName);
                _builtinFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }
        public string GetBuiltinPackageVersionFilePath()
        {
            string fileName = YooAssetSettingsData.GetPackageVersionFileName(PackageName);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        public string GetBuiltinPackageHashFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetPackageHashFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        public string GetBuiltinPackageManifestFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetManifestBinaryFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }
        public string GetCatalogBinaryFileLoadPath()
        {
            return PathUtility.Combine(_packageRoot, BuiltinFileSystemConstants.BuiltinCatalogBinaryFileName);
        }

        /// <summary>
        /// 记录文件信息
        /// </summary>
        public bool RecordCatalogFile(string bundleGUID, FileWrapper wrapper)
        {
            if (_wrappers.ContainsKey(bundleGUID))
            {
                YooLogger.Error($"{nameof(BuiltinFileSystem)} has element : {bundleGUID}");
                return false;
            }

            _wrappers.Add(bundleGUID, wrapper);
            return true;
        }

        /// <summary>
        /// 初始化解压文件系统
        /// </summary>
        public FSInitializeOperation InitializeUnpackFileSystem()
        {
            return _unpackFileSystem.InitializeAsync();
        }
        #endregion
    }
}
