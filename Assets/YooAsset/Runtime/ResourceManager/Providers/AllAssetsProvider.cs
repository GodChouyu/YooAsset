
namespace YooAsset
{
    /// <summary>
    /// 全资源提供者，负责加载资源包内所有资源
    /// </summary>
    internal sealed class AllAssetsProvider : ProviderBase
    {
        private BHLoadAllAssetsOperation _loadAllAssetsOp;

        public AllAssetsProvider(ResourceManager manager, string providerGUID, AssetInfo assetInfo) : base(manager, providerGUID, assetInfo)
        {
        }
        protected override void ProcessBundleHandle()
        {
            if (_loadAllAssetsOp == null)
            {
                _loadAllAssetsOp = LoadedBundleHandle.LoadAllAssetsAsync(MainAssetInfo);
                _loadAllAssetsOp.StartOperation();
                AddChildOperation(_loadAllAssetsOp);

#if UNITY_WEBGL
                if (_resourceManager.WebGLForceSyncLoadAsset)
                    _loadAllAssetsOp.WaitForAsyncComplete();
#endif
            }

            if (IsWaitForCompletion)
                _loadAllAssetsOp.WaitForCompletion();

            _loadAllAssetsOp.UpdateOperation();
            Progress = _loadAllAssetsOp.Progress;
            if (_loadAllAssetsOp.IsDone == false)
                return;

            if (_loadAllAssetsOp.Status != EOperationStatus.Succeeded)
            {
                InvokeCompletion(_loadAllAssetsOp.Error, EOperationStatus.Failed);
            }
            else
            {
                AllAssetObjects = _loadAllAssetsOp.Result;
                InvokeCompletion(string.Empty, EOperationStatus.Succeeded);
            }
        }
    }
}