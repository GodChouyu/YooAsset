
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 清理沙盒文件缓存操作
    /// </summary>
    internal class EFCClearCacheOperation : FCClearCacheOperation
    {
        private enum ESteps
        {
            None,
            GetResult,
            ClearCacheFiles,
            Done,
        }

        private readonly EditorFileCache _fileCache;
        private readonly FCClearCacheOptions _options;
        private List<string> _bundleGUIDs;
        private ESteps _steps = ESteps.None;

        internal EFCClearCacheOperation(EditorFileCache fileCache, FCClearCacheOptions options)
        {
            _fileCache = fileCache;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.GetResult;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.GetResult)
            {
                ClearResult clearResult;
                if (_options.ClearMode == EFileClearMode.ClearAllBundleFiles.ToString())
                {
                    clearResult = GetAllCache(_fileCache.GetAllEntries());
                }
                else if (_options.ClearMode == EFileClearMode.ClearUnusedBundleFiles.ToString())
                {
                    clearResult = GetUnusedCache(_options, _fileCache.GetAllEntries());
                }
                else if (_options.ClearMode == EFileClearMode.ClearBundleFilesByLocations.ToString())
                {
                    clearResult = GetCacheByLocations(_options, _fileCache.GetAllEntries());
                }
                else if (_options.ClearMode == EFileClearMode.ClearBundleFilesByTags.ToString())
                {
                    clearResult = GetCacheByTags(_options, _fileCache.GetAllEntries());
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Invalid clear mode: {_options.ClearMode}";
                    return;
                }

                if (clearResult.Succeeded == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = clearResult.Error;
                    return;
                }

                _bundleGUIDs = clearResult.BundleGUIDs;
                _steps = ESteps.ClearCacheFiles;
            }

            if (_steps == ESteps.ClearCacheFiles)
            {
                foreach(var bundleGUID in _bundleGUIDs)
                {
                    _fileCache.RemoveEntry(bundleGUID);
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}
