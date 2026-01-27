using UnityEngine.SceneManagement;

namespace YooAsset
{
    internal interface IBundleResult
    {
        /// <summary>
        /// 获取资源包文件的本地路径
        /// </summary>
        string GetBundleFilePath();

        /// <summary>
        /// 卸载资源包文件
        /// </summary>
        void UnloadBundleFile();

        /// <summary>
        /// 加载资源包内的资源对象
        /// </summary>
        FSLoadAssetOperation LoadAssetAsync(AssetInfo assetInfo);

        /// <summary>
        /// 加载资源包内的所有资源对象
        /// </summary>
        FSLoadAllAssetsOperation LoadAllAssetsAsync(AssetInfo assetInfo);

        /// <summary>
        /// 加载资源包内的资源对象及所有子资源对象
        /// </summary>
        FSLoadSubAssetsOperation LoadSubAssetsAsync(AssetInfo assetInfo);

        /// <summary>
        /// 加载资源包内的场景对象
        /// </summary>
        FSLoadSceneOperation LoadSceneOperation(AssetInfo assetInfo, LoadSceneParameters loadParams, bool suspendLoad);
    }
}