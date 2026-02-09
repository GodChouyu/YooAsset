
namespace YooAsset
{
    /// <summary>
    /// 清理缓存操作选项
    /// </summary>
    internal struct FCClearCacheOptions
    {
        /// <summary>
        /// 清理模式
        /// </summary>
        public string ClearMode { get; set; }

        /// <summary>
        /// 附加参数
        /// </summary>
        public object ClearParam { get; set; }

        /// <summary>
        /// 资源清单
        /// </summary>
        public PackageManifest Manifest { get; set; }
    }
}
