
namespace YooAsset
{
    /// <summary>
    /// 内置文件系统的初始化操作
    /// </summary>
    internal class BFSInitializeOperation : FSInitializeOperation
    {
        private enum ESteps
        {
            None,
            CheckPlatform,
            CheckAppFootprint,
            CopyPackageManifest,
            InitializeBuiltinFileCache,
            InitializeUnpackFileCache,
            CreateScheduler,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private FCInitializeOperation _initializeBuiltinFileCacheOp;
        private FCInitializeOperation _initializeUnpackFileCacheOp;
        private CopyBuiltinPackageManifestOperation _copyBuiltinPackageManifestOp;
        private ESteps _steps = ESteps.None;

        internal BFSInitializeOperation(BuiltinFileSystem fileSystem)
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
                Error = $"{nameof(BuiltinFileSystem)} does not support the WebGL platform.";
#else
                _steps = ESteps.CheckAppFootprint;
#endif
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
                        _fileSystem.DeleteAllTempFIles();
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

                _steps = ESteps.CopyPackageManifest;
            }

            if (_steps == ESteps.CopyPackageManifest)
            {
                if (_fileSystem.CopyBuiltinPackageManifest)
                {
                    if (_copyBuiltinPackageManifestOp == null)
                    {
                        _copyBuiltinPackageManifestOp = new CopyBuiltinPackageManifestOperation(_fileSystem);
                        _copyBuiltinPackageManifestOp.StartOperation();
                        AddChildOperation(_copyBuiltinPackageManifestOp);
                    }

                    _copyBuiltinPackageManifestOp.UpdateOperation();
                    if (_copyBuiltinPackageManifestOp.IsDone == false)
                        return;

                    if (_copyBuiltinPackageManifestOp.Status == EOperationStatus.Succeeded)
                    {
                        _steps = ESteps.InitializeBuiltinFileCache;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _copyBuiltinPackageManifestOp.Error;
                    }
                }
                else
                {
                    _steps = ESteps.InitializeBuiltinFileCache;
                }
            }

            if (_steps == ESteps.InitializeBuiltinFileCache)
            {
                if (_initializeBuiltinFileCacheOp == null)
                {
                    _initializeBuiltinFileCacheOp = _fileSystem.BuiltinFileCache.InitializeAsync();
                    _initializeBuiltinFileCacheOp.StartOperation();
                    AddChildOperation(_initializeBuiltinFileCacheOp);
                }

                _initializeBuiltinFileCacheOp.UpdateOperation();
                Progress = _initializeBuiltinFileCacheOp.Progress;
                if (_initializeBuiltinFileCacheOp.IsDone == false)
                    return;

                if (_initializeBuiltinFileCacheOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.InitializeUnpackFileCache;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _initializeBuiltinFileCacheOp.Error;
                }
            }

            if (_steps == ESteps.InitializeUnpackFileCache)
            {
                if (_initializeUnpackFileCacheOp == null)
                {
                    _initializeUnpackFileCacheOp = _fileSystem.UnpackFileCache.InitializeAsync();
                    _initializeUnpackFileCacheOp.StartOperation();
                    AddChildOperation(_initializeUnpackFileCacheOp);
                }

                _initializeUnpackFileCacheOp.UpdateOperation();
                Progress = _initializeUnpackFileCacheOp.Progress;
                if (_initializeUnpackFileCacheOp.IsDone == false)
                    return;

                if (_initializeUnpackFileCacheOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.CreateScheduler;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _initializeUnpackFileCacheOp.Error;
                }
            }

            if (_steps == ESteps.CreateScheduler)
            {
                // 注意: 下载调度中心在最后一步创建，防止初始化失败后残留任务。
                // 注意: 下载调度中心作为独立任务运行！
                if (_fileSystem.UnpackScheduler == null)
                {
                    var schedulerConfig = new DownloadSchedulerOperation.SchedulerConfig();
                    schedulerConfig.SchedulerName = _fileSystem.GetType().Name;
                    schedulerConfig.DownloadBackend = _fileSystem.DownloadBackend;
                    schedulerConfig.MaxConcurrency = _fileSystem.UnpackMaxConcurrency;
                    schedulerConfig.MaxRequestPerFrame = _fileSystem.UnpackMaxRequestPerFrame;
                    _fileSystem.UnpackScheduler = new DownloadSchedulerOperation(schedulerConfig);
                    AsyncOperationSystem.StartOperation(_fileSystem.PackageName, _fileSystem.UnpackScheduler);
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
    }
}