
namespace YooAsset
{
    /// <summary>
    /// 资源清单解密器
    /// </summary>
    public interface IManifestDecryptor
    {
        byte[] Decrypt(byte[] fileData);
    }
}