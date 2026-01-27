
namespace YooAsset
{
    internal class WRFCLoadAssetBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            CreateRequest,
            CheckRequest,
            TryAgain,
            Done,
        }

        private readonly WebRemoteFileCache _fileCache;
        private readonly LoadBundleOptions _options;
        private IDownloadAssetBundleRequest _downloadAssetBundleRequest;
        private ESteps _steps = ESteps.None;

        // 失败重试
        private int _requestCount = 0;
        private float _tryAgainTimer = 0;
        private int _failedTryAgain;

        public WRFCLoadAssetBundleOperation(WebRemoteFileCache fileCache, LoadBundleOptions options)
        {
            _fileCache = fileCache;
            _options = options;
            _failedTryAgain = fileCache.Config.RetryCount;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CreateRequest;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CreateRequest)
            {
                var entry = _fileCache.GetEntry(_options.Bundle);
                if(entry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Not found file cache entry: {_options.Bundle.BundleGUID}";
                    return;
                }

                string url = GetRequestURL(entry);
                var args = new DownloadAssetBundleRequestArgs(url, 0, _fileCache.Config.WatchdogTimeout, _fileCache.Config.DisableUnityWebCache, _options.Bundle.FileHash, _options.Bundle.UnityCRC);
                _downloadAssetBundleRequest = _fileCache.Config.DownloadBackend.CreateAssetBundleRequest(args);
                _downloadAssetBundleRequest.SendRequest();
                _steps = ESteps.CheckRequest;
            }

            if (_steps == ESteps.CheckRequest)
            {
                Progress = _downloadAssetBundleRequest.DownloadProgress;
                if (_downloadAssetBundleRequest.IsDone == false)
                    return;

                if (_downloadAssetBundleRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    var assetBundle = _downloadAssetBundleRequest.Result;
                    if (assetBundle == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Failed to load asset bundle : {_options.Bundle.BundleName}";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        BundleResult = new AssetBundleResult(null, _options.Bundle, assetBundle, null);
                    }
                }
                else
                {
                    if (_failedTryAgain > 0)
                    {
                        _steps = ESteps.TryAgain;
                        YooLogger.Warning($"Failed download : {_downloadAssetBundleRequest.Url} Try again.");
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _downloadAssetBundleRequest.Error;
                        YooLogger.Error(Error);
                    }
                }

                // 最终释放请求器
                _downloadAssetBundleRequest.Dispose();
            }

            if (_steps == ESteps.TryAgain)
            {
                _tryAgainTimer += UnityEngine.Time.unscaledDeltaTime;
                if (_tryAgainTimer > 1f)
                {
                    _tryAgainTimer = 0f;
                    _failedTryAgain--;
                    Progress = 0f;
                    _steps = ESteps.CreateRequest;
                }
            }
        }

        /// <summary>
        /// 获取网络请求地址
        /// </summary>
        protected string GetRequestURL(WebRemoteFileCacheEntry entry)
        {
            // 轮流返回请求地址
            _requestCount++;
            if (_requestCount % 2 == 0)
                return entry.FallbackURL;
            else
                return entry.MainURL;
        }
    }
}