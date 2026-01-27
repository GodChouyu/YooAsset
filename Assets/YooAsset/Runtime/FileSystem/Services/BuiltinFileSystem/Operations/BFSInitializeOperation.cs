
namespace YooAsset
{
    internal class BFSInitializeOperation : FSInitializeOperation
    {
        private enum ESteps
        {
            None,
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
        private CopyBuiltinPackageManifest _copyBuiltinPackageManifestOp;
        private ESteps _steps = ESteps.None;

        internal BFSInitializeOperation(BuiltinFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
#if UNITY_WEBGL
            _steps = ESteps.Done;
            Status = EOperationStatus.Failed;
            Error = $"{nameof(DefaultBuildinFileSystem)} is not support WEBGL platform.";
#else
            _steps = ESteps.CheckAppFootprint;
#endif
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckAppFootprint)
            {
                string footprintFilePath = _fileSystem.GetSandboxAppFootPrintFilePath();
                var appFootprint = new ApplicationFootprint(footprintFilePath);
                appFootprint.Load(_fileSystem.PackageName);

                // 如果水印发生变化，则说明覆盖安装后首次打开游戏
                if (appFootprint.IsDirty())
                {
                    if (_fileSystem.InstallClearMode == EInstallCleanupMode.None)
                    {
                        YooLogger.Warning("Do nothing when overwrite install application.");
                    }
                    else if (_fileSystem.InstallClearMode == EInstallCleanupMode.ClearAllCacheFiles)
                    {
                        _fileSystem.DeleteAllBundleFiles();
                        YooLogger.Warning("Delete all cache files when overwrite install application.");
                    }
                    else if (_fileSystem.InstallClearMode == EInstallCleanupMode.ClearAllBundleFiles)
                    {
                        _fileSystem.DeleteAllBundleFiles();
                        YooLogger.Warning("Delete all bundle files when overwrite install application.");
                    }
                    else if (_fileSystem.InstallClearMode == EInstallCleanupMode.ClearAllManifestFiles)
                    {
                        YooLogger.Warning("Do nothing when overwrite install application.");
                    }
                    else
                    {
                        throw new System.NotImplementedException(_fileSystem.InstallClearMode.ToString());
                    }

                    appFootprint.Coverage(_fileSystem.PackageName);
                }

                _steps = ESteps.CopyPackageManifest;
            }

            if (_steps == ESteps.CopyPackageManifest)
            {
                if (_fileSystem.CopyBuildinPackageManifest)
                {
                    if (_copyBuiltinPackageManifestOp == null)
                    {
                        _copyBuiltinPackageManifestOp = new CopyBuiltinPackageManifest(_fileSystem);
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