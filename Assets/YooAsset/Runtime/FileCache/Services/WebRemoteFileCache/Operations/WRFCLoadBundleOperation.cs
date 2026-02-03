
namespace YooAsset
{
    internal class WRFCLoadAssetBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            GetEntry,
            LoadBundle,
            Done,
        }

        private readonly WebRemoteFileCache _fileCache;
        private readonly LoadBundleOptions _options;
        private LoadWebAssetBundleOperation _loadWebAssetBundleOp;
        private WebRemoteFileCacheEntry _cacheEntry;
        private ESteps _steps = ESteps.None;

        public WRFCLoadAssetBundleOperation(WebRemoteFileCache fileCache, LoadBundleOptions options)
        {
            _fileCache = fileCache;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.GetEntry;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.GetEntry)
            {
                _cacheEntry = _fileCache.GetEntry(_options.Bundle);
                if (_cacheEntry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Not found file cache entry: {_options.Bundle.BundleGUID}";
                }
                else
                {
                    _steps = ESteps.LoadBundle;
                }
            }

            if (_steps == ESteps.LoadBundle)
            {
                if (_loadWebAssetBundleOp == null)
                {
                    var options = new LoadWebAssetBundleOptions();
                    options.CacheName = _fileCache.GetType().Name;
                    options.Bundle = _options.Bundle;
                    options.MainURL = _cacheEntry.MainURL;
                    options.FallbackURL = _cacheEntry.FallbackURL;
                    options.Decryptor = _fileCache.Config.AssetBundleDecryptor;
                    options.DownloadBackend = _fileCache.Config.DownloadBackend;
                    options.WatchdogTimeout = _fileCache.Config.WatchdogTimeout;
                    options.DisableUnityWebCache = _fileCache.Config.DisableUnityWebCache;

                    if (_options.Bundle.IsEncrypted)
                        _loadWebAssetBundleOp = new LoadWebEncryptedAssetBundleOperation(options);
                    else
                        _loadWebAssetBundleOp = new LoadWebNormalAssetBundleOperation(options);

                    _loadWebAssetBundleOp.StartOperation();
                    AddChildOperation(_loadWebAssetBundleOp);
                }

                _loadWebAssetBundleOp.UpdateOperation();
                if (_loadWebAssetBundleOp.IsDone == false)
                    return;

                if (_loadWebAssetBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadWebAssetBundleOp.BundleResult == null)
                        throw new YooInternalException("Loaded asset bundle result is null.");

                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleResult = _loadWebAssetBundleOp.BundleResult;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadWebAssetBundleOp.Error;
                }
            }
        }
    }
}