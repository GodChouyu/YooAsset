
namespace YooAsset
{
    /// <summary>
    /// 清单文件常量定义
    /// </summary>
    internal class PackageManifestDefine
    {
        /// <summary>
        /// 文件极限大小（100MB）
        /// </summary>
        public const int MaxFileSize = 104857600;

        /// <summary>
        /// 文件头标记
        /// </summary>
        public const uint FileSignature = 0x594F4F;

        /// <summary>
        /// 当前文件格式版本
        /// </summary>
        public const string FileVersion = "2025.9.30";

        /// <summary>
        /// 兼容的最低版本号
        /// </summary>
        public const string VERSION_2025_8_28 = "2025.8.28";

        /// <summary>
        /// 版本号 2025.9.30
        /// </summary>
        public const string VERSION_2025_9_30 = "2025.9.30";
    }
}