
namespace YooAsset
{
    /// <summary>
    /// 编辑器文件系统的查询包裹版本操作
    /// </summary>
    internal class EFSRequestPackageVersionOperation : FSRequestPackageVersionOperation
    {
        private enum ESteps
        {
            None,
            LoadPackageVersion,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private LoadEditorPackageVersionOperation _loadEditorPackageVersionOp;
        private ESteps _steps = ESteps.None;


        internal EFSRequestPackageVersionOperation(EditorFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadPackageVersion;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadPackageVersion)
            {
                if (_loadEditorPackageVersionOp == null)
                {
                    _loadEditorPackageVersionOp = new LoadEditorPackageVersionOperation(_fileSystem);
                    _loadEditorPackageVersionOp.StartOperation();
                    AddChildOperation(_loadEditorPackageVersionOp);
                }

                _loadEditorPackageVersionOp.UpdateOperation();
                if (_loadEditorPackageVersionOp.IsDone == false)
                    return;

                if (_loadEditorPackageVersionOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    PackageVersion = _loadEditorPackageVersionOp.PackageVersion;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadEditorPackageVersionOp.Error;
                }
            }
        }
    }
}