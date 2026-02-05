namespace YooAsset
{
    internal class LoadWebPackageManifestOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestFileData,
            VerifyFileData,
            LoadManifest,
            Done,
        }

        private readonly LoadWebPackageManifestOptions _options;
        private IDownloadBytesRequest _webDataRequestOp;
        private DeserializeManifestOperation _deserializer;
        private int _requestCount = 0;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹清单
        /// </summary>
        public PackageManifest Manifest { private set; get; }


        internal LoadWebPackageManifestOperation(LoadWebPackageManifestOptions options)
        {
            _options = options;
        }
        internal override void InternalStart()
        {
            _requestCount = DownloadFailureCounter.GetFailureCount(_options.PackageName, nameof(LoadWebPackageManifestOperation));
            _steps = ESteps.RequestFileData;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestFileData)
            {
                if (_webDataRequestOp == null)
                {
                    string fileName = YooAssetSettingsData.GetManifestBinaryFileName(_options.PackageName, _options.PackageVersion);
                    string url = GetRequestURL(fileName);
                    var args = new DownloadDataRequestArgs(url, _options.Timeout, 0);
                    _webDataRequestOp = _options.DownloadBackend.CreateBytesRequest(args);
                    _webDataRequestOp.SendRequest();
                }

                Progress = _webDataRequestOp.DownloadProgress;
                if (_webDataRequestOp.IsDone == false)
                    return;

                if (_webDataRequestOp.Status == EDownloadRequestStatus.Succeeded)
                {
                    _steps = ESteps.VerifyFileData;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _webDataRequestOp.Error;
                    DownloadFailureCounter.RecordFailure(_options.PackageName, nameof(LoadWebPackageManifestOperation));
                }
            }

            if (_steps == ESteps.VerifyFileData)
            {
                if (PackageManifestTools.VerifyManifestData(_webDataRequestOp.Result, _options.PackageHash))
                {
                    _steps = ESteps.LoadManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Failed to verify web package manifest file.";
                }
            }

            if (_steps == ESteps.LoadManifest)
            {
                if (_deserializer == null)
                {
                    _deserializer = new DeserializeManifestOperation(_options.ManifestDecryptor, _webDataRequestOp.Result);
                    _deserializer.StartOperation();
                    AddChildOperation(_deserializer);
                }

                _deserializer.UpdateOperation();
                Progress = _deserializer.Progress;
                if (_deserializer.IsDone == false)
                    return;

                if (_deserializer.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Manifest = _deserializer.Manifest;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _deserializer.Error;
                }
            }
        }
        internal override void InternalDispose()
        {
            if (_webDataRequestOp != null)
            {
                _webDataRequestOp.Dispose();
                _webDataRequestOp = null;
            }
        }
        internal override string InternalGetDescription()
        {
            return $"PackageVersion : {_options.PackageVersion} PackageHash : {_options.PackageHash}";
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