
namespace YooAsset
{
    /// <summary>
    /// 沙盒文件系统的初始化操作
    /// </summary>
    internal class SFSInitializeOperation : FSInitializeOperation
    {
        private enum ESteps
        {
            None,
            CheckPlatform,
            CheckParameter,
            CheckAppFootprint,
            InitializeFileCache,
            CreateScheduler,
            Done,
        }

        private readonly SandboxFileSystem _fileSystem;
        private FCInitializeOperation _initializeFileCacheOp;
        private ESteps _steps = ESteps.None;


        internal SFSInitializeOperation(SandboxFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckPlatform;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckPlatform)
            {
#if UNITY_WEBGL
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = $"{nameof(SandboxFileSystem)} does not support the WebGL platform.";
#else
                _steps = ESteps.CheckParameter;
#endif
            }

            if (_steps == ESteps.CheckParameter)
            {
                if (_fileSystem.RemoteServices == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"{nameof(IRemoteServices)} is null.";
                    return;
                }

                _steps = ESteps.CheckAppFootprint;
            }

            if (_steps == ESteps.CheckAppFootprint)
            {
                string footprintFilePath = _fileSystem.GetSandboxAppFootprintFilePath();
                var appFootprint = new ApplicationFootprint(footprintFilePath);
                appFootprint.Load(_fileSystem.PackageName);

                // 如果水印发生变化，则说明覆盖安装后首次打开游戏
                if (appFootprint.IsDirty())
                {
                    if (_fileSystem.InstallCleanupMode == EInstallCleanupMode.None)
                    {
                        YooLogger.Warning("No action required on overwrite installation.");
                    }
                    else if (_fileSystem.InstallCleanupMode == EInstallCleanupMode.ClearAllCacheFiles)
                    {
                        _fileSystem.DeleteAllBundleFiles();
                        _fileSystem.DeleteAllManifestFiles();
                        _fileSystem.DeleteAllTempFiles();
                        YooLogger.Warning("Deleted all cache files on overwrite installation.");
                    }
                    else if (_fileSystem.InstallCleanupMode == EInstallCleanupMode.ClearAllBundleFiles)
                    {
                        _fileSystem.DeleteAllBundleFiles();
                        YooLogger.Warning("Deleted all bundle files on overwrite installation.");
                    }
                    else if (_fileSystem.InstallCleanupMode == EInstallCleanupMode.ClearAllManifestFiles)
                    {
                        _fileSystem.DeleteAllManifestFiles();
                        YooLogger.Warning("Deleted all manifest files on overwrite installation.");
                    }
                    else
                    {
                        throw new System.NotImplementedException(_fileSystem.InstallCleanupMode.ToString());
                    }

                    appFootprint.Overwrite(_fileSystem.PackageName);
                }

                _steps = ESteps.InitializeFileCache;
            }

            if (_steps == ESteps.InitializeFileCache)
            {
                if (_initializeFileCacheOp == null)
                {
                    _initializeFileCacheOp = _fileSystem.FileCache.InitializeAsync();
                    _initializeFileCacheOp.StartOperation();
                    AddChildOperation(_initializeFileCacheOp);
                }

                _initializeFileCacheOp.UpdateOperation();
                Progress = _initializeFileCacheOp.Progress;
                if (_initializeFileCacheOp.IsDone == false)
                    return;

                if (_initializeFileCacheOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.CreateScheduler;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _initializeFileCacheOp.Error;
                }
            }

            if (_steps == ESteps.CreateScheduler)
            {
                // 注意: 下载调度中心在最后一步创建，防止初始化失败后残留任务。
                // 注意: 下载调度中心作为独立任务运行！
                if (_fileSystem.DownloadScheduler == null)
                {
                    var schedulerConfig = new DownloadSchedulerOperation.SchedulerConfig();
                    schedulerConfig.SchedulerName = _fileSystem.GetType().Name;
                    schedulerConfig.DownloadBackend = _fileSystem.DownloadBackend;
                    schedulerConfig.MaxConcurrency = _fileSystem.DownloadMaxConcurrency;
                    schedulerConfig.MaxRequestPerFrame = _fileSystem.DownloadMaxRequestPerFrame;
                    _fileSystem.DownloadScheduler = new DownloadSchedulerOperation(schedulerConfig);
                    AsyncOperationSystem.StartOperation(_fileSystem.PackageName, _fileSystem.DownloadScheduler);
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
    }
}