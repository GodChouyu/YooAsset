
namespace YooAsset
{
    /// <summary>
    /// Web服务器文件缓存加载 AssetBundle 操作
    /// </summary>
    internal class WSFCLoadAssetBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            GetEntry,
            LoadBundle,
            Done,
        }

        private readonly WebServerFileCache _fileCache;
        private readonly FCLoadBundleOptions _options;
        private LoadWebAssetBundleOperation _loadWebAssetBundleOp;
        private WebServerFileCacheEntry _cacheEntry;
        private ESteps _steps = ESteps.None;

        public WSFCLoadAssetBundleOperation(WebServerFileCache fileCache, FCLoadBundleOptions options)
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
                _cacheEntry = _fileCache.GetEntry(_options.Bundle.BundleGUID);
                if (_cacheEntry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"File cache entry not found: {_options.Bundle.BundleGUID}";
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
                    string url = DownloadSystemTools.ToLocalUrl(_cacheEntry.FilePath);
                    var options = new LoadWebAssetBundleOptions();
                    options.CacheName = _fileCache.GetType().Name;
                    options.Bundle = _options.Bundle;
                    options.MainURL = url;
                    options.FallbackURL = url;
                    options.AssetBundleDecryptor = _fileCache.Config.AssetBundleDecryptor;
                    options.DownloadBackend = _fileCache.Config.DownloadBackend;
                    options.DownloadVerifyLevel = _fileCache.Config.DownloadVerifyLevel;
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
                        throw new YooInternalException("Loaded bundle result is null.");

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
        internal override void InternalWaitForCompletion()
        {
            if (_steps != ESteps.Done)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = $"{nameof(WebServerFileCache)} not support sync load asset bundle.";
                YooLogger.Error(Error);
            }
        }
    }
}