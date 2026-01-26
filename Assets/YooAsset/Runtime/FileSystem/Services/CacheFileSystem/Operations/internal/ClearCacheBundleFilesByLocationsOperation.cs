using System.Collections.Generic;

namespace YooAsset
{
    internal class ClearCacheBundleFilesByLocationsOperation : FSClearCacheOperation
    {
        private enum ESteps
        {
            None,
            CheckManifest,
            CheckArgs,
            ClearCacheFiles,
            Done,
        }

        private readonly CacheFileSystem _fileSystem;
        private readonly PackageManifest _manifest;
        private readonly object _clearParam;
        private FCClearCacheOperation _clearCacheFileOp;
        private string[] _locations;
        private ESteps _steps = ESteps.None;

        internal ClearCacheBundleFilesByLocationsOperation(CacheFileSystem fileSystem, PackageManifest manifest, object clearParam)
        {
            _fileSystem = fileSystem;
            _manifest = manifest;
            _clearParam = clearParam;
        }

        internal override void InternalStart()
        {
            _steps = ESteps.CheckManifest;
        }

        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckManifest)
            {
                if (_manifest == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Can not found active package manifest.";
                }
                else
                {
                    _steps = ESteps.CheckArgs;
                }
            }

            if (_steps == ESteps.CheckArgs)
            {
                if (_clearParam == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Clear param is null.";
                    return;
                }

                if (_clearParam is string)
                {
                    _locations = new string[] { _clearParam as string };
                }
                else if (_clearParam is List<string>)
                {
                    var tempList = _clearParam as List<string>;
                    _locations = tempList.ToArray();
                }
                else if (_clearParam is string[])
                {
                    _locations = _clearParam as string[];
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Invalid clear param : {_clearParam.GetType().FullName}";
                    return;
                }

                _steps = ESteps.ClearCacheFiles;
            }

            if (_steps == ESteps.ClearCacheFiles)
            {
                if (_clearCacheFileOp == null)
                {
                    var options = new FCClearCacheOptions();
                    options.BundleGUIDs = GetRemoveFiles();
                    _clearCacheFileOp = _fileSystem.Cache.ClearCacheAsync(options);
                    _clearCacheFileOp.StartOperation();
                    AddChildOperation(_clearCacheFileOp);
                }

                _clearCacheFileOp.UpdateOperation();
                Progress = _clearCacheFileOp.Progress;
                if (_clearCacheFileOp.IsDone == false)
                    return;

                if (_clearCacheFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _clearCacheFileOp.Error;
                }
            }
        }

        private List<string> GetRemoveFiles()
        {
            List<string> result = new List<string>(_locations.Length);
            foreach (var location in _locations)
            {
                string assetPath = _manifest.TryMappingToAssetPath(location);
                if (_manifest.TryGetPackageAsset(assetPath, out PackageAsset packageAsset))
                {
                    PackageBundle bundle = _manifest.GetMainPackageBundle(packageAsset.BundleID);
                    if (bundle != null)
                    {
                        result.Add(bundle.BundleGUID);
                    }
                }
            }
            return result;
        }
    }
}