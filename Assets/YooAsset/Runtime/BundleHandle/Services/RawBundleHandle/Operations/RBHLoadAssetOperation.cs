
namespace YooAsset
{
    /// <summary>
    /// 原生资源包的加载单个资源操作
    /// </summary>
    internal class RBHLoadAssetOperation : BHLoadAssetOperation
    {
        protected enum ESteps
        {
            None,
            LoadObject,
            CheckResult,
            Done,
        }

        private readonly PackageBundle _packageBundle;
        private readonly RawBundle _rawBundle;
        private readonly AssetInfo _assetInfo;
        private ESteps _steps = ESteps.None;

        public RBHLoadAssetOperation(PackageBundle packageBundle, RawBundle rawBundle, AssetInfo assetInfo)
        {
            _packageBundle = packageBundle;
            _rawBundle = rawBundle;
            _assetInfo = assetInfo;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadObject;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadObject)
            {
                Result = _rawBundle.LoadRawFileObject();
                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (Result == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to load raw file object: {_assetInfo.AssetPath}";
                    YooLogger.Error(Error);
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }
        }
    }
}