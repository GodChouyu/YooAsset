using System;
using System.IO;
using YooAsset;

public class BundleStream : FileStream
{
    public const byte KEY = 64;

    public BundleStream(string path, FileMode mode, FileAccess access, FileShare share) : base(path, mode, access, share)
    {
    }
    public BundleStream(string path, FileMode mode) : base(path, mode)
    {
    }
    public override int Read(byte[] array, int offset, int count)
    {
        var index = base.Read(array, offset, count);
        for (int i = 0; i < array.Length; i++)
        {
            array[i] ^= KEY;
        }
        return index;
    }
}
public class TestFileStreamEncryption : IBundleEncryptionServices
{
    public BundleEncryptionResult Encrypt(BundleEncryptionContext fileInfo)
    {
        // 说明：对TestRes3资源目录进行加密
        if (fileInfo.BundleName.Contains("_testres3_"))
        {
            var fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
            for (int i = 0; i < fileData.Length; i++)
            {
                fileData[i] ^= BundleStream.KEY;
            }

            BundleEncryptionResult result = new BundleEncryptionResult();
            result.Encrypted = true;
            result.EncryptedData = fileData;
            return result;
        }
        else
        {
            BundleEncryptionResult result = new BundleEncryptionResult();
            result.Encrypted = false;
            return result;
        }
    }
}
public class TestFileOffsetEncryption : IBundleEncryptionServices
{
    public BundleEncryptionResult Encrypt(BundleEncryptionContext fileInfo)
    {
        // 说明：对TestRes3资源目录进行加密
        if (fileInfo.BundleName.Contains("_testres3_"))
        {
            int offset = 32;
            byte[] fileData = File.ReadAllBytes(fileInfo.FileLoadPath);
            var encryptedData = new byte[fileData.Length + offset];
            Buffer.BlockCopy(fileData, 0, encryptedData, offset, fileData.Length);

            BundleEncryptionResult result = new BundleEncryptionResult();
            result.Encrypted = true;
            result.EncryptedData = encryptedData;
            return result;
        }
        else
        {
            BundleEncryptionResult result = new BundleEncryptionResult();
            result.Encrypted = false;
            return result;
        }
    }
}

public class TestLoadAssetBundleFromOffsetOperation : DefaultLoadAssetBundleFromOffsetOperation
{
    private const uint FILE_OFFSET = 32;

    public TestLoadAssetBundleFromOffsetOperation(LoadAssetBundleOptions options) : base(options) { }

    protected override uint GetFileOffset()
    {
        return FILE_OFFSET;
    }
}
public class TestLoadAssetBundleFromMemoryOperation : DefaultLoadAssetBundleFromMemoryOperation
{
    public TestLoadAssetBundleFromMemoryOperation(LoadAssetBundleOptions options) : base(options) { }

    protected override byte[] DecryptData(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= BundleStream.KEY;
        }
        return data;
    }
}
public class TestLoadAssetBundleFromStreamOperation : DefaultLoadAssetBundleFromStreamOperation
{
    public TestLoadAssetBundleFromStreamOperation(LoadAssetBundleOptions options) : base(options) { }

    protected override FileStream CreateManagedFileStream()
    {
        var fileStream = new BundleStream(_options.FileLoadPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return fileStream;
    }
    protected override uint GetManagedReadBufferSize()
    {
        return 1024;
    }
    protected override byte[] DecryptData(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= BundleStream.KEY;
        }
        return data;
    }
}
public class TestWebAssetBundleFromMemoryDecryption : DefaultLoadWebAssetBundleFromMemoryOperation
{
    public TestWebAssetBundleFromMemoryDecryption(LoadWebAssetBundleOptions opionts) : base(opionts) { }

    protected override byte[] Decryption(byte[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= BundleStream.KEY;
        }

        return data;
    }
}
