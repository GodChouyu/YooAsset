using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 清理沙盒文件缓存操作
    /// </summary>
    internal abstract class SFCClearCacheOperation : FCClearCacheOperation
    {
        protected enum ESteps
        {
            None,
            ParseOptions,
            ClearCacheFiles,
            Done,
        }

        protected readonly SandboxFileCache _fileCache;
        protected readonly ClearCacheOptions _options;
        protected ClearCacheFilesOperation _clearCacheFilesOp;
        protected List<string> _bundleGUIDs;
        protected ESteps _steps = ESteps.None;

        internal SFCClearCacheOperation(SandboxFileCache fileCache, ClearCacheOptions options)
        {
            _fileCache = fileCache;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.ParseOptions;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.ParseOptions)
            {
                if (ParseOptionsStep() == false)
                    return;

                _clearCacheFilesOp = new ClearCacheFilesOperation(_fileCache, _bundleGUIDs);
                _clearCacheFilesOp.StartOperation();
                AddChildOperation(_clearCacheFilesOp);
                _steps = ESteps.ClearCacheFiles;
            }

            if (_steps == ESteps.ClearCacheFiles)
            {
                if (IsWaitForCompletion)
                    _clearCacheFilesOp.WaitForCompletion();

                _clearCacheFilesOp.UpdateOperation();
                Progress = _clearCacheFilesOp.Progress;
                if (_clearCacheFilesOp.IsDone == false)
                    return;

                if (_clearCacheFilesOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _clearCacheFilesOp.Error;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        protected abstract bool ParseOptionsStep();
    }

    /// <summary>
    /// 清理所有沙盒缓存操作
    /// </summary>
    internal sealed class SFCClearAllCacheOperation : SFCClearCacheOperation
    {
        internal SFCClearAllCacheOperation(SandboxFileCache fileCache, ClearCacheOptions options)
            : base(fileCache, options) { }

        protected override bool ParseOptionsStep()
        {
            var allEntries = _fileCache.GetAllEntries();
            _bundleGUIDs = new List<string>(allEntries.Count);
            foreach (var entry in allEntries)
            {
                _bundleGUIDs.Add(entry.BundleGUID);
            }
            return true;
        }
    }
    /// <summary>
    /// 清理未使用的沙盒缓存操作
    /// </summary>
    internal sealed class SFCClearUnusedCacheOperation : SFCClearCacheOperation
    {
        internal SFCClearUnusedCacheOperation(SandboxFileCache fileCache, ClearCacheOptions options)
            : base(fileCache, options) { }

        protected override bool ParseOptionsStep()
        {
            if (_options.Manifest == null)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Active package manifest not found.";
                return false;
            }

            var allEntries = _fileCache.GetAllEntries();
            _bundleGUIDs = new List<string>(allEntries.Count);
            foreach (var entry in allEntries)
            {
                if (_options.Manifest.IsIncludeBundleFile(entry.BundleGUID) == false)
                {
                    _bundleGUIDs.Add(entry.BundleGUID);
                }
            }
            return true;
        }
    }
    /// <summary>
    /// 按资源地址清理沙盒缓存操作
    /// </summary>
    internal sealed class SFCClearCacheByLocationsOperation : SFCClearCacheOperation
    {
        internal SFCClearCacheByLocationsOperation(SandboxFileCache fileCache, ClearCacheOptions options)
            : base(fileCache, options) { }

        protected override bool ParseOptionsStep()
        {
            if (_options.Manifest == null)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Active package manifest not found.";
                return false;
            }

            if (_options.ClearParam == null)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Clear param is null.";
                return false;
            }

            string[] locations;
            if (_options.ClearParam is string str)
                locations = new string[] { str };
            else if (_options.ClearParam is List<string> list)
                locations = list.ToArray();
            else if (_options.ClearParam is string[] array)
                locations = array;
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = $"Invalid clear param: {_options.ClearParam.GetType().FullName}";
                return false;
            }

            _bundleGUIDs = new List<string>(locations.Length);
            foreach (var location in locations)
            {
                string assetPath = _options.Manifest.TryMappingToAssetPath(location);
                if (_options.Manifest.TryGetPackageAsset(assetPath, out PackageAsset packageAsset))
                {
                    PackageBundle bundle = _options.Manifest.GetMainPackageBundle(packageAsset.BundleID);
                    _bundleGUIDs.Add(bundle.BundleGUID);
                }
            }
            return true;
        }
    }
    /// <summary>
    /// 按标签清理沙盒缓存操作
    /// </summary>
    internal sealed class SFCClearCacheByTagsOperation : SFCClearCacheOperation
    {
        internal SFCClearCacheByTagsOperation(SandboxFileCache fileCache, ClearCacheOptions options)
            : base(fileCache, options) { }

        protected override bool ParseOptionsStep()
        {
            if (_options.Manifest == null)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Active package manifest not found.";
                return false;
            }

            if (_options.ClearParam == null)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Clear param is null.";
                return false;
            }

            string[] tags;
            if (_options.ClearParam is string str)
                tags = new string[] { str };
            else if (_options.ClearParam is List<string> list)
                tags = list.ToArray();
            else if (_options.ClearParam is string[] array)
                tags = array;
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = $"Invalid clear param: {_options.ClearParam.GetType().FullName}";
                return false;
            }

            var allEntries = _fileCache.GetAllEntries();
            _bundleGUIDs = new List<string>(allEntries.Count);
            foreach (var entry in allEntries)
            {
                if (_options.Manifest.TryGetPackageBundleByBundleGUID(entry.BundleGUID, out PackageBundle bundle))
                {
                    if (bundle.HasTag(tags))
                        _bundleGUIDs.Add(bundle.BundleGUID);
                }
            }
            return true;
        }
    }
}
