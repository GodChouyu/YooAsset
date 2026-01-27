
namespace YooAsset
{
    internal class BFSClearCacheOperation : FSClearCacheOperation
    {
        private enum ESteps
        {
            None,
            ClearCache,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private readonly ClearCacheOptions _options;
        private FCClearCacheOperation _clearCacheOp;
        private ESteps _steps = ESteps.None;

        internal BFSClearCacheOperation(BuiltinFileSystem fileSystem, ClearCacheOptions options)
        {
            _fileSystem = fileSystem;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.ClearCache;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.ClearCache)
            {
                if (_clearCacheOp == null)
                {
                    _clearCacheOp = _fileSystem.UnpackFileCache.ClearCacheAsync(_options);
                    _clearCacheOp.StartOperation();
                    AddChildOperation(_clearCacheOp);
                }

                _clearCacheOp.UpdateOperation();
                if (_clearCacheOp.IsDone == false)
                    return;

                if (_clearCacheOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _clearCacheOp.Error;
                }
            }
        }
    }
}