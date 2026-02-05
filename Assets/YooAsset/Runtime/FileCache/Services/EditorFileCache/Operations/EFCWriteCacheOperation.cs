
namespace YooAsset
{
    /// <summary>
    /// 编辑器文件缓存写入操作
    /// </summary>
    internal class EFCWriteCacheOperation : FCWriteCacheOperation
    {
        private enum ESteps
        {
            None,
            CheckCache,
            CacheFile,
            Done,
        }

        private readonly EditorFileCache _fileCache;
        private readonly FCWriteCacheOptions _options;
        private ESteps _steps = ESteps.None;

        public EFCWriteCacheOperation(EditorFileCache cache, FCWriteCacheOptions options)
        {
            _fileCache = cache;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckCache;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckCache)
            {
                if (_fileCache.IsCached(_options.Bundle.BundleGUID))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "The bundle is already cached.";
                }
                else
                {
                    _steps = ESteps.CacheFile;
                }
            }

            if (_steps == ESteps.CacheFile)
            {
                var cacheEntry = new EditorFileCacheEntry(_options.Bundle.BundleGUID, _options.FilePath);
                _fileCache.AddEntry(_options.Bundle.BundleGUID, cacheEntry);
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}
