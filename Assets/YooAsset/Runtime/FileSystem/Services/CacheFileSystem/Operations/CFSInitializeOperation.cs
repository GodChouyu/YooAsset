
namespace YooAsset
{
    internal class CFSInitializeOperation : FSInitializeOperation
    {
        private enum ESteps
        {
            None,
            CheckAppFootPrint,
            CacheInitialize,
            CreateDownloadScheduler,
            Done,
        }

        private readonly CacheFileSystem _fileSystem;
        private FCInitializeOperation _initializeCacheOp;
        private ESteps _steps = ESteps.None;


        internal CFSInitializeOperation(CacheFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
#if UNITY_WEBGL
            _steps = ESteps.Done;
            Status = EOperationStatus.Failed;
            Error = $"{nameof(DefaultCacheFileSystem)} is not support WEBGL platform.";
#else
            _steps = ESteps.CheckAppFootPrint;
#endif
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckAppFootPrint)
            {
                var appFootPrint = new ApplicationFootprint(_fileSystem);
                appFootPrint.Load(_fileSystem.PackageName);

                // 如果水印发生变化，则说明覆盖安装后首次打开游戏
                if (appFootPrint.IsDirty())
                {
                    if (_fileSystem.InstallClearMode == EOverwriteInstallClearMode.None)
                    {
                        YooLogger.Warning("Do nothing when overwrite install application.");
                    }
                    else if (_fileSystem.InstallClearMode == EOverwriteInstallClearMode.ClearAllCacheFiles)
                    {
                        _fileSystem.DeleteAllBundleFiles();
                        _fileSystem.DeleteAllManifestFiles();
                        YooLogger.Warning("Delete all cache files when overwrite install application.");
                    }
                    else if (_fileSystem.InstallClearMode == EOverwriteInstallClearMode.ClearAllBundleFiles)
                    {
                        _fileSystem.DeleteAllBundleFiles();
                        YooLogger.Warning("Delete all bundle files when overwrite install application.");
                    }
                    else if (_fileSystem.InstallClearMode == EOverwriteInstallClearMode.ClearAllManifestFiles)
                    {
                        _fileSystem.DeleteAllManifestFiles();
                        YooLogger.Warning("Delete all manifest files when overwrite install application.");
                    }
                    else
                    {
                        throw new System.NotImplementedException(_fileSystem.InstallClearMode.ToString());
                    }

                    appFootPrint.Coverage(_fileSystem.PackageName);
                }

                _steps = ESteps.CacheInitialize;
            }

            if (_steps == ESteps.CacheInitialize)
            {
                if (_initializeCacheOp == null)
                {
                    var options = new FCInitializeOptions();
                    options.FileVerifyLevel = _fileSystem.FileVerifyLevel;
                    options.FileVerifyMaxConcurrency = _fileSystem.FileVerifyMaxConcurrency;
                    _initializeCacheOp = _fileSystem.Cache.InitializeAsync(options);
                    _initializeCacheOp.StartOperation();
                    AddChildOperation(_initializeCacheOp);
                }

                _initializeCacheOp.UpdateOperation();
                Progress = _initializeCacheOp.Progress;
                if (_initializeCacheOp.IsDone == false)
                    return;

                if (_initializeCacheOp.Status != EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _initializeCacheOp.Error;
                }
                else
                {
                    _steps = ESteps.CreateDownloadScheduler;
                }
            }

            if (_steps == ESteps.CreateDownloadScheduler)
            {
                // 注意：下载中心作为独立任务运行！
                if (_fileSystem.DownloadScheduler == null)
                {
                    _fileSystem.DownloadScheduler = new DownloadSchedulerOperation(_fileSystem);
                    AsyncOperationSystem.StartOperation(_fileSystem.PackageName, _fileSystem.DownloadScheduler);
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
    }
}