using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YooAsset
{
    internal class AssetBundleResult : IBundleResult
    {
        private readonly string _bundleFilePath;
        private readonly PackageBundle _packageBundle;
        private readonly AssetBundle _assetBundle;
        private readonly Stream _managedStream;

        public AssetBundleResult(string bundleFilePath, PackageBundle packageBundle, AssetBundle assetBundle, Stream managedStream)
        {
            _bundleFilePath = bundleFilePath;
            _packageBundle = packageBundle;
            _assetBundle = assetBundle;
            _managedStream = managedStream;
        }
        public string GetBundleFilePath()
        {
            return _bundleFilePath;
        }
        public void UnloadBundleFile()
        {
            if (_assetBundle != null)
            {
                _assetBundle.Unload(true);
            }

            if (_managedStream != null)
            {
                _managedStream.Close();
                _managedStream.Dispose();
            }
        }

        public FSLoadAssetOperation LoadAssetAsync(AssetInfo assetInfo)
        {
            var operation = new AssetBundleLoadAssetOperation(_packageBundle, _assetBundle, assetInfo);
            return operation;
        }
        public FSLoadAllAssetsOperation LoadAllAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new AssetBundleLoadAllAssetsOperation(_packageBundle, _assetBundle, assetInfo);
            return operation;
        }
        public FSLoadSubAssetsOperation LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new AssetBundleLoadSubAssetsOperation(_packageBundle, _assetBundle, assetInfo);
            return operation;
        }
        public FSLoadSceneOperation LoadSceneOperation(AssetInfo assetInfo, LoadSceneParameters loadParams, bool suspendLoad)
        {
            var operation = new AssetBundleLoadSceneOperation(assetInfo, loadParams, suspendLoad);
            return operation;
        }
    }
}