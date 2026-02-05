
namespace YooAsset
{
    /// <summary>
    /// 内置资源目录常量定义
    /// </summary>
    internal class BuiltinCatalogDefine
    {
        /// <summary>
        /// 文件极限大小（100MB）
        /// </summary>
        public const int MaxFileSize = 104857600;

        /// <summary>
        /// 文件头标记
        /// </summary>
        public const uint FileHeader = 0x133C5EE;

        /// <summary>
        /// 文件格式版本
        /// </summary>
        public const string FileVersion = "1.0.0";


        /// <summary>
        /// JSON文件名称
        /// </summary>
        public const string JsonFileName = "BuiltinCatalog.json";

        /// <summary>
        /// 二进制文件名称
        /// </summary>
        public const string BinaryFileName = "BuiltinCatalog.bytes";
    }
}