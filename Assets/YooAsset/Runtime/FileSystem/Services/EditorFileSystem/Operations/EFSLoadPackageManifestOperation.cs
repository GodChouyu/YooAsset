
namespace YooAsset
{
    /// <summary>
    /// 编辑器文件系统的加载包裹清单操作
    /// </summary>
    internal class EFSLoadPackageManifestOperation : FSLoadPackageManifestOperation
    {
        private enum ESteps
        {
            None,
            LoadPackageHash,
            LoadPackageManifest,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private readonly string _packageVersion;
        private LoadEditorPackageHashOperation _loadEditorPackageHashOpe;
        private LoadEditorPackageManifestOperation _loadEditorPackageManifestOp;
        private ESteps _steps = ESteps.None;


        internal EFSLoadPackageManifestOperation(EditorFileSystem fileSystem, string packageVersion)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadPackageHash;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadPackageHash)
            {
                if (_loadEditorPackageHashOpe == null)
                {
                    _loadEditorPackageHashOpe = new LoadEditorPackageHashOperation(_fileSystem, _packageVersion);
                    _loadEditorPackageHashOpe.StartOperation();
                    AddChildOperation(_loadEditorPackageHashOpe);
                }

                _loadEditorPackageHashOpe.UpdateOperation();
                if (_loadEditorPackageHashOpe.IsDone == false)
                    return;

                if (_loadEditorPackageHashOpe.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.LoadPackageManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadEditorPackageHashOpe.Error;
                }
            }

            if (_steps == ESteps.LoadPackageManifest)
            {
                if (_loadEditorPackageManifestOp == null)
                {
                    string packageHash = _loadEditorPackageHashOpe.PackageHash;
                    _loadEditorPackageManifestOp = new LoadEditorPackageManifestOperation(_fileSystem, _packageVersion, packageHash);
                    _loadEditorPackageManifestOp.StartOperation();
                    AddChildOperation(_loadEditorPackageManifestOp);
                }

                _loadEditorPackageManifestOp.UpdateOperation();
                Progress = _loadEditorPackageManifestOp.Progress;
                if (_loadEditorPackageManifestOp.IsDone == false)
                    return;

                if (_loadEditorPackageManifestOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    Manifest = _loadEditorPackageManifestOp.Manifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadEditorPackageManifestOp.Error;
                }
            }
        }
    }
}