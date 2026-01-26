using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    internal sealed class ClearAllCacheBundleFilesOperation : FSClearCacheOperation
    {
        private enum ESteps
        {
            None,
            ClearCacheFiles,
            Done,
        }

        private readonly CacheFileSystem _fileSystem;
        private FCClearCacheOperation _clearCacheFileOp;
        private ESteps _steps = ESteps.None;


        internal ClearAllCacheBundleFilesOperation(CacheFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.ClearCacheFiles;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

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
                result.Add(entry.BundleGUID);
            }
            return result;
        }
    }
}