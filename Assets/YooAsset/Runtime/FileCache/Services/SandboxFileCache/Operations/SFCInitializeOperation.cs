
namespace YooAsset
{
    internal class SFCInitializeOperation : FCInitializeOperation
    {
        private enum ESteps
        {
            None,
            SearchCacheFiles,
            VerifyCacheFiles,
            Done,
        }

        private readonly SandboxFileCache _cache;
        private SearchCacheFilesOperation _searchCacheFilesOp;
        private VerifyCacheFilesOperation _verifyCacheFilesOp;
        private ESteps _steps = ESteps.None;

        public SFCInitializeOperation(SandboxFileCache cache)
        {
            _cache = cache;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.SearchCacheFiles;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.SearchCacheFiles)
            {
                if (_searchCacheFilesOp == null)
                {
                    _searchCacheFilesOp = new SearchCacheFilesOperation(_cache);
                    _searchCacheFilesOp.StartOperation();
                    AddChildOperation(_searchCacheFilesOp);
                }

                _searchCacheFilesOp.UpdateOperation();
                Progress = _searchCacheFilesOp.Progress;
                if (_searchCacheFilesOp.IsDone == false)
                    return;

                _steps = ESteps.VerifyCacheFiles;
            }

            if (_steps == ESteps.VerifyCacheFiles)
            {
                if (_verifyCacheFilesOp == null)
                {
                    _verifyCacheFilesOp = new VerifyCacheFilesOperation(_cache, _cache.Config.FileVerifyLevel, _cache.Config.FileVerifyMaxConcurrency, _searchCacheFilesOp.Result);
                    _verifyCacheFilesOp.StartOperation();
                    AddChildOperation(_verifyCacheFilesOp);
                }

                _verifyCacheFilesOp.UpdateOperation();
                Progress = _verifyCacheFilesOp.Progress;
                if (_verifyCacheFilesOp.IsDone == false)
                    return;

                if (_verifyCacheFilesOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _verifyCacheFilesOp.Error;
                }
            }
        }
    }
}
