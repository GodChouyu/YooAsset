using UnityEngine.SceneManagement;

namespace YooAsset
{
    internal class RawBundleResult : BundleResult
    {
        private readonly IFileSystem _fileSystem;
        private readonly PackageBundle _packageBundle;
        private readonly RawBundle _rawBundle;

        public RawBundleResult(IFileSystem fileSystem, PackageBundle packageBundle, RawBundle rawBundle)
        {
            _fileSystem = fileSystem;
            _packageBundle = packageBundle;
            _rawBundle = rawBundle;
        }

        public override void UnloadBundleFile()
        {
            if (_rawBundle != null)
            {
                _rawBundle.Unload();
            }
        }
        public override string GetBundleFilePath()
        {
            return _fileSystem.GetBundleFilePath(_packageBundle);
        }

        public override FSLoadAssetOperation LoadAssetAsync(AssetInfo assetInfo)
        {
            var operation = new RawBundleLoadAssetOperation(_packageBundle, _rawBundle, assetInfo);
            return operation;
        }
        public override FSLoadAllAssetsOperation LoadAllAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new RawBundleLoadAllAssetsOperation();
            return operation;
        }
        public override FSLoadSubAssetsOperation LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new RawBundleLoadSubAssetsOperation();
            return operation;
        }
        public override FSLoadSceneOperation LoadSceneOperation(AssetInfo assetInfo, LoadSceneParameters loadParams, bool suspendLoad)
        {
            var operation = new RawBundleLoadSceneOperation();
            return operation;
        }
    }
}