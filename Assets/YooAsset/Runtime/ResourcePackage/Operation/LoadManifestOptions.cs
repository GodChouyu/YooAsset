
namespace YooAsset
{
    public struct LoadManifestOptions
    {
        /// <summary>
        /// 包裹版本
        /// </summary>
        public string PackageVersion { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout { get; set; }

        public LoadManifestOptions(string packageVersion, int timeout)
        {
            PackageVersion = packageVersion;
            Timeout = timeout;
        }
    }
}