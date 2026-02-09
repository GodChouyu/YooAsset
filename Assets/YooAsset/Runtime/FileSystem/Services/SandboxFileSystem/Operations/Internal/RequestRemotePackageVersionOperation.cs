
namespace YooAsset
{
    /// <summary>
    /// 请求远端包裹版本操作
    /// </summary>
    internal class RequestRemotePackageVersionOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly SandboxFileSystem _fileSystem;
        private readonly bool _appendTimeTicks;
        private readonly int _timeout;
        private IDownloadTextRequest _downloadTextRequest;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹版本
        /// </summary>
        internal string PackageVersion { set; get; }


        internal RequestRemotePackageVersionOperation(SandboxFileSystem fileSystem, bool appendTimeTicks, int timeout)
        {
            _fileSystem = fileSystem;
            _appendTimeTicks = appendTimeTicks;
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
                    string fileName = YooAssetSettingsData.GetPackageVersionFileName(_fileSystem.PackageName);
                    string url = GetWebRequestURL(fileName);
                    int watchDogTime = _fileSystem.DownloadWatchdogTimeout;
                    var args = new DownloadDataRequestArgs(url, _timeout, watchDogTime);
                    _downloadTextRequest = _fileSystem.DownloadBackend.CreateTextRequest(args);
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
                        Error = $"Remote package version file validate failed: {validateError}";
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
                    _fileSystem.DownloadURLPolicy.OnFailure(_downloadTextRequest.Url, _downloadTextRequest.HttpCode, _downloadTextRequest.HttpError);
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

        private string GetWebRequestURL(string fileName)
        {
            var urls = _fileSystem.RemoteServices.GetRemoteURLs(fileName);
            string url = _fileSystem.DownloadURLPolicy.SelectURL(urls);

            // 在URL末尾添加时间戳
            if (_appendTimeTicks)
                return $"{url}?{System.DateTime.UtcNow.Ticks}";
            else
                return url;
        }
    }
}