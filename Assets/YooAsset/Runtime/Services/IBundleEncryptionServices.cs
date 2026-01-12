
namespace YooAsset
{
    public struct BundleEncryptionContext
    {
        /// <summary>
        /// 资源包名称
        /// </summary>
        public string BundleName;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FileLoadPath;
    }
    public struct BundleEncryptionResult
    {
        /// <summary>
        /// 文件是否加密
        /// </summary>
        public bool Encrypted;

        /// <summary>
        /// 加密后的文件数据
        /// </summary>
        public byte[] EncryptedData;
    }
    public interface IBundleEncryptionServices
    {
        BundleEncryptionResult Encrypt(BundleEncryptionContext bundleInfo);
    }
}