
namespace YooAsset
{
    internal class WSFCInitializeOperation : FCInitializeOperation
    {
        private enum ESteps
        {
            None,
            LoadCatalogFile,
            RecordFiles,
            Done,
        }

        private readonly WebServerFileCache _fileCache;
        private LoadWebServerCatalogOperation _loadWebCatalogFileOp;
        private ESteps _steps = ESteps.None;

        public WSFCInitializeOperation(WebServerFileCache cache)
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
                if (_loadWebCatalogFileOp == null)
                {
                    _loadWebCatalogFileOp = new LoadWebServerCatalogOperation(_fileCache);
                    _loadWebCatalogFileOp.StartOperation();
                    AddChildOperation(_loadWebCatalogFileOp);
                }

                _loadWebCatalogFileOp.UpdateOperation();
                if (_loadWebCatalogFileOp.IsDone == false)
                    return;

                if (_loadWebCatalogFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.RecordFiles;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadWebCatalogFileOp.Error;
                }
            }

            if (_steps == ESteps.RecordFiles)
            {
                var catalog = _loadWebCatalogFileOp.Catalog;
                foreach (var wrapper in catalog.Wrappers)
                {
                    string filePath = PathUtility.Combine(_fileCache.RootPath, wrapper.FileName);
                    var entry = new WebServerFileCacheEntry(wrapper.BundleGUID, filePath);
                    _fileCache.AddEntry(wrapper.BundleGUID, entry);
                }
            }
        }
    }
}
