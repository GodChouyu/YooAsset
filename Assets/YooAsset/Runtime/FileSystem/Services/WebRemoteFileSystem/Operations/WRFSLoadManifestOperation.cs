
namespace YooAsset
{
    internal class WRFSLoadManifestOperation : FSLoadManifestOperation
    {
        private enum ESteps
        {
            None,
            RequestWebPackageHash,
            LoadWebPackageManifest,
            Done,
        }

        private readonly WebRemoteFileSystem _fileSystem;
        private readonly string _packageVersion;
        private readonly int _timeout;
        private RequestWebPackageHashOperation _requestWebPackageHashOp;
        private LoadWebPackageManifestOperation _loadWebPackageManifestOp;
        private ESteps _steps = ESteps.None;


        public WRFSLoadManifestOperation(WebRemoteFileSystem fileSystem, string packageVersion, int timeout)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
            _timeout = timeout;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.RequestWebPackageHash;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestWebPackageHash)
            {
                if (_requestWebPackageHashOp == null)
                {
                    var options = new RequestWebPackageHashOptions();
                    options.PackageName = _fileSystem.PackageName;
                    options.PackageVersion = _packageVersion;
                    options.Timeout = _timeout;
                    options.RemoteServices = _fileSystem.RemoteServices;
                    options.DownloadBackend = _fileSystem.DownloadBackend;
                    _requestWebPackageHashOp = new RequestWebPackageHashOperation(options);
                    _requestWebPackageHashOp.StartOperation();
                    AddChildOperation(_requestWebPackageHashOp);
                }

                _requestWebPackageHashOp.UpdateOperation();
                if (_requestWebPackageHashOp.IsDone == false)
                    return;

                if (_requestWebPackageHashOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.LoadWebPackageManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _requestWebPackageHashOp.Error;
                }
            }

            if (_steps == ESteps.LoadWebPackageManifest)
            {
                if (_loadWebPackageManifestOp == null)
                {
                    var options = new LoadWebPackageManifestOptions();
                    options.PackageName = _fileSystem.PackageName;
                    options.PackageVersion = _packageVersion;
                    options.PackageHash = _requestWebPackageHashOp.PackageHash;
                    options.Timeout = _timeout;
                    options.RemoteServices = _fileSystem.RemoteServices;
                    options.ManifestDecryptor = _fileSystem.ManifestDecryptor;
                    options.DownloadBackend = _fileSystem.DownloadBackend;
                    _loadWebPackageManifestOp = new LoadWebPackageManifestOperation(options);
                    _loadWebPackageManifestOp.StartOperation();
                    AddChildOperation(_loadWebPackageManifestOp);
                }

                _loadWebPackageManifestOp.UpdateOperation();
                Progress = _loadWebPackageManifestOp.Progress;
                if (_loadWebPackageManifestOp.IsDone == false)
                    return;

                if (_loadWebPackageManifestOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Manifest = _loadWebPackageManifestOp.Manifest;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadWebPackageManifestOp.Error;
                }
            }
        }
    }
}