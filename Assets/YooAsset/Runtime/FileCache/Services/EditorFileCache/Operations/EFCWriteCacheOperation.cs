using System;
using System.IO;

namespace YooAsset
{
    internal class EFCWriteCacheOperation : FCWriteCacheOperation
    {
        private enum ESteps
        {
            None,
            Check,
            CacheFile,
            Done,
        }

        private readonly EditorFileCache _cache;
        private readonly WriteCacheOptions _options;
        private ESteps _steps = ESteps.None;

        public EFCWriteCacheOperation(EditorFileCache cache, WriteCacheOptions options)
        {
            _cache = cache;
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
                if (_cache.IsCached(_options.Bundle.BundleGUID))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "The bundle is cached.";
                }
                else
                {
                    _steps = ESteps.CacheFile;
                }
            }

            if (_steps == ESteps.CacheFile)
            {
                var cacheEntry = new EditorFileCacheEntry(_options.Bundle.BundleGUID, _options.FilePath);
                _cache.AddEntry(_options.Bundle.BundleGUID, cacheEntry);
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
