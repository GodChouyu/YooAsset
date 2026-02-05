namespace YooAsset
{
    internal class RequestWebPackageHashOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestPackageHash,
            Done,
        }

        private readonly RequestWebPackageHashOptions _options;
        private IDownloadTextRequest _webTextRequestOp;
        private int _requestCount = 0;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹哈希值
        /// </summary>
        public string PackageHash { private set; get; }


        public RequestWebPackageHashOperation(RequestWebPackageHashOptions options)
        {
            _options = options;
        }
        internal override void InternalStart()
        {
            _requestCount = DownloadFailureCounter.GetFailureCount(_options.PackageName, nameof(RequestWebPackageHashOperation));
            _steps = ESteps.RequestPackageHash;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestPackageHash)
            {
                if (_webTextRequestOp == null)
                {
                    string fileName = YooAssetSettingsData.GetPackageHashFileName(_options.PackageName, _options.PackageVersion);
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
                    PackageHash = _webTextRequestOp.Result;
                    if (string.IsNullOrEmpty(PackageHash))
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Web package hash file content is empty.";
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
                    DownloadFailureCounter.RecordFailure(_options.PackageName, nameof(RequestWebPackageHashOperation));
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
            // 轮流返回请求地址
            if (_requestCount % 2 == 0)
                return _options.RemoteServices.GetRemoteMainURL(fileName);
            else
                return _options.RemoteServices.GetRemoteFallbackURL(fileName);
        }
    }
}