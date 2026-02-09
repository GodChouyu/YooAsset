
namespace YooAsset
{
    /// <summary>
    /// 内置文件系统的查询包裹版本操作
    /// </summary>
    internal class BFSRequestPackageVersionOperation : FSRequestPackageVersionOperation
    {
        private enum ESteps
        {
            None,
            RequestPackageVersion,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private RequestBuiltinPackageVersionOperation _requestBuiltinPackageVersionOp;
        private ESteps _steps = ESteps.None;


        internal BFSRequestPackageVersionOperation(BuiltinFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
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
                if (_requestBuiltinPackageVersionOp == null)
                {
                    _requestBuiltinPackageVersionOp = new RequestBuiltinPackageVersionOperation(_fileSystem);
                    _requestBuiltinPackageVersionOp.StartOperation();
                    AddChildOperation(_requestBuiltinPackageVersionOp);
                }

                _requestBuiltinPackageVersionOp.UpdateOperation();
                if (_requestBuiltinPackageVersionOp.IsDone == false)
                    return;

                if (_requestBuiltinPackageVersionOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    PackageVersion = _requestBuiltinPackageVersionOp.PackageVersion;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _requestBuiltinPackageVersionOp.Error;
                }
            }
        }
    }
}