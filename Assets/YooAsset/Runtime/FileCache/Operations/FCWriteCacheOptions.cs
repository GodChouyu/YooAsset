
namespace YooAsset
{
    internal struct WriteCacheOptions
    {
        /// <summary>
        /// 要缓存的资源包
        /// </summary>
        public PackageBundle Bundle { get; set; }

        /// <summary>
        /// 要缓存的文件路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 要缓存的文件数据
        /// </summary>
        public byte[] FileData { get; set; }
    }
}
