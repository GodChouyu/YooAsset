
namespace YooAsset
{
    /// <summary>
    /// 清理缓存选项
    /// </summary>
    public struct ClearCacheOptions
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
        internal PackageManifest Manifest { get; set; }

        /// <summary>
        /// 创建清理缓存选项
        /// </summary>
        /// <param name="clearMode">清理模式</param>
        public ClearCacheOptions(EFileClearMode clearMode)
        {
            ClearMode = clearMode.ToString();
            ClearParam = null;
            Manifest = null;
        }

        /// <summary>
        /// 创建清理缓存选项
        /// </summary>
        /// <param name="clearMode">清理模式</param>
        /// <param name="clearParam">附加参数</param>
        public ClearCacheOptions(EFileClearMode clearMode, object clearParam)
        {
            ClearMode = clearMode.ToString();
            ClearParam = clearParam;
            Manifest = null;
        }

        /// <summary>
        /// 创建清理缓存选项
        /// </summary>
        /// <param name="clearMode">清理模式</param>
        public ClearCacheOptions(EManifestClearMode clearMode)
        {
            ClearMode = clearMode.ToString();
            ClearParam = null;
            Manifest = null;
        }

        /// <summary>
        /// 创建清理缓存选项
        /// </summary>
        /// <param name="clearMode">清理模式</param>
        /// <param name="clearParam">附加参数</param>
        public ClearCacheOptions(EManifestClearMode clearMode, object clearParam)
        {
            ClearMode = clearMode.ToString();
            ClearParam = clearParam;
            Manifest = null;
        }

        /// <summary>
        /// 创建清理缓存选项
        /// </summary>
        /// <param name="clearMode">清理模式名称</param>
        /// <param name="clearParam">附加参数</param>
        public ClearCacheOptions(string clearMode, object clearParam)
        {
            ClearMode = clearMode;
            ClearParam = clearParam;
            Manifest = null;
        }
    }
}