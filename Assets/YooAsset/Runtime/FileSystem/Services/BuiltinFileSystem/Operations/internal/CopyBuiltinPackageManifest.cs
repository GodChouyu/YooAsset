
namespace YooAsset
{
    internal class CopyBuiltinPackageManifest : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            LoadBuiltinPackageVersion,
            CopyBuiltinPackageHash,
            CopyBuiltinPackageManifest,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private RequestBuiltinPackageVersionOperation _requestBuildinPackageVersionOp;
        private CopyBuiltinFileOperation _copyBuiltinHashFileOp;
        private CopyBuiltinFileOperation _copyBuiltinManifestFileOp;
        private ESteps _steps = ESteps.None;

        public CopyBuiltinPackageManifest(BuiltinFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadBuiltinPackageVersion;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadBuiltinPackageVersion)
            {
                if (_requestBuildinPackageVersionOp == null)
                {
                    _requestBuildinPackageVersionOp = new RequestBuiltinPackageVersionOperation(_fileSystem);
                    _requestBuildinPackageVersionOp.StartOperation();
                    AddChildOperation(_requestBuildinPackageVersionOp);
                }

                _requestBuildinPackageVersionOp.UpdateOperation();
                if (_requestBuildinPackageVersionOp.IsDone == false)
                    return;

                if (_requestBuildinPackageVersionOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.CopyBuiltinPackageHash;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _requestBuildinPackageVersionOp.Error;
                }
            }

            if (_steps == ESteps.CopyBuiltinPackageHash)
            {
                if (_copyBuiltinHashFileOp == null)
                {
                    string packageVersion = _requestBuildinPackageVersionOp.PackageVersion;
                    string destFilePath = GetCopyPackageHashDestPath(packageVersion);
                    string sourceFilePath = _fileSystem.GetBuiltinPackageHashFilePath(packageVersion);
                    _copyBuiltinHashFileOp = new CopyBuiltinFileOperation(_fileSystem, sourceFilePath, destFilePath);
                    _copyBuiltinHashFileOp.StartOperation();
                    AddChildOperation(_copyBuiltinHashFileOp);
                }

                _copyBuiltinHashFileOp.UpdateOperation();
                if (_copyBuiltinHashFileOp.IsDone == false)
                    return;

                if (_copyBuiltinHashFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.CopyBuiltinPackageManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _copyBuiltinHashFileOp.Error;
                }
            }

            if (_steps == ESteps.CopyBuiltinPackageManifest)
            {
                if (_copyBuiltinManifestFileOp == null)
                {
                    string packageVersion = _requestBuildinPackageVersionOp.PackageVersion;
                    string destFilePath = GetCopyPackageManifestDestPath(packageVersion);
                    string sourceFilePath = _fileSystem.GetBuiltinPackageManifestFilePath(packageVersion);
                    _copyBuiltinManifestFileOp = new CopyBuiltinFileOperation(_fileSystem, sourceFilePath, destFilePath);
                    _copyBuiltinManifestFileOp.StartOperation();
                    AddChildOperation(_copyBuiltinManifestFileOp);
                }

                _copyBuiltinManifestFileOp.UpdateOperation();
                if (_copyBuiltinManifestFileOp.IsDone == false)
                    return;

                if (_copyBuiltinManifestFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _copyBuiltinManifestFileOp.Error;
                }
            }
        }

        private string GetCopyManifestFileRoot()
        {
            string destRoot = _fileSystem.CopyBuildinPackageManifestDestRoot;
            if (string.IsNullOrEmpty(destRoot))
            {
                string defaultCacheRoot = YooAssetSettingsData.GetYooDefaultCacheRoot();
                destRoot = PathUtility.Combine(defaultCacheRoot, _fileSystem.PackageName, DefaultCacheFileSystemDefine.ManifestFilesFolderName);
            }
            return destRoot;
        }
        private string GetCopyPackageHashDestPath(string packageVersion)
        {
            string fileRoot = GetCopyManifestFileRoot();
            string fileName = YooAssetSettingsData.GetPackageHashFileName(_fileSystem.PackageName, packageVersion);
            return PathUtility.Combine(fileRoot, fileName);
        }
        private string GetCopyPackageManifestDestPath(string packageVersion)
        {
            string fileRoot = GetCopyManifestFileRoot();
            string fileName = YooAssetSettingsData.GetManifestBinaryFileName(_fileSystem.PackageName, packageVersion);
            return PathUtility.Combine(fileRoot, fileName);
        }
    }
}