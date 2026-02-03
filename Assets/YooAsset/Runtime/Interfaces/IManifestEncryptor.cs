
namespace YooAsset
{
    /// <summary>
    /// 资源清单加密器
    /// </summary>
    public interface IManifestEncryptor
    {
        byte[] Encrypt(byte[] fileData);
    }
}