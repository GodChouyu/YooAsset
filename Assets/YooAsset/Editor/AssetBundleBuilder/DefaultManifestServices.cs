
namespace YooAsset.Editor
{
    public class ManifestEncryptorNone : IManifestEncryptor
    {
        byte[] IManifestEncryptor.Encrypt(byte[] fileData)
        {
            return fileData;
        }
    }
    
    public class ManifestDecryptorNone : IManifestDecryptor
    {
        byte[] IManifestDecryptor.Decrypt(byte[] fileData)
        {
            return fileData;
        }
    }
}