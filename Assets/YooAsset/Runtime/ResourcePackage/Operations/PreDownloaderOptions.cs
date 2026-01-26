
namespace YooAsset
{
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

        public PreDownloaderOptions(string packageVersion, int timeout)
        {
            PackageVersion = packageVersion;
            Timeout = timeout;
        }
    }
}