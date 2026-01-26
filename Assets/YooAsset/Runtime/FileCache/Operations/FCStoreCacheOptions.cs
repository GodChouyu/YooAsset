
namespace YooAsset
{
    internal struct FCStoreCacheOptions
    {
        /// <summary>
        /// 要缓存的资源包
        /// </summary>
        public PackageBundle Bundle { get; set; }

        /// <summary>
        /// 要缓存的文件路径
        /// </summary>
        public string FilePath { get; set; }
    }
}
