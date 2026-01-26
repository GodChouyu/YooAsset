
namespace YooAsset
{
    internal class FCInitializeOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            SearchCacheFiles,
            VerifyCacheFiles,
            Done,
        }

        private readonly BundleCache _cache;
        private readonly FCInitializeOptions _options;
        private SearchCacheFilesOperation _searchCacheFilesOp;
        private VerifyCacheFilesOperation _verifyCacheFilesOp;
        private ESteps _steps = ESteps.None;

        public FCInitializeOperation(BundleCache cache, FCInitializeOptions options)
        {
            _cache = cache;
            _options = options;
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
                    _verifyCacheFilesOp = new VerifyCacheFilesOperation(_cache, _options.FileVerifyLevel, _options.FileVerifyMaxConcurrency, _searchCacheFilesOp.Result);
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
