namespace YooAsset
{
    /// <summary>
    /// 请求Web远端包裹版本操作
    /// </summary>
    internal class RequestWebPackageVersionOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly RequestWebPackageVersionOptions _options;
        private IDownloadTextRequest _downloadTextRequest;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹版本
        /// </summary>
        public string PackageVersion { get; private set; }


        public RequestWebPackageVersionOperation(RequestWebPackageVersionOptions options)
        {
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.RequestPackageVersion;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestPackageVersion)
            {
                if (_downloadTextRequest == null)
                {
                    string fileName = YooAssetSettingsData.GetPackageVersionFileName(_options.PackageName);
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
                    PackageVersion = _downloadTextRequest.Result;
                    if (TextUtility.ValidateContent(PackageVersion, out string validateError) == false)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Web package version file validate failed: {validateError}";
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
            string url = _options.URLPolicy.SelectURL(urls);

            // 在URL末尾添加时间戳
            if (_options.AppendTimeTicks)
                return $"{url}?{System.DateTime.UtcNow.Ticks}";
            else
                return url;
        }
    }
}