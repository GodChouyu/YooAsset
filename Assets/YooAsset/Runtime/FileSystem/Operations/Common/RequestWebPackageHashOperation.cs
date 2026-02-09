namespace YooAsset
{
    /// <summary>
    /// 请求Web远端包裹哈希操作
    /// </summary>
    internal class RequestWebPackageHashOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestPackageHash,
            Done,
        }

        private readonly RequestWebPackageHashOptions _options;
        private IDownloadTextRequest _downloadTextRequest;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹哈希值
        /// </summary>
        public string PackageHash { get; private set; }


        public RequestWebPackageHashOperation(RequestWebPackageHashOptions options)
        {
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.RequestPackageHash;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestPackageHash)
            {
                if (_downloadTextRequest == null)
                {
                    string fileName = YooAssetSettingsData.GetPackageHashFileName(_options.PackageName, _options.PackageVersion);
                    string url = GetRequestURL(fileName);
                    var args = new DownloadDataRequestArgs(url, _options.Timeout, 0);
                    _downloadTextRequest = _options.DownloadBackend.CreateTextRequest(args);
                    _downloadTextRequest.SendRequest();
                }

                Progress = _downloadTextRequest.DownloadProgress;
                if (_downloadTextRequest.IsDone == false)
                    return;

                if (_downloadTextRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    PackageHash = _downloadTextRequest.Result;
                    if (TextUtility.ValidateContent(PackageHash, out string validateError) == false)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Web package hash file validate failed: {validateError}";
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
                    Error = _downloadTextRequest.Error;
                    _options.URLPolicy.OnFailure(_downloadTextRequest.Url, _downloadTextRequest.HttpCode, _downloadTextRequest.HttpError);
                }
            }
        }
        internal override void InternalDispose()
        {
            if (_downloadTextRequest != null)
            {
                _downloadTextRequest.Dispose();
                _downloadTextRequest = null;
            }
        }

        private string GetRequestURL(string fileName)
        {
            var urls = _options.RemoteServices.GetRemoteURLs(fileName);
            return _options.URLPolicy.SelectURL(urls);
        }
    }
}