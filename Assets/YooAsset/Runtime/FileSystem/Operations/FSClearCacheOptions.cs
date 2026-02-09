
namespace YooAsset
{
    /// <summary>
    /// 清理缓存操作选项
    /// </summary>
    internal struct FSClearCacheOptions
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

        /// <summary>
        /// 转换为 FileCache 的清理选项
        /// </summary>
        public FCClearCacheOptions ConvertTo()
        {
            var options = new FCClearCacheOptions();
            options.ClearMode = ClearMode;
            options.ClearParam = ClearParam;
            options.Manifest = Manifest;
            return options;
        }
    }
}
