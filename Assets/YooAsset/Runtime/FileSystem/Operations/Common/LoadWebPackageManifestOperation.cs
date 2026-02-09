namespace YooAsset
{
    /// <summary>
    /// 加载Web远端包裹清单文件操作
    /// </summary>
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
        private IDownloadBytesRequest _downloadBytesRequest;
        private DeserializeManifestOperation _deserializeManifestOp;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹清单
        /// </summary>
        public PackageManifest Manifest { get; private set; }


        internal LoadWebPackageManifestOperation(LoadWebPackageManifestOptions options)
        {
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.RequestFileData;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestFileData)
            {
                if (_downloadBytesRequest == null)
                {
                    string fileName = YooAssetSettingsData.GetManifestBinaryFileName(_options.PackageName, _options.PackageVersion);
                    string url = GetRequestURL(fileName);
                    var args = new DownloadDataRequestArgs(url, _options.Timeout, 0);
                    _downloadBytesRequest = _options.DownloadBackend.CreateBytesRequest(args);
                    _downloadBytesRequest.SendRequest();
                }

                Progress = _downloadBytesRequest.DownloadProgress;
                if (_downloadBytesRequest.IsDone == false)
                    return;

                if (_downloadBytesRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    _steps = ESteps.VerifyFileData;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _downloadBytesRequest.Error;
                    _options.URLPolicy.OnFailure(_downloadBytesRequest.Url, _downloadBytesRequest.HttpCode, _downloadBytesRequest.HttpError);
                }
            }

            if (_steps == ESteps.VerifyFileData)
            {
                if (PackageManifestTools.VerifyManifestData(_downloadBytesRequest.Result, _options.PackageHash))
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
                if (_deserializeManifestOp == null)
                {
                    _deserializeManifestOp = new DeserializeManifestOperation(_options.ManifestDecryptor, _downloadBytesRequest.Result);
                    _deserializeManifestOp.StartOperation();
                    AddChildOperation(_deserializeManifestOp);
                }

                _deserializeManifestOp.UpdateOperation();
                Progress = _deserializeManifestOp.Progress;
                if (_deserializeManifestOp.IsDone == false)
                    return;

                if (_deserializeManifestOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Manifest = _deserializeManifestOp.Manifest;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _deserializeManifestOp.Error;
                }
            }
        }
        internal override void InternalDispose()
        {
            if (_downloadBytesRequest != null)
            {
                _downloadBytesRequest.Dispose();
                _downloadBytesRequest = null;
            }
        }
        internal override string InternalGetDescription()
        {
            return $"PackageVersion: {_options.PackageVersion} PackageHash: {_options.PackageHash}";
        }

        private string GetRequestURL(string fileName)
        {
            var urls = _options.RemoteServices.GetRemoteURLs(fileName);
            return _options.URLPolicy.SelectURL(urls);
        }
    }
}