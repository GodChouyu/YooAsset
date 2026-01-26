
namespace YooAsset
{
    public sealed class LoadManifestOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            CheckParams,
            CheckActiveManifest,
            LoadPackageManifest,
            Done,
        }

        private readonly FileSystemHost _host;
        private readonly LoadManifestOptions _options;
        private FSLoadManifestOperation _loadPackageManifestOp;
        private ESteps _steps = ESteps.None;

        internal LoadManifestOperation(FileSystemHost host, LoadManifestOptions options)
        {
            _host = host;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckParams;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckParams)
            {
                if (string.IsNullOrEmpty(_options.PackageVersion))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Package version is null or empty.";
                }
                else
                {
                    _steps = ESteps.CheckActiveManifest;
                }
            }

            if (_steps == ESteps.CheckActiveManifest)
            {
                // 检测当前激活的清单对象	
                if (_host.ActiveManifest != null && _host.ActiveManifest.PackageVersion == _options.PackageVersion)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.LoadPackageManifest;
                }
            }

            if (_steps == ESteps.LoadPackageManifest)
            {
                if (_loadPackageManifestOp == null)
                {
                    var mainFileSystem = _host.GetMainFileSystem();
                    _loadPackageManifestOp = mainFileSystem.LoadManifestAsync(_options);
                    _loadPackageManifestOp.StartOperation();
                    AddChildOperation(_loadPackageManifestOp);
                }

                _loadPackageManifestOp.UpdateOperation();
                if (_loadPackageManifestOp.IsDone == false)
                    return;

                if (_loadPackageManifestOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    _host.SetActiveManifest(_loadPackageManifestOp.Manifest);
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadPackageManifestOp.Error;
                }
            }
        }
        internal override string InternalGetDescription()
        {
            return $"PackageVersion : {_options.PackageVersion}";
        }
    }
}