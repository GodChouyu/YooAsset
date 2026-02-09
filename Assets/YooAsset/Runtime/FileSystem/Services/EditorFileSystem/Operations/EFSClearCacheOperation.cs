
namespace YooAsset
{
    /// <summary>
    /// 编辑器文件系统的清理缓存操作
    /// </summary>
    internal class EFSClearCacheOperation : FSClearCacheOperation
    {
        private enum ESteps
        {
            None,
            ClearCache,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private readonly FSClearCacheOptions _options;
        private FCClearCacheOperation _clearCacheOp;
        private ESteps _steps = ESteps.None;
        
        internal EFSClearCacheOperation(EditorFileSystem fileSystem, FSClearCacheOptions options)
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
                    _clearCacheOp = _fileSystem.FileCache.ClearCacheAsync(_options.ConvertTo());
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