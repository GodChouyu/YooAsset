using System.Collections.Generic;

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
        /// 候选下载地址列表
        /// </summary>
        public IReadOnlyList<string> URLs { get; private set; }

        /// <summary>
        /// 创建Web远端文件缓存条目
        /// </summary>
        /// <param name="bundleGUID">资源包唯一标识</param>
        /// <param name="urls">候选下载地址列表</param>
        public WebRemoteFileCacheEntry(string bundleGUID, IReadOnlyList<string> urls)
        {
            BundleGUID = bundleGUID;
            URLs = urls;
        }
    }
}
