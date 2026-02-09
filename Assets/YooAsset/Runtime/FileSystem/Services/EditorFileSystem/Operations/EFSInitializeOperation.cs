
namespace YooAsset
{
    /// <summary>
    /// 编辑器文件系统的初始化操作
    /// </summary>
    internal class EFSInitializeOperation : FSInitializeOperation
    {
        private enum ESteps
        {
            None,
            CheckPlatform,
            InitializeFileCache,
            CreateScheduler,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private FCInitializeOperation _initializeFileCacheOp;
        private ESteps _steps = ESteps.None;

        internal EFSInitializeOperation(EditorFileSystem fileSystem)
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
#if !UNITY_EDITOR
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = $"{nameof(EditorFileSystem)} only support the Unity Editor.";
#else
                _steps = ESteps.InitializeFileCache;
#endif
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