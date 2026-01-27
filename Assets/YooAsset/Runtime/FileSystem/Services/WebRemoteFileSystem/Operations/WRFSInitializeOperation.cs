
namespace YooAsset
{
    internal class WRFSInitializeOperation : FSInitializeOperation
    {
        private enum ESteps
        {
            None,
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
            _steps = ESteps.InitializeFileCache;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

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