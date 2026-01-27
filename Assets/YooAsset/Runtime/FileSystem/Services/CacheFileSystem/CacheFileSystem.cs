using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 缓存文件系统
    /// 说明：正在进行的下载器会在ResourcePackage销毁的时候执行Abort操作！
    /// </summary>
    internal class CacheFileSystem : IFileSystem
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
        public UnityWebRequestCreator WebRequestCreator { private set; get; }

        /// <summary>
        /// 自定义参数：远程服务接口的实例类
        /// </summary>
        public IRemoteServices RemoteServices { private set; get; }

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
        /// 默认值：8（推荐范围 1-32）
        /// 说明：过大的值可能导致线程池任务过多，影响系统稳定性 
        /// </summary>
        public int FileVerifyMaxConcurrency { private set; get; } = 8;

        /// <summary>
        /// 自定义参数：禁用边玩边下机制
        /// </summary>
        public bool DisableOnDemandDownload { private set; get; } = false;

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
        /// 自定义参数：下载任务的看门狗机制超时时间
        /// </summary>
        public int DownloadWatchDogTimeout { private set; get; } = 0;

        /// <summary>
        /// 自定义参数：启用断点续传的最小尺寸
        /// </summary>
        public long ResumeDownloadMinimumSize { private set; get; } = long.MaxValue;

        /// <summary>
        /// 自定义参数：断点续传下载器关注的错误码
        /// </summary>
        public List<long> ResumeDownloadResponseCodes { private set; get; } = null;

        /// <summary>
        /// 自定义参数：资源清单服务类
        /// </summary>
        public IManifestRestoreServices ManifestRestoreServices { private set; get; }
        #endregion

        public CacheFileSystem()
        {
        }
        public virtual FSInitializeOperation InitializeAsync()
        {
            var operation = new CFSInitializeOperation(this);
            return operation;
        }
        public virtual FSRequestVersionOperation RequestVersionAsync(RequestVersionOptions options)
        {
            var operation = new CFSRequestPackageVersionOperation(this, options.AppendTimeTicks, options.Timeout);
            return operation;
        }
        public virtual FSLoadManifestOperation LoadManifestAsync(LoadManifestOptions options)
        {
            var operation = new CFSLoadPackageManifestOperation(this, options.PackageVersion, options.Timeout);
            return operation;
        }
        public virtual FSClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            if (options.ClearMode == EManifestClearMode.ClearAllManifestFiles.ToString())
            {
                var operation = new CFSClearAllCacheManifestOperation(this);
                return operation;
            }
            else if (options.ClearMode == EManifestClearMode.ClearUnusedManifestFiles.ToString())
            {
                var operation = new CFSClearUnusedCacheManifestOperation(this, options.Manifest);
                return operation;
            }
            else
            {
                var operation = new CFSClearCacheOperation(this, options);
                return operation;
            }
        }
        public virtual FSDownloadFileOperation DownloadFileAsync(DownloadFileOptions options)
        {
            var downloader = new CFSDownloadFileOperation(this, options);
            return downloader;
        }
        public virtual FSLoadBundleOperation LoadBundleAsync(LoadBundleOptions options)
        {
            var operation = new CFSLoadBundleOperation(this, options);
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
            else if (name == FileSystemParametersDefine.REMOTE_SERVICES)
            {
                RemoteServices = (IRemoteServices)value;
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
                if (convertValue > 32)
                {
                    YooLogger.Warning($"FILE_VERIFY_MAX_CONCURRENCY value {convertValue} is too large, clamped to 32. Recommended range: 1 - 32.");
                }

                // 限制在合理范围内：1-32                                                                            
                FileVerifyMaxConcurrency = Mathf.Clamp(convertValue, 1, 32);
            }
            else if (name == FileSystemParametersDefine.DISABLE_ONDEMAND_DOWNLOAD)
            {
                DisableOnDemandDownload = Convert.ToBoolean(value);
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
            else if (name == FileSystemParametersDefine.DOWNLOAD_WATCH_DOG_TIME)
            {
                int convertValue = Convert.ToInt32(value);
                DownloadWatchDogTimeout = Mathf.Clamp(convertValue, 0, int.MaxValue);
            }
            else if (name == FileSystemParametersDefine.RESUME_DOWNLOAD_MINMUM_SIZE)
            {
                ResumeDownloadMinimumSize = Convert.ToInt64(value);
            }
            else if (name == FileSystemParametersDefine.RESUME_DOWNLOAD_RESPONSE_CODES)
            {
                ResumeDownloadResponseCodes = (List<long>)value;
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
                _packageRoot = GetDefaultCachePackageRoot(packageName);
            else
                _packageRoot = packageRoot;

            _cacheBundleFilesRoot = PathUtility.Combine(_packageRoot, DefaultCacheFileSystemDefine.BundleFilesFolderName);
            _cacheManifestFilesRoot = PathUtility.Combine(_packageRoot, DefaultCacheFileSystemDefine.ManifestFilesFolderName);
            _tempFilesRoot = PathUtility.Combine(_packageRoot, DefaultCacheFileSystemDefine.TempFilesFolderName);

            // 创建文件缓存系统
            {
                var cacheConfig = new SandboxFileCache.CacheConfig();
                cacheConfig.FileVerifyLevel = FileVerifyLevel;
                cacheConfig.FileVerifyMaxConcurrency = FileVerifyMaxConcurrency;
                FileCache = new SandboxFileCache(PackageName, _cacheBundleFilesRoot, cacheConfig);
            }

            // 创建默认的下载后台接口
            if (DownloadBackend == null)
                DownloadBackend = new UnityWebRequestBackend(WebRequestCreator);
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
            // 注意：沙盒文件系统保底加载！
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
        public string GetDefaultCachePackageRoot(string packageName)
        {
            string rootDirectory = YooAssetSettingsData.GetYooDefaultCacheRoot();
            return PathUtility.Combine(rootDirectory, packageName);
        }
        public string GetCacheManifestFilesRoot()
        {
            return _cacheManifestFilesRoot;
        }
        public string GetCachePackageHashFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetPackageHashFileName(PackageName, packageVersion);
            return PathUtility.Combine(_cacheManifestFilesRoot, fileName);
        }
        public string GetCachePackageManifestFilePath(string packageVersion)
        {
            string fileName = YooAssetSettingsData.GetManifestBinaryFileName(PackageName, packageVersion);
            return PathUtility.Combine(_cacheManifestFilesRoot, fileName);
        }
        public string GetSandboxAppFootPrintFilePath()
        {
            return PathUtility.Combine(_cacheManifestFilesRoot, DefaultCacheFileSystemDefine.AppFootPrintFileName);
        }
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
        #endregion
    }
}

