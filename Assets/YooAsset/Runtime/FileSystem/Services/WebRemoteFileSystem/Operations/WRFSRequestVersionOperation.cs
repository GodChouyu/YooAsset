
namespace YooAsset
{
    internal class WRFSRequestVersionOperation : FSRequestVersionOperation
    {
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly WebRemoteFileSystem _fileSystem;
        private readonly bool _appendTimeTicks;
        private readonly int _timeout;
        private RequestWebPackageVersionOperation _requestWebPackageVersionOp;
        private ESteps _steps = ESteps.None;


        internal WRFSRequestVersionOperation(WebRemoteFileSystem fileSystem, bool appendTimeTicks, int timeout)
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
                if (_requestWebPackageVersionOp == null)
                {
                    var options = new RequestWebPackageVersionOptions();
                    options.PackageName = _fileSystem.PackageName;
                    options.AppendTimeTicks = _appendTimeTicks;
                    options.Timeout = _timeout;
                    options.RemoteServices = _fileSystem.RemoteServices;
                    options.DownloadBackend = _fileSystem.DownloadBackend;
                    _requestWebPackageVersionOp = new RequestWebPackageVersionOperation(options);
                    _requestWebPackageVersionOp.StartOperation();
                    AddChildOperation(_requestWebPackageVersionOp);
                }

                _requestWebPackageVersionOp.UpdateOperation();
                Progress = _requestWebPackageVersionOp.Progress;
                if (_requestWebPackageVersionOp.IsDone == false)
                    return;

                if (_requestWebPackageVersionOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    PackageVersion = _requestWebPackageVersionOp.PackageVersion;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _requestWebPackageVersionOp.Error;
                }
            }
        }
    }
}