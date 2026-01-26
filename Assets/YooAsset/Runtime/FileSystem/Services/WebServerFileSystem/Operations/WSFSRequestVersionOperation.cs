
namespace YooAsset
{
    internal class WSFSRequestVersionOperation : FSRequestVersionOperation
    {
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly WebServerFileSystem _fileSystem;
        private readonly int _timeout;
        private RequestWebServerPackageVersionOperation _requestWebPackageVersionOp;
        private ESteps _steps = ESteps.None;


        internal WSFSRequestVersionOperation(WebServerFileSystem fileSystem, int timeout)
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
                if (_requestWebPackageVersionOp == null)
                {
                    _requestWebPackageVersionOp = new RequestWebServerPackageVersionOperation(_fileSystem, _timeout);
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