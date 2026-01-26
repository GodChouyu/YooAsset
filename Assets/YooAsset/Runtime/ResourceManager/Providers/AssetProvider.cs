
namespace YooAsset
{
    internal sealed class AssetProvider : ProviderOperation
    {
        private FSLoadAssetOperation _loadAssetOp;

        public AssetProvider(ResourceManager manager, string providerGUID, AssetInfo assetInfo) : base(manager, providerGUID, assetInfo)
        {
        }
        protected override void ProcessBundleResult()
        {
            if (_loadAssetOp == null)
            {
                _loadAssetOp = LoadedBundleResult.LoadAssetAsync(MainAssetInfo);
                _loadAssetOp.StartOperation();
                AddChildOperation(_loadAssetOp);

#if UNITY_WEBGL
                if (_resManager.WebGLForceSyncLoadAsset)
                    _loadAssetOp.WaitForAsyncComplete();
#endif
            }

            if (IsWaitForCompletion)
                _loadAssetOp.WaitForCompletion();

            _loadAssetOp.UpdateOperation();
            Progress = _loadAssetOp.Progress;
            if (_loadAssetOp.IsDone == false)
                return;

            if (_loadAssetOp.Status != EOperationStatus.Succeeded)
            {
                InvokeCompletion(_loadAssetOp.Error, EOperationStatus.Failed);
            }
            else
            {
                AssetObject = _loadAssetOp.Result;
                InvokeCompletion(string.Empty, EOperationStatus.Succeeded);
            }
        }
    }
}