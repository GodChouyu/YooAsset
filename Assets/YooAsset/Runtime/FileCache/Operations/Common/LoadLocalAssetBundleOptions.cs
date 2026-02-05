
namespace YooAsset
{
    /// <summary>
    /// 加载 AssetBundle 的上下文信息
    /// </summary>
    internal struct LoadLocalAssetBundleOptions
    {
        /// <summary>
        /// 文件缓存名称
        /// </summary>
        public string CacheName { get; set; }

        /// <summary>
        /// 资源包信息
        /// </summary>
        public PackageBundle Bundle { get; set; }

        /// <summary>
        /// 文件加载路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// AssetBundle 解密器
        /// </summary>
        public IBundleDecryptor AssetBundleDecryptor { get; set; }
    }
}

