
namespace YooAsset
{
    public struct BundleEncryptArgs
    {
        /// <summary>
        /// 资源包名称
        /// </summary>
        internal string BundleName;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath;
    }

    public struct BundleEncryptResult
    {
        /// <summary>
        /// 文件是否加密
        /// </summary>
        public bool Encrypted;

        /// <summary>
        /// 加密后的文件数据
        /// </summary>
        public byte[] EncryptedFileData;
    }

    public interface IBundleEncryptor
    {
        BundleEncryptResult Encrypt(BundleEncryptArgs args);
    }
}