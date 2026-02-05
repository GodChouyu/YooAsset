
namespace YooAsset
{
    /// <summary>
    /// Web远端文件缓存条目
    /// </summary>
    internal class WebRemoteFileCacheEntry : ICacheEntry
    {
        /// <summary>
        /// 资源包唯一标识
        /// </summary>
        public string BundleGUID { get; private set; }

        /// <summary>
        /// 主下载地址
        /// </summary>
        public string MainURL { get; private set; }

        /// <summary>
        /// 备用下载地址
        /// </summary>
        public string FallbackURL { get; private set; }

        /// <summary>
        /// 创建Web远端文件缓存条目
        /// </summary>
        /// <param name="bundleGUID">资源包唯一标识</param>
        /// <param name="mainURL">主下载地址</param>
        /// <param name="fallbackURL">备用下载地址</param>
        public WebRemoteFileCacheEntry(string bundleGUID, string mainURL, string fallbackURL)
        {
            BundleGUID = bundleGUID;
            MainURL = mainURL;
            FallbackURL = fallbackURL;
        }
    }
}