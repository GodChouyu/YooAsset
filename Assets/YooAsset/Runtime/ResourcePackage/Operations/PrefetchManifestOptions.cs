
namespace YooAsset
{
    /// <summary>
    /// 预取清单选项
    /// </summary>
    public struct PrefetchManifestOptions
    {
        /// <summary>
        /// 预取的包裹版本
        /// </summary>
        public string PackageVersion { get; set; }

        /// <summary>
        /// 资源清单请求超时时间
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// 创建预取清单选项
        /// </summary>
        /// <param name="packageVersion">预取的包裹版本</param>
        /// <param name="timeout">资源清单请求超时时间（秒）</param>
        public PrefetchManifestOptions(string packageVersion, int timeout)
        {
            PackageVersion = packageVersion;
            Timeout = timeout;
        }
    }
}