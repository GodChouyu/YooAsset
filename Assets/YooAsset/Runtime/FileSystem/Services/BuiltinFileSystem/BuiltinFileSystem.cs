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
        protected string _tempFilesRoot;
        protected string _unpackManifestFilesRoot;
        protected string _unpackBundleFilesRoot;

        /// <summary>
        /// 内置文件缓存系统
        /// </summary>
        public IFileCache BuiltinFileCache { get; private set; }

        /// <summary>
        /// 沙盒文件缓存系统
        /// </summary>
        public IFileCache UnpackFileCache { get; private set; }

        /// <summary>
        /// 解压调度器
        /// </summary>
        public DownloadSchedulerOperation UnpackScheduler { get; set; }

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
        /// 自定义参数：覆盖安装缓存清理模式
        /// </summary>
        public EInstallCleanupMode InstallCleanupMode { get; private set; } = EInstallCleanupMode.None;

        /// <summary>
        /// 自定义参数：初始化的时候缓存文件校验级别
        /// </summary>
        public EFileVerifyLevel FileVerifyLevel { get; private set; } = EFileVerifyLevel.Low;

        /// <summary>
        /// 自定义参数：初始化的时候缓存文件校验最大并发数
        /// 默认值：8（推荐值为处理器数两倍）
        /// 说明：过大的值可能导致线程池任务过多，影响系统稳定性 
        /// </summary>
        public int FileVerifyMaxConcurrency { get; private set; } = 8;

        /// <summary>
        /// 自定义参数：拷贝内置清单
        /// </summary>
        public bool CopyBuiltinPackageManifest { get; private set; } = false;

        /// <summary>
        /// 自定义参数：拷贝内置清单的目标目录
        /// 注意：该参数为空的时候，会获取默认的沙盒目录！
        /// </summary>
        public string CopyBuiltinPackageManifestDestRoot { get; private set; }

        /// <summary>
        /// 自定义参数：解压文件系统的根目录
        /// </summary>
        public string UnpackFileSystemRoot { get; private set; }

        /// <summary>
        /// 自定义参数：最大并发连接数
        /// 默认值：8（推荐范围 1-32）
        /// </summary>
        public int UnpackMaxConcurrency { get; private set; } = 8;

        /// <summary>
        /// 自定义参数：每帧发起的最大请求数
        /// 默认值：8（推荐范围 1-32） 
        /// 说明：避免单帧发起过多请求导致卡顿 
        /// </summary>
        public int UnpackMaxRequestPerFrame { get; private set; } = 8;

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
        public IManifestDecryptor ManifestDecryptor { get; private set; }
        #endregion


        public BuiltinFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new BFSInitializeOperation(this);
            return operation;
        }
        public virtual FSRequestPackageVersionOperation RequestPackageVersionAsync(FSRequestPackageVersionOptions options)
        {
            var operation = new BFSRequestPackageVersionOperation(this);
            return operation;
        }
        public virtual FSLoadPackageManifestOperation LoadPackageManifestAsync(FSLoadPackageManifestOptions options)
        {
            var operation = new BFSLoadPackageManifestOperation(this, options.PackageVersion);
            return operation;
        }
        public virtual FSLoadPackageBundleOperation LoadPackageBundleAsync(FSLoadPackageBundleOptions options)
        {
            var operation = new BFSLoadPackageBundleOperation(this, options);
            return operation;
        }
        public virtual FSDownloadFileOperation DownloadFileAsync(FSDownloadFileOptions options)
        {
            var operation = new BFSDownloadFileOperation(this, options);
            return operation;
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
                var operation = new BFSClearCacheOperation(this, options);
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
            else if (name == FileSystemConsts.INSTALL_CLEANUP_MODE)
            {
                InstallCleanupMode = (EInstallCleanupMode)value;
            }
            else if (name == FileSystemConsts.FILE_VERIFY_LEVEL)
            {
                FileVerifyLevel = (EFileVerifyLevel)value;
            }
            else if (name == FileSystemConsts.FILE_VERIFY_MAX_CONCURRENCY)
            {
                int convertValue = Convert.ToInt32(value);
                FileVerifyMaxConcurrency = Mathf.Clamp(convertValue, 1, int.MaxValue);
            }
            else if (name == FileSystemConsts.COPY_BUILTIN_PACKAGE_MANIFEST)
            {
                CopyBuiltinPackageManifest = Convert.ToBoolean(value);
            }
            else if (name == FileSystemConsts.COPY_BUILTIN_PACKAGE_MANIFEST_DEST_ROOT)
            {
                CopyBuiltinPackageManifestDestRoot = (string)value;
            }
            else if (name == FileSystemConsts.UNPACK_FILE_SYSTEM_ROOT)
            {
                UnpackFileSystemRoot = (string)value;
            }
            else if (name == FileSystemConsts.DOWNLOAD_MAX_CONCURRENCY)
            {
                int convertValue = Convert.ToInt32(value);
                if (convertValue > 32)
                {
                    YooLogger.Warning($"DOWNLOAD_MAX_CONCURRENCY value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32          
                UnpackMaxConcurrency = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemConsts.DOWNLOAD_MAX_REQUEST_PER_FRAME)
            {
                int convertValue = Convert.ToInt32(value);
                if (convertValue > 32)
                {
                    YooLogger.Warning($"DOWNLOAD_MAX_REQUEST_PER_FRAME value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32          
                UnpackMaxRequestPerFrame = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemConsts.ASSETBUNDLE_DECRYPTOR)
            {
                AssetBundleDecryptor = (IBundleDecryptor)value;
            }
            else if (name == FileSystemConsts.RAWBUNDLE_DECRYPTOR)
            {
                RawBundleDecryptor = (IBundleDecryptor)value;
            }
            else if (name == FileSystemConsts.ASSETBUNDLE_FALLBACK_DECRYPTOR)
            {
                AssetBundleFallbackDecryptor = (IBundleMemoryDecryptor)value;
            }
            else if (name == FileSystemConsts.MANIFEST_DECRYPTOR)
            {
                ManifestDecryptor = (IManifestDecryptor)value;
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
                _packageRoot = GetDefaultBuiltinPackageRoot(packageName);
            else
                _packageRoot = packageRoot;

            // 设置根目录
            string unpackRoot;
            if (string.IsNullOrEmpty(UnpackFileSystemRoot))
                unpackRoot = GetDefaultUnpackPackageRoot(packageName);
            else
                unpackRoot = UnpackFileSystemRoot;
            _unpackManifestFilesRoot = PathUtility.Combine(unpackRoot, BuiltinFileSystemConsts.UnpackManifestFilesFolderName);
            _unpackBundleFilesRoot = PathUtility.Combine(unpackRoot, BuiltinFileSystemConsts.UnpackBundleFilesFolderName);
            _tempFilesRoot = PathUtility.Combine(unpackRoot, BuiltinFileSystemConsts.UnpackTempFilesFolderName);

            // 创建默认的下载后台接口
            if (DownloadBackend == null)
                DownloadBackend = new UnityWebRequestBackend(WebRequestCreator);

            // 创建内置文件缓存系统
            {
                var cacheConfig = new BuiltinFileCache.CacheConfig();
                cacheConfig.AssetBundleDecryptor = AssetBundleDecryptor;
                cacheConfig.RawBundleDecryptor = RawBundleDecryptor;
                cacheConfig.DownloadBackend = DownloadBackend;
                BuiltinFileCache = new BuiltinFileCache(packageName, _packageRoot, cacheConfig);
            }

            // 创建沙盒文件缓存系统
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
                UnpackScheduler.AbortOperation();
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
        /// <summary>
        /// 获取默认的内置包裹根目录
        /// </summary>
        public string GetDefaultBuiltinPackageRoot(string packageName)
        {
            string rootDirectory = YooAssetSettingsData.GetYooDefaultBuiltinRoot();
            return PathUtility.Combine(rootDirectory, packageName);
        }

        /// <summary>
        /// 获取内置文件路径
        /// </summary>
        public string GetBuiltinBundleFilePath(PackageBundle bundle)
        {
            if (_builtinFilePathMapping.TryGetValue(bundle.BundleGUID, out string filePath) == false)
            {
                filePath = PathUtility.Combine(_packageRoot, bundle.FileName);
                _builtinFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 获取内置包裹版本文件路径
        /// </summary>
        public string GetBuiltinPackageVersionFilePath()
        {
            string fileName = YooAssetSettingsData.GetPackageVersionFileName(PackageName);
            return PathUtility.Combine(_packageRoot, fileName);
        }

        /// <summary>
        /// 获取内置包裹哈希文件路径
        /// </summary>
        public string GetBuiltinPackageHashFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetPackageHashFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }

        /// <summary>
        /// 获取内置包裹清单文件路径
        /// </summary>
        public string GetBuiltinPackageManifestFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetManifestBinaryFileName(PackageName, packageVersion);
            return PathUtility.Combine(_packageRoot, fileName);
        }

        /// <summary>
        /// 获取沙盒应用程序水印文件路径
        /// </summary>
        public string GetSandboxAppFootprintFilePath()
        {
            return PathUtility.Combine(_unpackManifestFilesRoot, SandboxFileSystemConsts.AppFootprintFileName);
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
        /// 删除所有缓存的清单文件
        /// </summary>
        public void DeleteAllManifestFiles()
        {
            if (Directory.Exists(_unpackManifestFilesRoot))
            {
                Directory.Delete(_unpackManifestFilesRoot, true);
            }
        }

        /// <summary>
        /// 删除所有缓存的临时文件
        /// </summary>
        public void DeleteAllTempFIles()
        {
            if (Directory.Exists(_tempFilesRoot))
            {
                Directory.Delete(_tempFilesRoot, true);
            }
        }

        /// <summary>
        /// 获取默认的解压根目录
        /// </summary>
        public string GetDefaultUnpackPackageRoot(string packageName)
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
                filePath = PathUtility.Combine(_tempFilesRoot, bundle.BundleGUID);
                _tempFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }
        #endregion
    }
}
