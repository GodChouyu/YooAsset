
namespace YooAsset
{
    public sealed class RequestVersionOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly FileSystemHost _host;
        private readonly RequestVersionOptions _options;
        private FSRequestVersionOperation _requestPackageVersionOp;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 当前最新的包裹版本
        /// </summary>
        public string PackageVersion { get; private set; }


        internal RequestVersionOperation(FileSystemHost host, RequestVersionOptions options)
        {
            _host = host;
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
                if (_requestPackageVersionOp == null)
                {
                    var mainFileSystem = _host.GetMainFileSystem();
                    _requestPackageVersionOp = mainFileSystem.RequestVersionAsync(_options);
                    _requestPackageVersionOp.StartOperation();
                    AddChildOperation(_requestPackageVersionOp);
                }

                _requestPackageVersionOp.UpdateOperation();
                if (_requestPackageVersionOp.IsDone == false)
                    return;

                if (_requestPackageVersionOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.Done;
                    PackageVersion = _requestPackageVersionOp.PackageVersion;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _requestPackageVersionOp.Error;
                }
            }
        }
    }
}