
namespace YooAsset
{
    internal class SFCVerifyCacheOperation : FCVerifyCacheOperation
    {
        private enum ESteps
        {
            None,
            Check,
            VerifyFile,
            Done,
        }

        private readonly SandboxFileCache _fileCache;
        private readonly VerifyCacheOptions _options;
        private VerifyTempFileOperation _verifyOperation;
        private ESteps _steps = ESteps.None;

        public SFCVerifyCacheOperation(SandboxFileCache cache, VerifyCacheOptions options)
        {
            _fileCache = cache;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.Check;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.Check)
            {
                if (_fileCache.IsCached(_options.Bundle.BundleGUID) == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Not found cached bundle.";
                }
                else
                {
                    _steps = ESteps.VerifyFile;
                }
            }

            if (_steps == ESteps.VerifyFile)
            {
                if (_verifyOperation == null)
                {
                    var entry = _fileCache.GetEntry(_options.Bundle.BundleGUID);
                    var element = new TempFileInfo(entry.DataFilePath, _options.Bundle.FileCRC, _options.Bundle.FileSize);
                    _verifyOperation = new VerifyTempFileOperation(element);
                    _verifyOperation.StartOperation();
                    AddChildOperation(_verifyOperation);
                }

                if (IsWaitForCompletion)
                    _verifyOperation.WaitForCompletion();

                _verifyOperation.UpdateOperation();
                if (_verifyOperation.IsDone == false)
                    return;

                if (_verifyOperation.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _verifyOperation.Error;

                    if (_options.FailedDeleteCache)
                    {
                        YooLogger.Error($"Find corrupted bundle file and remove cache entry : {_options.Bundle.BundleGUID}");
                        _fileCache.RemoveEntry(_options.Bundle.BundleGUID);
                    }
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}
