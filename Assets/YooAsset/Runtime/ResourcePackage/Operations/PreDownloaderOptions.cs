
namespace YooAsset
{
    /// <summary>
    /// 预下载选项
    /// </summary>
    public struct PreDownloaderOptions
    {
        /// <summary>
        /// 预下载的包裹版本
        /// </summary>
        public string PackageVersion { get; set; }

        /// <summary>
        /// 资源清单请求超时时间
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 创建预下载选项
        /// </summary>
        /// <param name="packageVersion">预下载的包裹版本</param>
        /// <param name="timeout">资源清单请求超时时间（秒）</param>
        public PreDownloaderOptions(string packageVersion, int timeout)
        {
            PackageVersion = packageVersion;
            Timeout = timeout;
        }
    }
}