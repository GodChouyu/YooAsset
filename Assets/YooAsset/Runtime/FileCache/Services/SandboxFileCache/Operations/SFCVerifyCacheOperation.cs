
namespace YooAsset
{
    /// <summary>
    /// 沙盒文件缓存验证操作
    /// </summary>
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
        private readonly FCVerifyCacheOptions _options;
        private VerifyTempFileOperation _verifyTempFileOp;
        private ESteps _steps = ESteps.None;

        public SFCVerifyCacheOperation(SandboxFileCache cache, FCVerifyCacheOptions options)
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
                    Error = "Cached bundle not found.";
                }
                else
                {
                    _steps = ESteps.VerifyFile;
                }
            }

            if (_steps == ESteps.VerifyFile)
            {
                if (_verifyTempFileOp == null)
                {
                    var entry = _fileCache.GetEntry(_options.Bundle.BundleGUID);
                    var element = new TempFileInfo(entry.DataFilePath, _options.Bundle.FileCRC, _options.Bundle.FileSize);
                    _verifyTempFileOp = new VerifyTempFileOperation(element);
                    _verifyTempFileOp.StartOperation();
                    AddChildOperation(_verifyTempFileOp);
                }

                if (IsWaitForCompletion)
                    _verifyTempFileOp.WaitForCompletion();

                _verifyTempFileOp.UpdateOperation();
                if (_verifyTempFileOp.IsDone == false)
                    return;

                if (_verifyTempFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _verifyTempFileOp.Error;

                    if (_options.DeleteCacheEntryOnFailure)
                    {
                        YooLogger.Error($"Found corrupted bundle file. Removing cache entry: {_options.Bundle.BundleGUID}");
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
