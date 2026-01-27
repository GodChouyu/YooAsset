using UnityEngine.SceneManagement;

namespace YooAsset
{
    internal class VirtualBundleResult : IBundleResult
    {
        private readonly string _bundleFilePath;
        private readonly PackageBundle _packageBundle;

        public VirtualBundleResult(string bundleFilePath, PackageBundle bundle)
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

        public FSLoadAssetOperation LoadAssetAsync(AssetInfo assetInfo)
        {
            var operation = new VirtualBundleLoadAssetOperation(_packageBundle, assetInfo);
            return operation;
        }
        public FSLoadAllAssetsOperation LoadAllAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new VirtualBundleLoadAllAssetsOperation(_packageBundle, assetInfo);
            return operation;
        }
        public FSLoadSubAssetsOperation LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new VirtualBundleLoadSubAssetsOperation(_packageBundle, assetInfo);
            return operation;
        }
        public FSLoadSceneOperation LoadSceneOperation(AssetInfo assetInfo, LoadSceneParameters loadParams, bool suspendLoad)
        {
            var operation = new VirtualBundleLoadSceneOperation(assetInfo, loadParams, suspendLoad);
            return operation;
        }
    }
}