
namespace YooAsset
{
    /// <summary>
    /// 资源提供者，负责加载单个资源对象
    /// </summary>
    internal sealed class AssetProvider : ProviderBase
    {
        private BHLoadAssetOperation _loadAssetOp;

        public AssetProvider(ResourceManager manager, string providerGUID, AssetInfo assetInfo) : base(manager, providerGUID, assetInfo)
        {
        }
        protected override void ProcessBundleHandle()
        {
            if (_loadAssetOp == null)
            {
                _loadAssetOp = LoadedBundleHandle.LoadAssetAsync(MainAssetInfo);
                _loadAssetOp.StartOperation();
                AddChildOperation(_loadAssetOp);

#if UNITY_WEBGL
                if (_resourceManager.WebGLForceSyncLoadAsset)
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