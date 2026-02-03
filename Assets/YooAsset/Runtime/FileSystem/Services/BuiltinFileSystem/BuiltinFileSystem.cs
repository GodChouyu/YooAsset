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
        protected readonly Dictionary<string, string> _builtinFilePathMapping = new Dictionary<string, string>(10000);
        protected readonly Dictionary<string, string> _tempFilePathMapping = new Dictionary<string, string>(10000);
        protected string _packageRoot;
        protected string _unpackTempFilesRoot;
        protected string _unpackManifestFilesRoot;
        protected string _unpackBundleFilesRoot;

        /// <summary>
        /// 内置文件缓存系统
        /// </summary>
        public IFileCache BuiltinFileCache { private set; get; }

        /// <summary>
        /// 沙盒文件缓存系统
        /// </summary>
        public IFileCache UnpackFileCache { private set; get; }

        /// <summary>
        /// 解压调度器
        /// </summary>
        public DownloadSchedulerOperation UnpackScheduler { get; set; }

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
        /// 自定义参数：覆盖安装缓存清理模式
        /// </summary>
        public EInstallCleanupMode InstallClearMode { private set; get; } = EInstallCleanupMode.ClearAllManifestFiles;

        /// <summary>
        /// 自定义参数：初始化的时候缓存文件校验级别
        /// </summary>
        public EFileVerifyLevel FileVerifyLevel { private set; get; } = EFileVerifyLevel.Middle;

        /// <summary>
        /// 自定义参数：初始化的时候缓存文件校验最大并发数
        /// </summary>
        public int FileVerifyMaxConcurrency { private set; get; } = 32;

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
        /// 自定义参数：最大并发连接数
        /// 默认值：8（推荐范围 1-32）
        /// </summary>
        public int UnpackMaxConcurrency { private set; get; }

        /// <summary>
        /// 自定义参数：每帧发起的最大请求数
        /// 默认值：8（推荐范围 1-32） 
        /// 说明：避免单帧发起过多请求导致卡顿 
        /// </summary>
        public int UnpackMaxRequestPerFrame { private set; get; }

        /// <summary>
        /// 自定义参数：AssetBundle 解密器
        /// </summary>
        public IBundleDecryptor AssetBundleDecryptor { get; set; }

        /// <summary>
        /// 自定义参数：RawBundle 解密器
        /// </summary>
        public IBundleDecryptor RawBundleDecryptor { get; set; }

        /// <summary>
        /// 自定义参数：AssetBundle 备用解密器
        /// </summary>
        public IBundleMemoryDecryptor AssetBundleFallbackDecryptor { get; set; }

        /// <summary>
        /// 自定义参数：资源清单解密器
        /// </summary>
        public IManifestDecryptor ManifestDecryptor { private set; get; }
        #endregion


        public BuiltinFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new BFSInitializeOperation(this);
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
            var operation = new BFSClearCacheOperation(this, options);
            return operation;
        }
        public virtual FSDownloadFileOperation DownloadFileAsync(DownloadFileOptions options)
        {
            var operation = new BFSDownloadFileOperation(this, options);
            return operation;
        }
        public virtual FSLoadBundleOperation LoadBundleAsync(LoadBundleOptions options)
        {
            var operation = new BFSLoadBundleOperation(this, options);
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
            else if (name == FileSystemParametersDefine.INSTALL_CLEAR_MODE)
            {
                InstallClearMode = (EInstallCleanupMode)value;
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
            else if (name == FileSystemParametersDefine.DOWNLOAD_MAX_CONCURRENCY)
            {
                int convertValue = Convert.ToInt32(value);
                if (convertValue > 32)
                {
                    YooLogger.Warning($"DOWNLOAD_MAX_CONCURRENCY value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32          
                UnpackMaxConcurrency = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemParametersDefine.DOWNLOAD_MAX_REQUEST_PER_FRAME)
            {
                int convertValue = Convert.ToInt32(value);
                if (convertValue > 32)
                {
                    YooLogger.Warning($"DOWNLOAD_MAX_REQUEST_PER_FRAME value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32          
                UnpackMaxRequestPerFrame = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemParametersDefine.ASSETBUNDLE_DECRYPTOR)
            {
                AssetBundleDecryptor = (IBundleDecryptor)value;
            }
            else if (name == FileSystemParametersDefine.RAWBUNDLE_DECRYPTOR)
            {
                RawBundleDecryptor = (IBundleDecryptor)value;
            }
            else if (name == FileSystemParametersDefine.ASSETBUNDLE_FALLBACK_DECRYPTOR)
            {
                AssetBundleFallbackDecryptor = (IBundleMemoryDecryptor)value;
            }
            else if (name == FileSystemParametersDefine.MANIFEST_DECRYPTOR)
            {
                ManifestDecryptor = (IManifestDecryptor)value;
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

            // 创建解压缓存系统
            string unpackRoot;
            if (string.IsNullOrEmpty(UnpackFileSystemRoot))
                unpackRoot = GetDefaultUnpackCacheRoot(packageName);
            else
                unpackRoot = UnpackFileSystemRoot;
            _unpackManifestFilesRoot = PathUtility.Combine(unpackRoot, BuiltinFileSystemDefine.UnpackManifestFilesFolderName);
            _unpackBundleFilesRoot = PathUtility.Combine(unpackRoot, BuiltinFileSystemDefine.UnpackBundleFilesFolderName);
            _unpackTempFilesRoot = PathUtility.Combine(unpackRoot, BuiltinFileSystemDefine.UnpackTempFilesFolderName);

            // 创建内置缓存对象
            {
                var cacheConfig = new BuiltinFileCache.CacheConfig();
                cacheConfig.AssetBundleDecryptor = AssetBundleDecryptor;
                cacheConfig.RawBundleDecryptor = RawBundleDecryptor;
                cacheConfig.DownloadBackend = DownloadBackend;
                BuiltinFileCache = new BuiltinFileCache(packageName, _packageRoot, cacheConfig);
            }

            // 创建沙盒缓存对象
            {
                var cacheConfig = new SandboxFileCache.CacheConfig();
                cacheConfig.FileVerifyMaxConcurrency = FileVerifyMaxConcurrency;
                cacheConfig.FileVerifyLevel = FileVerifyLevel;
                cacheConfig.AssetBundleDecryptor = AssetBundleDecryptor;
                cacheConfig.RawBundleDecryptor = RawBundleDecryptor;
                cacheConfig.AssetBundleFallbackDecryptor = AssetBundleFallbackDecryptor;
                UnpackFileCache = new SandboxFileCache(packageName, _unpackBundleFilesRoot, cacheConfig);
            }
        }
        public virtual void OnDestroy()
        {
            if (BuiltinFileCache != null)
            {
                BuiltinFileCache.Dispose();
                BuiltinFileCache = null;
            }

            if (UnpackFileCache != null)
            {
                UnpackFileCache.Dispose();
                UnpackFileCache = null;
            }

            if (UnpackScheduler != null)
            {
                UnpackScheduler.Dispose();
                UnpackScheduler = null;
            }

            if (DownloadBackend != null)
            {
                DownloadBackend.Dispose();
                DownloadBackend = null;
            }
        }

        public virtual bool Belong(PackageBundle bundle)
        {
            return BuiltinFileCache.IsCached(bundle.BundleGUID);
        }
        public virtual bool NeedDownload(PackageBundle bundle)
        {
            return false;
        }
        public virtual bool NeedUnpack(PackageBundle bundle)
        {
            if (IsUnpackBundleFile(bundle))
            {
                return UnpackFileCache.IsCached(bundle.BundleGUID) == false;
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

        /// <summary>
        /// 是否属于解压资源包文件
        /// </summary>
        public virtual bool IsUnpackBundleFile(PackageBundle bundle)
        {
            if (Belong(bundle) == false)
                return false;

#if UNITY_ANDROID || UNITY_OPENHARMONY
            if (bundle.IsEncrypted)
                return true;

            if (bundle.BundleType == (int)EBundleType.RawBundle)
                return true;

            return false;
#else
            return false;
#endif
        }

        #region 内部方法
        public string GetDefaultBuiltinPackageRoot(string packageName)
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
        public string GetSandboxAppFootPrintFilePath()
        {
            return PathUtility.Combine(_unpackManifestFilesRoot, SandboxFileSystemDefine.AppFootPrintFileName);
        }

        /// <summary>
        /// 删除所有缓存的资源文件
        /// </summary>
        public void DeleteAllBundleFiles()
        {
            if (Directory.Exists(_unpackBundleFilesRoot))
            {
                Directory.Delete(_unpackBundleFilesRoot, true);
            }
        }

        /// <summary>
        /// 获取默认的解压缓存根目录
        /// </summary>
        public string GetDefaultUnpackCacheRoot(string packageName)
        {
            string rootDirectory = YooAssetSettingsData.GetYooDefaultCacheRoot();
            return PathUtility.Combine(rootDirectory, packageName);
        }

        /// <summary>
        /// 获取解压的临时文件路径
        /// </summary>
        public string GetUnpackTempFilePath(PackageBundle bundle)
        {
            if (_tempFilePathMapping.TryGetValue(bundle.BundleGUID, out string filePath) == false)
            {
                filePath = PathUtility.Combine(_unpackTempFilesRoot, bundle.BundleGUID);
                _tempFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }
        #endregion
    }
}
