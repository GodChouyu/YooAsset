using System.Collections.Generic;

namespace YooAsset
{
    internal class ClearCacheBundleFilesByTagsOperation : FSClearCacheOperation
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
        private string[] _tags;
        private ESteps _steps = ESteps.None;

        internal ClearCacheBundleFilesByTagsOperation(CacheFileSystem fileSystem, PackageManifest manifest, object clearParam)
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
                    _tags = new string[] { _clearParam as string };
                }
                else if (_clearParam is List<string>)
                {
                    var tempList = _clearParam as List<string>;
                    _tags = tempList.ToArray();
                }
                else if (_clearParam is string[])
                {
                    _tags = _clearParam as string[];
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
            var allEntrys = _fileSystem.Cache.GetAllEntries();
            List<string> result = new List<string>(allEntrys.Count);
            foreach (var entry in allEntrys)
            {
                if (_manifest.TryGetPackageBundleByBundleGUID(entry.BundleGUID, out PackageBundle bundle))
                {
                    if (bundle.HasTag(_tags))
                    {
                        result.Add(bundle.BundleGUID);
                    }
                }
            }
            return result;
        }
    }
}