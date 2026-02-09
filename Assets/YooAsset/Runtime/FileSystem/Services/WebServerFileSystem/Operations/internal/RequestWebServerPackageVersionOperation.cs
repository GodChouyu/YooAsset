
namespace YooAsset
{
    /// <summary>
    /// 请求Web服务端包裹版本操作
    /// </summary>
    internal class RequestWebServerPackageVersionOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly WebServerFileSystem _fileSystem;
        private readonly int _timeout;
        private IDownloadTextRequest _downloadTextRequest;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹版本
        /// </summary>
        public string PackageVersion { get; private set; }


        internal RequestWebServerPackageVersionOperation(WebServerFileSystem fileSystem, int timeout)
        {
            _fileSystem = fileSystem;
            _timeout = timeout;
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
                    string filePath = _fileSystem.GetWebPackageVersionFilePath();
                    string url = DownloadSystemTools.ToLocalUrl(filePath);
                    var args = new DownloadDataRequestArgs(url, _timeout, 0);
                    _downloadTextRequest = _fileSystem.DownloadBackend.CreateTextRequest(args);
                    _downloadTextRequest.SendRequest();
                }

                if (_downloadTextRequest.IsDone == false)
                    return;

                if (_downloadTextRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    PackageVersion = _downloadTextRequest.Result;
                    if (TextUtility.ValidateContent(PackageVersion, out string validateError) == false)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Web server package version file validate failed: {validateError}";
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
    }
}