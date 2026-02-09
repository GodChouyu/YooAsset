using UnityEngine.SceneManagement;

namespace YooAsset
{
    /// <summary>
    /// 资源包句柄接口，提供对已加载资源包的操作能力
    /// </summary>
    internal interface IBundleHandle
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
        /// 加载资源对象
        /// </summary>
        BHLoadAssetOperation LoadAssetAsync(AssetInfo assetInfo);

        /// <summary>
        /// 加载所有资源对象
        /// </summary>
        BHLoadAllAssetsOperation LoadAllAssetsAsync(AssetInfo assetInfo);

        /// <summary>
        /// 加载资源对象及所有子资源对象
        /// </summary>
        BHLoadSubAssetsOperation LoadSubAssetsAsync(AssetInfo assetInfo);

        /// <summary>
        /// 加载场景对象
        /// </summary>
        BHLoadSceneOperation LoadSceneAsync(AssetInfo assetInfo, LoadSceneParameters loadSceneParams, bool suspendLoad);
    }
}