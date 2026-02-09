using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YooAsset
{
    /// <summary>
    /// AssetBundle资源包句柄
    /// </summary>
    internal class AssetBundleHandle : IBundleHandle
    {
        private readonly string _bundleFilePath;
        private readonly PackageBundle _packageBundle;
        private readonly AssetBundle _assetBundle;
        private readonly Stream _managedStream;

        public AssetBundleHandle(string bundleFilePath, PackageBundle packageBundle, AssetBundle assetBundle, Stream managedStream)
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

        public BHLoadAssetOperation LoadAssetAsync(AssetInfo assetInfo)
        {
            var operation = new ABHLoadAssetOperation(_packageBundle, _assetBundle, assetInfo);
            return operation;
        }
        public BHLoadAllAssetsOperation LoadAllAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new ABHLoadAllAssetsOperation(_packageBundle, _assetBundle, assetInfo);
            return operation;
        }
        public BHLoadSubAssetsOperation LoadSubAssetsAsync(AssetInfo assetInfo)
        {
            var operation = new ABHLoadSubAssetsOperation(_packageBundle, _assetBundle, assetInfo);
            return operation;
        }
        public BHLoadSceneOperation LoadSceneAsync(AssetInfo assetInfo, LoadSceneParameters loadSceneParams, bool suspendLoad)
        {
            var operation = new ABHLoadSceneOperation(assetInfo, loadSceneParams, suspendLoad);
            return operation;
        }
    }
}