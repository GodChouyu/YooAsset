
namespace YooAsset
{
    internal class WSFSInitializeOperation : FSInitializeOperation
    {
        private enum ESteps
        {
            None,
            LoadCatalogFile,
            Done,
        }

        private readonly WebServerFileSystem _fileSystem;
        private LoadWebServerCatalogFileOperation _loadCatalogFileOp;
        private ESteps _steps = ESteps.None;


        public WSFSInitializeOperation(WebServerFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadCatalogFile;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadCatalogFile)
            {
                if (_loadCatalogFileOp == null)
                {
                    _loadCatalogFileOp = new LoadWebServerCatalogFileOperation(_fileSystem, 60);
                    _loadCatalogFileOp.StartOperation();
                    AddChildOperation(_loadCatalogFileOp);
                }

                _loadCatalogFileOp.UpdateOperation();
                if (_loadCatalogFileOp.IsDone == false)
                    return;

                if (_loadCatalogFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadCatalogFileOp.Error;
                }
            }
        }
    }
}