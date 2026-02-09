using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 虚拟资源包的加载子资源操作
    /// </summary>
    internal class VBHLoadSubAssetsOperation : BHLoadSubAssetsOperation
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

        public VBHLoadSubAssetsOperation(PackageBundle packageBundle, AssetInfo assetInfo)
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
            Error = $"{nameof(VirtualBundleLoadSubAssetsOperation)} only support unity editor platform.";
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
                    Result = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(_assetInfo.AssetPath);
                }
                else
                {
                    UnityEngine.Object[] findAssets = UnityEditor.AssetDatabase.LoadAllAssetRepresentationsAtPath(_assetInfo.AssetPath);
                    List<UnityEngine.Object> result = new List<UnityEngine.Object>(findAssets.Length);
                    foreach (var findAsset in findAssets)
                    {
                        if (_assetInfo.AssetType.IsAssignableFrom(findAsset.GetType()))
                            result.Add(findAsset);
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
                        error = $"Failed to load sub-assets: {_assetInfo.AssetPath} AssetType: null";
                    else
                        error = $"Failed to load sub-assets: {_assetInfo.AssetPath} AssetType: {_assetInfo.AssetType}";

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