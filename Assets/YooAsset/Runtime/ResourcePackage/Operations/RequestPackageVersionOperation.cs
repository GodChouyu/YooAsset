
namespace YooAsset
{
    /// <summary>
    /// 请求包裹版本操作
    /// </summary>
    public sealed class RequestPackageVersionOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly FileSystemHost _host;
        private readonly RequestPackageVersionOptions _options;
        private FSRequestPackageVersionOperation _requestVersionOp;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 当前最新的包裹版本
        /// </summary>
        public string PackageVersion { get; private set; }


        internal RequestPackageVersionOperation(FileSystemHost host, RequestPackageVersionOptions options)
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
                if (_requestVersionOp == null)
                {
                    var mainFileSystem = _host.GetMainFileSystem();
                    _requestVersionOp = mainFileSystem.RequestPackageVersionAsync(_options.ConvertTo());
                    _requestVersionOp.StartOperation();
                    AddChildOperation(_requestVersionOp);
                }

                _requestVersionOp.UpdateOperation();
                if (_requestVersionOp.IsDone == false)
                    return;

                if (_requestVersionOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    PackageVersion = _requestVersionOp.PackageVersion;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _requestVersionOp.Error;
                }
            }
        }
    }
}