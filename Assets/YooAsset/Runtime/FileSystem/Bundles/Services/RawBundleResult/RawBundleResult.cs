using UnityEngine.SceneManagement;

namespace YooAsset
{
    internal class RawBundleResult : IBundleResult
    {
        private readonly string _bundleFilePath;
        private readonly PackageBundle _packageBundle;
        private readonly RawBundle _rawBundle;

        public RawBundleResult(string bundleFilePath, PackageBundle packageBundle, RawBundle rawBundle)
        {
            _bundleFilePath = bundleFilePath;
            _packageBundle = packageBundle;
            _rawBundle = rawBundle;
        }
        public string GetBundleFilePath()
        {
            return _bundleFilePath;
        }
        public void UnloadBundleFile()
        {
            if (_rawBundle != null)
            {
                _rawBundle.Unload();
            }
        }

        public FSLoadAssetOperation LoadAssetAsync(AssetInfo assetInfo)
        {
            var operation = new RawBundleLoadAssetOperation(_packageBundle, _rawBundle, assetInfo);
            return operation;
        }
        public FSLoadAllAssetsOperation LoadAllAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new RawBundleLoadAllAssetsOperation();
            return operation;
        }
        public FSLoadSubAssetsOperation LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new RawBundleLoadSubAssetsOperation();
            return operation;
        }
        public FSLoadSceneOperation LoadSceneOperation(AssetInfo assetInfo, LoadSceneParameters loadParams, bool suspendLoad)
        {
            var operation = new RawBundleLoadSceneOperation();
            return operation;
        }
    }
}