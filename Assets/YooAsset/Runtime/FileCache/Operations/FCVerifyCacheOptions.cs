
namespace YooAsset
{
    /// <summary>
    /// 验证缓存操作选项
    /// </summary>
    internal struct FCVerifyCacheOptions
    {
        /// <summary>
        /// 要验证的资源包
        /// </summary>
        public PackageBundle Bundle { get; set; }

        /// <summary>
        /// 失败后直接移除缓存条目
        /// </summary>
        public bool DeleteCacheEntryOnFailure { get; set; }
    }
}
