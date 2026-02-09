
namespace YooAsset
{
    /// <summary>
    /// 子资源提供者，负责加载资源包内的子资源
    /// </summary>
    internal sealed class SubAssetsProvider : ProviderBase
    {
        private BHLoadSubAssetsOperation _loadSubAssetsOp;
        
        public SubAssetsProvider(ResourceManager manager, string providerGUID, AssetInfo assetInfo) : base(manager, providerGUID, assetInfo)
        {
        }
        protected override void ProcessBundleHandle()
        {
            if (_loadSubAssetsOp == null)
            {
                _loadSubAssetsOp = LoadedBundleHandle.LoadSubAssetsAsync(MainAssetInfo);
                _loadSubAssetsOp.StartOperation();
                AddChildOperation(_loadSubAssetsOp);

#if UNITY_WEBGL
                if (_resourceManager.WebGLForceSyncLoadAsset)
                    _loadSubAssetsOp.WaitForAsyncComplete();
#endif
            }

            if (IsWaitForCompletion)
                _loadSubAssetsOp.WaitForCompletion();

            _loadSubAssetsOp.UpdateOperation();
            Progress = _loadSubAssetsOp.Progress;
            if (_loadSubAssetsOp.IsDone == false)
                return;

            if (_loadSubAssetsOp.Status != EOperationStatus.Succeeded)
            {
                InvokeCompletion(_loadSubAssetsOp.Error, EOperationStatus.Failed);
            }
            else
            {
                SubAssetObjects = _loadSubAssetsOp.Result;
                InvokeCompletion(string.Empty, EOperationStatus.Succeeded);
            }
        }
    }
}