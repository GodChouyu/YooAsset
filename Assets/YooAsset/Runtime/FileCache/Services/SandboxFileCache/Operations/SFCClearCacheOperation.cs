
namespace YooAsset
{
    /// <summary>
    /// 清理沙盒文件缓存操作
    /// </summary>
    internal class SFCClearCacheOperation : FCClearCacheOperation
    {
        private enum ESteps
        {
            None,
            GetResult,
            ClearCacheFiles,
            Done,
        }

        private readonly SandboxFileCache _fileCache;
        private readonly FCClearCacheOptions _options;
        private ClearCacheFilesOperation _clearCacheFilesOp;
        private ESteps _steps = ESteps.None;

        internal SFCClearCacheOperation(SandboxFileCache fileCache, FCClearCacheOptions options)
        {
            _fileCache = fileCache;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.GetResult;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.GetResult)
            {
                ClearResult clearResult;
                if (_options.ClearMode == EFileClearMode.ClearAllBundleFiles.ToString())
                {
                    clearResult = GetAllCache(_fileCache.GetAllEntries());
                }
                else if (_options.ClearMode == EFileClearMode.ClearUnusedBundleFiles.ToString())
                {
                    clearResult = GetUnusedCache(_options, _fileCache.GetAllEntries());
                }
                else if (_options.ClearMode == EFileClearMode.ClearBundleFilesByLocations.ToString())
                {
                    clearResult = GetCacheByLocations(_options, _fileCache.GetAllEntries());
                }
                else if (_options.ClearMode == EFileClearMode.ClearBundleFilesByTags.ToString())
                {
                    clearResult = GetCacheByTags(_options, _fileCache.GetAllEntries());
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Invalid clear mode: {_options.ClearMode}";
                    return;
                }

                if (clearResult.Succeeded == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = clearResult.Error;
                    return;
                }

                _clearCacheFilesOp = new ClearCacheFilesOperation(_fileCache, clearResult.BundleGUIDs);
                _clearCacheFilesOp.StartOperation();
                AddChildOperation(_clearCacheFilesOp);
                _steps = ESteps.ClearCacheFiles;
            }

            if (_steps == ESteps.ClearCacheFiles)
            {
                if (IsWaitForCompletion)
                    _clearCacheFilesOp.WaitForCompletion();

                _clearCacheFilesOp.UpdateOperation();
                Progress = _clearCacheFilesOp.Progress;
                if (_clearCacheFilesOp.IsDone == false)
                    return;

                if (_clearCacheFilesOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _clearCacheFilesOp.Error;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}
