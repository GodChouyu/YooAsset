using UnityEngine.SceneManagement;

namespace YooAsset
{
    /// <summary>
    /// 原生资源包句柄
    /// </summary>
    internal class RawBundleHandle : IBundleHandle
    {
        private readonly string _bundleFilePath;
        private readonly PackageBundle _packageBundle;
        private readonly RawBundle _rawBundle;

        public RawBundleHandle(string bundleFilePath, PackageBundle packageBundle, RawBundle rawBundle)
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

        public BHLoadAssetOperation LoadAssetAsync(AssetInfo assetInfo)
        {
            var operation = new RBHLoadAssetOperation(_packageBundle, _rawBundle, assetInfo);
            return operation;
        }
        public BHLoadAllAssetsOperation LoadAllAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new RBHLoadAllAssetsOperation();
            return operation;
        }
        public BHLoadSubAssetsOperation LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new RBHLoadSubAssetsOperation();
            return operation;
        }
        public BHLoadSceneOperation LoadSceneAsync(AssetInfo assetInfo, LoadSceneParameters loadSceneParams, bool suspendLoad)
        {
            var operation = new RBHLoadSceneOperation();
            return operation;
        }
    }
}