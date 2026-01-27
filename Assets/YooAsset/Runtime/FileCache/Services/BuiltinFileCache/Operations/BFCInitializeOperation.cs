
namespace YooAsset
{
    internal class BFCInitializeOperation : FCInitializeOperation
    {
        private enum ESteps
        {
            None,
            LoadCatalogFile,
            RecordFiles,
            Done,
        }

        private readonly BuiltinFileCache _fileCache;
        private LoadBuiltinCatalogFileOperation _loadBuiltinCatalogFileOp;
        private ESteps _steps = ESteps.None;

        public BFCInitializeOperation(BuiltinFileCache cache)
        {
            _fileCache = cache;
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
                if (_loadBuiltinCatalogFileOp == null)
                {
                    _loadBuiltinCatalogFileOp = new LoadBuiltinCatalogFileOperation(_fileCache);
                    _loadBuiltinCatalogFileOp.StartOperation();
                    AddChildOperation(_loadBuiltinCatalogFileOp);
                }

                _loadBuiltinCatalogFileOp.UpdateOperation();
                if (_loadBuiltinCatalogFileOp.IsDone == false)
                    return;

                if (_loadBuiltinCatalogFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.RecordFiles;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadBuiltinCatalogFileOp.Error;
                }
            }

            if (_steps == ESteps.RecordFiles)
            {
                var catalog = _loadBuiltinCatalogFileOp.Catalog;
                foreach (var wrapper in catalog.Wrappers)
                {
                    string filePath = PathUtility.Combine(_fileCache.RootPath, wrapper.FileName);
                    var entry = new BuiltinFileCacheEntry(wrapper.BundleGUID, filePath);
                    _fileCache.AddEntry(wrapper.BundleGUID, entry);
                }
            }
        }
    }
}
