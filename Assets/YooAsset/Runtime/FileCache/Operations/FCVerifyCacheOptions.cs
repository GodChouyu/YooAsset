
namespace YooAsset
{
    internal struct VerifyCacheOptions
    {
        /// <summary>
        /// 要验证的资源包
        /// </summary>
        public PackageBundle Bundle { get; set; }

        /// <summary>
        /// 失败后直接移除缓存
        /// </summary>
        public bool FailedDeleteCache { get; set; }
    }
}
