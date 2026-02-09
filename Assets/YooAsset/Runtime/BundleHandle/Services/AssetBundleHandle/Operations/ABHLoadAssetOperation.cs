using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// AssetBundle的加载单个资源操作
    /// </summary>
    internal class ABHLoadAssetOperation : BHLoadAssetOperation
    {
        protected enum ESteps
        {
            None,
            CheckBundle,
            LoadAsset,
            CheckResult,
            Done,
        }

        private readonly PackageBundle _packageBundle;
        private readonly AssetBundle _assetBundle;
        private readonly AssetInfo _assetInfo;
        private AssetBundleRequest _request;
        private ESteps _steps = ESteps.None;

        public ABHLoadAssetOperation(PackageBundle packageBundle, AssetBundle assetBundle, AssetInfo assetInfo)
        {
            _packageBundle = packageBundle;
            _assetBundle = assetBundle;
            _assetInfo = assetInfo;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckBundle)
            {
                if (_assetBundle == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"The bundle {_packageBundle.BundleName} has been destroyed due to Unity engine bugs.";
                    return;
                }

                _steps = ESteps.LoadAsset;
            }

            if (_steps == ESteps.LoadAsset)
            {
                if (IsWaitForCompletion)
                {
                    if (_assetInfo.AssetType == null)
                        Result = _assetBundle.LoadAsset(_assetInfo.AssetPath);
                    else
                        Result = _assetBundle.LoadAsset(_assetInfo.AssetPath, _assetInfo.AssetType);
                }
                else
                {
                    if (_assetInfo.AssetType == null)
                        _request = _assetBundle.LoadAssetAsync(_assetInfo.AssetPath);
                    else
                        _request = _assetBundle.LoadAssetAsync(_assetInfo.AssetPath, _assetInfo.AssetType);
                }

                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (_request != null)
                {
                    // 注意: 异步加载过程中，业务逻辑可能会强制转换为同步加载
                    if (IsWaitForCompletion)
                    {
                        // 强制挂起主线程（注意：该操作会很耗时）
                        YooLogger.Warning("Suspending the main thread to load Unity asset.");
                        Result = _request.asset;
                    }
                    else
                    {
                        Progress = _request.progress;
                        if (_request.isDone == false)
                            return;
                        Result = _request.asset;
                    }
                }

                if (Result == null)
                {
                    string error;
                    if (_assetInfo.AssetType == null)
                        error = $"Failed to load asset: {_assetInfo.AssetPath} AssetType: null AssetBundle: {_packageBundle.BundleName}";
                    else
                        error = $"Failed to load asset: {_assetInfo.AssetPath} AssetType: {_assetInfo.AssetType} AssetBundle: {_packageBundle.BundleName}";

                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = error;
                    YooLogger.Error(Error);
                }
                else
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