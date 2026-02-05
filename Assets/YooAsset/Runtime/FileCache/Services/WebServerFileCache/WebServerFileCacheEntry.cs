
namespace YooAsset
{
    /// <summary>
    /// Web服务器文件缓存条目
    /// </summary>
    internal class WebServerFileCacheEntry : ICacheEntry
    {
        /// <summary>
        /// 资源包唯一标识
        /// </summary>
        public string BundleGUID { get; private set; }

        /// <summary>
        /// 资源包文件路径
        /// </summary>
        public string FilePath { get; private set; }

        /// <summary>
        /// 创建Web服务器文件缓存条目
        /// </summary>
        /// <param name="bundleGUID">资源包唯一标识</param>
        /// <param name="filePath">资源包文件路径</param>
        public WebServerFileCacheEntry(string bundleGUID, string filePath)
        {
            BundleGUID = bundleGUID;
            FilePath = filePath;
        }
    }
}