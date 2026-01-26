
namespace YooAsset
{
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

        public ClearCacheOptions(EFileClearMode clearMode)
        {
            ClearMode = clearMode.ToString();
            ClearParam = null;
            Manifest = null;
        }
        public ClearCacheOptions(EFileClearMode clearMode, object clearParam)
        {
            ClearMode = clearMode.ToString();
            ClearParam = clearParam;
            Manifest = null;
        }
        public ClearCacheOptions(string clearMode, object clearParam)
        {
            ClearMode = clearMode;
            ClearParam = clearParam;
            Manifest = null;
        }
    }
}