
namespace YooAsset
{
    /// <summary>
    /// 加载 RawBundle 的上下文信息
    /// </summary>
    internal struct LoadLocalRawBundleOptions
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
        /// 解密接口
        /// </summary>
        public IBundleDecryptor Decryptor { get; set; }
    }
}
