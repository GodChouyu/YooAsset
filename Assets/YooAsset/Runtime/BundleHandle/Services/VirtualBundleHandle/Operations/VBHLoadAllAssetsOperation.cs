using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 虚拟资源包的加载所有资源操作
    /// </summary>
    internal class VBHLoadAllAssetsOperation : BHLoadAllAssetsOperation
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
        private readonly AssetInfo _assetInfo;
        private ESteps _steps = ESteps.None;

        public VBHLoadAllAssetsOperation(PackageBundle packageBundle, AssetInfo assetInfo)
        {
            _packageBundle = packageBundle;
            _assetInfo = assetInfo;
        }
        internal override void InternalStart()
        {
#if UNITY_EDITOR
            _steps = ESteps.CheckBundle;
#else
            _steps = ESteps.Done;
            Status = EOperationStatus.Failed;
            Error = $"{nameof(VirtualBundleLoadAllAssetsOperation)} only support unity editor platform.";
#endif
        }
        internal override void InternalUpdate()
        {
#if UNITY_EDITOR
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckBundle)
            {
                // 检测资源文件是否存在
                string guid = UnityEditor.AssetDatabase.AssetPathToGUID(_assetInfo.AssetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Asset not found: {_assetInfo.AssetPath}";
                    YooLogger.Error(Error);
                    return;
                }

                _steps = ESteps.LoadAsset;
            }

            if (_steps == ESteps.LoadAsset)
            {
                if (_assetInfo.AssetType == null)
                {
                    List<UnityEngine.Object> result = new List<UnityEngine.Object>();
                    foreach (var packageAsset in _packageBundle.IncludeMainAssets)
                    {
                        string assetPath = packageAsset.AssetPath;
                        UnityEngine.Object mainAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(assetPath);
                        if (mainAsset != null)
                            result.Add(mainAsset);
                    }
                    Result = result.ToArray();
                }
                else
                {
                    List<UnityEngine.Object> result = new List<UnityEngine.Object>();
                    foreach (var packageAsset in _packageBundle.IncludeMainAssets)
                    {
                        string assetPath = packageAsset.AssetPath;
                        UnityEngine.Object mainAsset = UnityEditor.AssetDatabase.LoadAssetAtPath(assetPath, _assetInfo.AssetType);
                        if (mainAsset != null)
                            result.Add(mainAsset);
                    }
                    Result = result.ToArray();
                }
                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (Result == null)
                {
                    string error;
                    if (_assetInfo.AssetType == null)
                        error = $"Failed to load all assets: {_assetInfo.AssetPath} AssetType: null";
                    else
                        error = $"Failed to load all assets: {_assetInfo.AssetPath} AssetType: {_assetInfo.AssetType}";

                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = error;
                    YooLogger.Error(error);
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }
#endif
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}