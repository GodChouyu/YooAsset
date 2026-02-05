
namespace YooAsset
{
    /// <summary>
    /// 内置文件缓存加载 AssetBundle 操作
    /// </summary>
    internal class BFCLoadAssetBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            GetEntry,
            LoadBundle,
            Done,
        }

        private readonly BuiltinFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private LoadLocalAssetBundleOperation _loadLocalAssetBundleOp;
        private BuiltinFileCacheEntry _cacheEntry;
        private ESteps _steps = ESteps.None;

        public BFCLoadAssetBundleOperation(BuiltinFileCache fileCache, PackageBundle bundle)
        {
            _fileCache = fileCache;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.GetEntry;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.GetEntry)
            {
                _cacheEntry = _fileCache.GetEntry(_bundle.BundleGUID);
                if (_cacheEntry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"File cache entry not found: {_bundle.BundleGUID}";
                }
                else
                {
                    _steps = ESteps.LoadBundle;
                }
            }

            if (_steps == ESteps.LoadBundle)
            {
                if (_loadLocalAssetBundleOp == null)
                {
                    var options = new LoadLocalAssetBundleOptions();
                    options.CacheName = _fileCache.GetType().Name;
                    options.Bundle = _bundle;
                    options.FilePath = _cacheEntry.FilePath;
                    options.AssetBundleDecryptor = _fileCache.Config.AssetBundleDecryptor;
                    _loadLocalAssetBundleOp = new LoadLocalAssetBundleOperation(options);
                    _loadLocalAssetBundleOp.StartOperation();
                    AddChildOperation(_loadLocalAssetBundleOp);
                }

                if (IsWaitForCompletion)
                    _loadLocalAssetBundleOp.WaitForCompletion();

                _loadLocalAssetBundleOp.UpdateOperation();
                if (_loadLocalAssetBundleOp.IsDone == false)
                    return;

                if (_loadLocalAssetBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadLocalAssetBundleOp.BundleResult == null)
                        throw new YooInternalException("Loaded bundle result is null.");

                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleResult = _loadLocalAssetBundleOp.BundleResult;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadLocalAssetBundleOp.Error;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }

    /// <summary>
    /// 内置文件缓存加载 RawBundle 操作
    /// </summary>
    internal class BFCLoadRawBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            GetEntry,
            LoadBundle,
            Done,
        }

        private readonly BuiltinFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private LoadLocalRawBundleOperation _loadLocalRawBundleOp;
        private BuiltinFileCacheEntry _cacheEntry;
        private ESteps _steps = ESteps.None;

        public BFCLoadRawBundleOperation(BuiltinFileCache fileCache, PackageBundle bundle)
        {
            _fileCache = fileCache;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.GetEntry;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.GetEntry)
            {
                _cacheEntry = _fileCache.GetEntry(_bundle.BundleGUID);
                if (_cacheEntry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"File cache entry not found: {_bundle.BundleGUID}";
                }
                else
                {
                    _steps = ESteps.LoadBundle;
                }
            }

            if (_steps == ESteps.LoadBundle)
            {
                if(_loadLocalRawBundleOp == null)
                {
                    var options = new LoadLocalRawBundleOptions();
                    options.CacheName = _fileCache.GetType().Name;
                    options.Bundle = _bundle;
                    options.FilePath = _cacheEntry.FilePath;
                    options.RawBundleDecryptor = _fileCache.Config.RawBundleDecryptor;
                    _loadLocalRawBundleOp = new LoadLocalRawBundleOperation(options);
                    _loadLocalRawBundleOp.StartOperation();
                    AddChildOperation(_loadLocalRawBundleOp);
                }

                if (IsWaitForCompletion)
                    _loadLocalRawBundleOp.WaitForCompletion();

                _loadLocalRawBundleOp.UpdateOperation();
                if (_loadLocalRawBundleOp.IsDone == false)
                    return;

                if(_loadLocalRawBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadLocalRawBundleOp.BundleResult == null)
                        throw new YooInternalException("Loaded bundle result is null.");

                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleResult = _loadLocalRawBundleOp.BundleResult;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadLocalRawBundleOp.Error;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}