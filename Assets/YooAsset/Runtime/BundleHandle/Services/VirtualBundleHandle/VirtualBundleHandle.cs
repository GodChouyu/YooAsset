using UnityEngine.SceneManagement;

namespace YooAsset
{
    /// <summary>
    /// 虚拟资源包句柄
    /// </summary>
    internal class VirtualBundleHandle : IBundleHandle
    {
        private readonly string _bundleFilePath;
        private readonly PackageBundle _packageBundle;

        public VirtualBundleHandle(string bundleFilePath, PackageBundle bundle)
        {
            _bundleFilePath = bundleFilePath;
            _packageBundle = bundle;
        }
        public void UnloadBundleFile()
        {
        }
        public string GetBundleFilePath()
        {
            return _bundleFilePath;
        }

        public BHLoadAssetOperation LoadAssetAsync(AssetInfo assetInfo)
        {
            var operation = new VBHLoadAssetOperation(_packageBundle, assetInfo);
            return operation;
        }
        public BHLoadAllAssetsOperation LoadAllAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new VBHLoadAllAssetsOperation(_packageBundle, assetInfo);
            return operation;
        }
        public BHLoadSubAssetsOperation LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new VBHLoadSubAssetsOperation(_packageBundle, assetInfo);
            return operation;
        }
        public BHLoadSceneOperation LoadSceneAsync(AssetInfo assetInfo, LoadSceneParameters loadSceneParams, bool suspendLoad)
        {
            var operation = new VBHLoadSceneOperation(assetInfo, loadSceneParams, suspendLoad);
            return operation;
        }
    }
}