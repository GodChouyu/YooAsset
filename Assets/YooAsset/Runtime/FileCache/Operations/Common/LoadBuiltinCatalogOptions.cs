
namespace YooAsset
{
    /// <summary>
    /// 加载内置资源目录操作选项
    /// </summary>
    internal struct LoadBuiltinCatalogOptions
    {
        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath {  get; set; }

        /// <summary>
        /// 下载后台
        /// </summary>
        public IDownloadBackend DownloadBackend { get; set; }
    }
}