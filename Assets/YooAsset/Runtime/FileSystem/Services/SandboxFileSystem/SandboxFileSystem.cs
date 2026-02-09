using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 沙盒文件系统
    /// </summary>
    internal class SandboxFileSystem : IFileSystem
    {
        protected readonly Dictionary<string, string> _tempFilePathMapping = new Dictionary<string, string>(10000);
        protected string _packageRoot;
        protected string _tempFilesRoot;
        protected string _cacheManifestFilesRoot;
        protected string _cacheBundleFilesRoot;

        /// <summary>
        /// 沙盒文件缓存系统
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
        /// 自定义参数：远程服务接口的实例类
        /// </summary>
        public IRemoteServices RemoteServices { get; private set; }

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
        /// 自定义参数：禁用边玩边下机制
        /// </summary>
        public bool DisableOnDemandDownload { get; private set; } = false;

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
        /// 自定义参数：下载任务的看门狗机制超时时间
        /// </summary>
        public int DownloadWatchdogTimeout { get; private set; } = 0;

        /// <summary>
        /// 自定义参数：启用断点续传的最小尺寸
        /// </summary>
        public long ResumeDownloadMinimumSize { get; private set; } = long.MaxValue;

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

        /// <summary>
        /// 自定义参数：下载重试判定策略
        /// </summary>
        public IDownloadRetryPolicy DownloadRetryPolicy { get; private set; }

        /// <summary>
        /// 自定义参数：URL 选择策略
        /// </summary>
        public IDownloadURLPolicy DownloadURLPolicy { get; private set; }
        #endregion

        public SandboxFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new SFSInitializeOperation(this);
            return operation;
        }
        public virtual FSRequestPackageVersionOperation RequestPackageVersionAsync(FSRequestPackageVersionOptions options)
        {
            var operation = new SFSRequestPackageVersionOperation(this, options.AppendTimeTicks, options.Timeout);
            return operation;
        }
        public virtual FSLoadPackageManifestOperation LoadPackageManifestAsync(FSLoadPackageManifestOptions options)
        {
            var operation = new SFSLoadPackageManifestOperation(this, options.PackageVersion, options.Timeout);
            return operation;
        }
        public virtual FSLoadPackageBundleOperation LoadPackageBundleAsync(FSLoadPackageBundleOptions options)
        {
            var operation = new SFSLoadPackageBundleOperation(this, options);
            return operation;
        }
        public virtual FSDownloadFileOperation DownloadFileAsync(FSDownloadFileOptions options)
        {
            var downloader = new SFSDownloadFileOperation(this, options);
            return downloader;
        }
        public virtual FSClearCacheOperation ClearCacheAsync(FSClearCacheOptions options)
        {
            if (options.ClearMode == EManifestClearMode.ClearAllManifestFiles.ToString())
            {
                var operation = new SFSClearAllCacheManifestOperation(this);
                return operation;
            }
            else if (options.ClearMode == EManifestClearMode.ClearUnusedManifestFiles.ToString())
            {
                var operation = new SFSClearUnusedCacheManifestOperation(this, options.Manifest);
                return operation;
            }
            else
            {
                var operation = new SFSClearCacheOperation(this, options);
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
            else if (name == FileSystemConsts.REMOTE_SERVICES)
            {
                RemoteServices = (IRemoteServices)value;
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
                if (convertValue > 32)
                {
                    YooLogger.Warning($"FILE_VERIFY_MAX_CONCURRENCY value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32                                                                            
                FileVerifyMaxConcurrency = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemConsts.DOWNLOAD_DISABLE_ONDEMAND)
            {
                DisableOnDemandDownload = Convert.ToBoolean(value);
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
            else if (name == FileSystemConsts.DOWNLOAD_WATCHDOG_TIMEOUT)
            {
                int convertValue = Convert.ToInt32(value);
                DownloadWatchdogTimeout = Mathf.Clamp(convertValue, 0, int.MaxValue);
            }
            else if (name == FileSystemConsts.DOWNLOAD_RESUME_MINIMUM_SIZE)
            {
                ResumeDownloadMinimumSize = Convert.ToInt64(value);
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

            if (string.IsNullOrEmpty(packageRoot))
                _packageRoot = GetDefaultCachePackageRoot(packageName);
            else
                _packageRoot = packageRoot;

            _cacheBundleFilesRoot = PathUtility.Combine(_packageRoot, SandboxFileSystemConsts.BundleFilesFolderName);
            _cacheManifestFilesRoot = PathUtility.Combine(_packageRoot, SandboxFileSystemConsts.ManifestFilesFolderName);
            _tempFilesRoot = PathUtility.Combine(_packageRoot, SandboxFileSystemConsts.TempFilesFolderName);

            // 创建默认的下载后台接口
            if (DownloadBackend == null)
                DownloadBackend = new UnityWebRequestBackend(WebRequestCreator);

            // 创建默认的下载重试策略
            if (DownloadRetryPolicy == null)
                DownloadRetryPolicy = new DefaultDownloadRetryPolicy();

            // 创建默认的 URL 选择策略
            if (DownloadURLPolicy == null)
                DownloadURLPolicy = new DefaultDownloadURLPolicy();

            // 创建文件缓存系统
            {
                var cacheConfig = new SandboxFileCache.CacheConfig();
                cacheConfig.FileVerifyMaxConcurrency = FileVerifyMaxConcurrency;
                cacheConfig.FileVerifyLevel = FileVerifyLevel;
                cacheConfig.AssetBundleDecryptor = AssetBundleDecryptor;
                cacheConfig.RawBundleDecryptor = RawBundleDecryptor;
                cacheConfig.AssetBundleFallbackDecryptor = AssetBundleFallbackDecryptor;
                FileCache = new SandboxFileCache(PackageName, _cacheBundleFilesRoot, cacheConfig);
            }
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
            // 注意：保底加载！
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
            if (Belong(bundle) == false)
                return false;

            return FileCache.IsCached(bundle.BundleGUID) == false;
        }

        #region 内部方法
        /// <summary>
        /// 获取默认的缓存包裹根目录
        /// </summary>
        public string GetDefaultCachePackageRoot(string packageName)
        {
            string rootDirectory = YooAssetSettingsData.GetYooDefaultCacheRoot();
            return PathUtility.Combine(rootDirectory, packageName);
        }

        /// <summary>
        /// 获取缓存清单文件的根目录
        /// </summary>
        public string GetCacheManifestFilesRoot()
        {
            return _cacheManifestFilesRoot;
        }

        /// <summary>
        /// 获取缓存包裹哈希文件路径
        /// </summary>
        public string GetCachePackageHashFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetPackageHashFileName(PackageName, packageVersion);
            return PathUtility.Combine(_cacheManifestFilesRoot, fileName);
        }

        /// <summary>
        /// 获取缓存包裹清单文件路径
        /// </summary>
        public string GetCachePackageManifestFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetManifestBinaryFileName(PackageName, packageVersion);
            return PathUtility.Combine(_cacheManifestFilesRoot, fileName);
        }

        /// <summary>
        /// 获取沙盒应用程序水印文件路径
        /// </summary>
        public string GetSandboxAppFootprintFilePath()
        {
            return PathUtility.Combine(_cacheManifestFilesRoot, SandboxFileSystemConsts.AppFootprintFileName);
        }

        /// <summary>
        /// 获取临时文件路径
        /// </summary>
        public string GetTempFilePath(PackageBundle bundle)
        {
            if (_tempFilePathMapping.TryGetValue(bundle.BundleGUID, out string filePath) == false)
            {
                filePath = PathUtility.Combine(_tempFilesRoot, bundle.BundleGUID);
                _tempFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 删除所有缓存的资源文件
        /// </summary>
        public void DeleteAllBundleFiles()
        {
            if (Directory.Exists(_cacheBundleFilesRoot))
            {
                Directory.Delete(_cacheBundleFilesRoot, true);
            }
        }

        /// <summary>
        /// 删除所有缓存的清单文件
        /// </summary>
        public void DeleteAllManifestFiles()
        {
            if (Directory.Exists(_cacheManifestFilesRoot))
            {
                Directory.Delete(_cacheManifestFilesRoot, true);
            }
        }

        /// <summary>
        /// 删除所有缓存的临时文件
        /// </summary>
        public void DeleteAllTempFiles()
        {
            if (Directory.Exists(_tempFilesRoot))
            {
                Directory.Delete(_tempFilesRoot, true);
            }
        }
        #endregion
    }
}

