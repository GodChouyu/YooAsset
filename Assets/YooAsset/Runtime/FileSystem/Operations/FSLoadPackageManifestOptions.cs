
namespace YooAsset
{
    /// <summary>
    /// 加载包裹清单操作选项
    /// </summary>
    internal readonly struct FSLoadPackageManifestOptions
    {
        /// <summary>
        /// 包裹版本
        /// </summary>
        public readonly string PackageVersion;

        /// <summary>
        /// 超时时间
        /// </summary>
        public readonly int Timeout;

        public FSLoadPackageManifestOptions(string packageVersion, int timeout)
        {
            PackageVersion = packageVersion;
            Timeout = timeout;
        }
    }
}
