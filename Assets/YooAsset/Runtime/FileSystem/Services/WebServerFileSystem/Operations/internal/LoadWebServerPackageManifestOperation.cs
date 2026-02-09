
namespace YooAsset
{
    /// <summary>
    /// 加载Web服务端包裹清单文件操作
    /// </summary>
    internal class LoadWebServerPackageManifestOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestFileData,
            VerifyFileData,
            LoadManifest,
            Done,
        }

        private readonly WebServerFileSystem _fileSystem;
        private readonly string _packageVersion;
        private readonly string _packageHash;
        private readonly int _timeout;
        private IDownloadBytesRequest _downloadBytesRequest;
        private DeserializeManifestOperation _deserializeManifestOp;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹清单
        /// </summary>
        public PackageManifest Manifest { get; private set; }


        internal LoadWebServerPackageManifestOperation(WebServerFileSystem fileSystem, string packageVersion, string packageHash, int timeout)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
            _packageHash = packageHash;
            _timeout = timeout;
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
                    string filePath = _fileSystem.GetWebPackageManifestFilePath(_packageVersion);
                    string url = DownloadSystemTools.ToLocalUrl(filePath);
                    var args = new DownloadDataRequestArgs(url, _timeout, 0);
                    _downloadBytesRequest = _fileSystem.DownloadBackend.CreateBytesRequest(args);
                    _downloadBytesRequest.SendRequest();
                }

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
                }
            }

            if (_steps == ESteps.VerifyFileData)
            {
                if (PackageManifestTools.VerifyManifestData(_downloadBytesRequest.Result, _packageHash))
                {
                    _steps = ESteps.LoadManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Failed to verify web server package manifest file.";
                }
            }

            if (_steps == ESteps.LoadManifest)
            {
                if (_deserializeManifestOp == null)
                {
                    _deserializeManifestOp = new DeserializeManifestOperation(_fileSystem.ManifestDecryptor, _downloadBytesRequest.Result);
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
            return $"PackageVersion: {_packageVersion} PackageHash: {_packageHash}";
        }
    }
}