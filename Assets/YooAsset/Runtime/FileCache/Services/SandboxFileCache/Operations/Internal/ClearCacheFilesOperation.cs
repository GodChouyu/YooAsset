using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 清理缓存文件操作
    /// </summary>
    internal class ClearCacheFilesOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            CheckParam,
            ClearCache,
            Done,
        }

        private readonly SandboxFileCache _fileCache;
        private readonly List<string> _bundleGUIDs;
        private int _fileTotalCount;
        private ESteps _steps = ESteps.None;

        public ClearCacheFilesOperation(SandboxFileCache fileCache, List<string> bundleGUIDs)
        {
            _fileCache = fileCache;
            _bundleGUIDs = bundleGUIDs;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckParam;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckParam)
            {
                if (_bundleGUIDs == null || _bundleGUIDs.Count == 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    return;
                }

                _fileTotalCount = _bundleGUIDs.Count;
                _steps = ESteps.ClearCache;
            }

            if (_steps == ESteps.ClearCache)
            {
                for (int i = _bundleGUIDs.Count - 1; i >= 0; i--)
                {
                    string bundleGUID = _bundleGUIDs[i];
                    _fileCache.RemoveEntry(bundleGUID);
                    _bundleGUIDs.RemoveAt(i);
                    if (IsBusy)
                        break;
                }

                if (_fileTotalCount == 0)
                    Progress = 1.0f;
                else
                    Progress = 1.0f - ((float)_bundleGUIDs.Count / _fileTotalCount);

                if (_bundleGUIDs.Count == 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}
