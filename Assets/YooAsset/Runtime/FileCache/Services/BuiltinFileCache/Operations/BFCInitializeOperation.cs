
namespace YooAsset
{
    /// <summary>
    /// 内置文件缓存初始化操作
    /// </summary>
    internal class BFCInitializeOperation : FCInitializeOperation
    {
        private enum ESteps
        {
            None,
            LoadCatalog,
            RecordEntry,
            Done,
        }

        private readonly BuiltinFileCache _fileCache;
        private LoadBuiltinCatalogOperation _loadBuiltinCatalogOp;
        private ESteps _steps = ESteps.None;

        public BFCInitializeOperation(BuiltinFileCache fileCache)
        {
            _fileCache = fileCache;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadCatalog;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadCatalog)
            {
                if (_loadBuiltinCatalogOp == null)
                {
                    var options = new LoadBuiltinCatalogOptions();
                    options.PackageName = _fileCache.PackageName;
                    options.FilePath = _fileCache.GetCatalogBinaryFileLoadPath();
                    options.DownloadBackend = _fileCache.Config.DownloadBackend;
                    _loadBuiltinCatalogOp = new LoadBuiltinCatalogOperation(options);
                    _loadBuiltinCatalogOp.StartOperation();
                    AddChildOperation(_loadBuiltinCatalogOp);
                }

                _loadBuiltinCatalogOp.UpdateOperation();
                if (_loadBuiltinCatalogOp.IsDone == false)
                    return;

                if (_loadBuiltinCatalogOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.RecordEntry;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadBuiltinCatalogOp.Error;
                }
            }

            if (_steps == ESteps.RecordEntry)
            {
                var catalog = _loadBuiltinCatalogOp.Catalog;
                foreach (var fileEntry in catalog.FileEntries)
                {
                    string filePath = PathUtility.Combine(_fileCache.RootPath, fileEntry.FileName);
                    var cacheEntry = new BuiltinFileCacheEntry(fileEntry.BundleGUID, filePath);
                    _fileCache.AddEntry(fileEntry.BundleGUID, cacheEntry);
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
    }
}
