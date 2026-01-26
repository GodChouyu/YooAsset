using System.Collections.Generic;
using System.Linq;

namespace YooAsset
{
    internal class FCClearCacheOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            CheckOptions,
            ClearCache,
            Done,
        }

        private BundleCache _cache;
        private FCClearCacheOptions _options;
        private int _clearFileTotalCount;
        private List<string> _bundleGUIDs;
        private ESteps _steps = ESteps.None;

        public FCClearCacheOperation(BundleCache cache, FCClearCacheOptions options)
        {
            _cache = cache;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckOptions;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckOptions)
            {
                if (_options.BundleGUIDs == null || _options.BundleGUIDs.Count == 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    return;
                }

                _bundleGUIDs = _options.BundleGUIDs.ToList();
                _clearFileTotalCount = _options.BundleGUIDs.Count;
                _steps = ESteps.ClearCache;
            }

            if (_steps == ESteps.ClearCache)
            {
                for (int i = _bundleGUIDs.Count - 1; i >= 0; i--)
                {
                    string bundleGUID = _bundleGUIDs[i];
                    _cache.RemoveEntry(bundleGUID);
                    _bundleGUIDs.RemoveAt(i);
                    if (IsBusy)
                        break;
                }

                if (_clearFileTotalCount == 0)
                    Progress = 1.0f;
                else
                    Progress = 1.0f - ((float)_bundleGUIDs.Count / _clearFileTotalCount);

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
