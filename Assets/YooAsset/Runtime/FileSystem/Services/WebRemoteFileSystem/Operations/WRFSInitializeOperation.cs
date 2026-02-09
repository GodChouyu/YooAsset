
namespace YooAsset
{
    /// <summary>
    /// Web远端文件系统的初始化操作
    /// </summary>
    internal class WRFSInitializeOperation : FSInitializeOperation
    {
        private enum ESteps
        {
            None,
            CheckPlatform,
            CheckParameter,
            InitializeFileCache,
            Done,
        }

        private readonly WebRemoteFileSystem _fileSystem;
        private FCInitializeOperation _initializeFileCacheOp;
        private ESteps _steps = ESteps.None;

        public WRFSInitializeOperation(WebRemoteFileSystem fileSystem)
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
#if !UNITY_WEBGL
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = $"{nameof(WebRemoteFileSystem)} only support the WebGL platform.";
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
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _initializeFileCacheOp.Error;
                }
            }
        }
    }
}