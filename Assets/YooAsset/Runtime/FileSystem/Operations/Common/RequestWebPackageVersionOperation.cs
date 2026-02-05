namespace YooAsset
{
    internal class RequestWebPackageVersionOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly RequestWebPackageVersionOptions _options;
        private IDownloadTextRequest _webTextRequestOp;
        private int _requestCount = 0;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹版本
        /// </summary>
        public string PackageVersion { private set; get; }


        public RequestWebPackageVersionOperation(RequestWebPackageVersionOptions options)
        {
            _options = options;
        }
        internal override void InternalStart()
        {
            _requestCount = DownloadFailureCounter.GetFailureCount(_options.PackageName, nameof(RequestWebPackageVersionOperation));
            _steps = ESteps.RequestPackageVersion;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestPackageVersion)
            {
                if (_webTextRequestOp == null)
                {
                    string fileName = YooAssetSettingsData.GetPackageVersionFileName(_options.PackageName);
                    string url = GetRequestURL(fileName);
                    var args = new DownloadDataRequestArgs(url, _options.Timeout, 0);
                    _webTextRequestOp = _options.DownloadBackend.CreateTextRequest(args);
                    _webTextRequestOp.SendRequest();
                }

                Progress = _webTextRequestOp.DownloadProgress;
                if (_webTextRequestOp.IsDone == false)
                    return;

                if (_webTextRequestOp.Status == EDownloadRequestStatus.Succeeded)
                {
                    PackageVersion = _webTextRequestOp.Result;
                    if (string.IsNullOrEmpty(PackageVersion))
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Web package version file content is empty.";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _webTextRequestOp.Error;
                    DownloadFailureCounter.RecordFailure(_options.PackageName, nameof(RequestWebPackageVersionOperation));
                }
            }
        }
        internal override void InternalDispose()
        {
            if (_webTextRequestOp != null)
            {
                _webTextRequestOp.Dispose();
                _webTextRequestOp = null;
            }
        }

        private string GetRequestURL(string fileName)
        {
            string url;

            // 轮流返回请求地址
            if (_requestCount % 2 == 0)
                url = _options.RemoteServices.GetRemoteMainURL(fileName);
            else
                url = _options.RemoteServices.GetRemoteFallbackURL(fileName);

            // 在URL末尾添加时间戳
            if (_options.AppendTimeTicks)
                return $"{url}?{System.DateTime.UtcNow.Ticks}";
            else
                return url;
        }
    }
}