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
public class TestFileStreamEncryption : IBundleEncryptor
{
    public BundleEncryptResult Encrypt(BundleEncryptArgs fileInfo)
    {
        // 说明：对TestRes3资源目录进行加密
        if (fileInfo.BundleName.Contains("_testres3_"))
        {
            var fileData = File.ReadAllBytes(fileInfo.FilePath);
            for (int i = 0; i < fileData.Length; i++)
            {
                fileData[i] ^= BundleStream.KEY;
            }

            BundleEncryptResult result = new BundleEncryptResult();
            result.Encrypted = true;
            result.EncryptedFileData = fileData;
            return result;
        }
        else
        {
            BundleEncryptResult result = new BundleEncryptResult();
            result.Encrypted = false;
            return result;
        }
    }
}
public class TestFileOffsetEncryption : IBundleEncryptor
{
    public BundleEncryptResult Encrypt(BundleEncryptArgs fileInfo)
    {
        // 说明：对TestRes3资源目录进行加密
        if (fileInfo.BundleName.Contains("_testres3_"))
        {
            int offset = 32;
            byte[] fileData = File.ReadAllBytes(fileInfo.FilePath);
            var encryptedData = new byte[fileData.Length + offset];
            Buffer.BlockCopy(fileData, 0, encryptedData, offset, fileData.Length);

            BundleEncryptResult result = new BundleEncryptResult();
            result.Encrypted = true;
            result.EncryptedFileData = encryptedData;
            return result;
        }
        else
        {
            BundleEncryptResult result = new BundleEncryptResult();
            result.Encrypted = false;
            return result;
        }
    }
}

public class TestFileOffsetDecryption : IBundleOffsetDecryptor
{
    private const uint FILE_OFFSET = 32;

    uint IBundleOffsetDecryptor.GetFileOffset(BundleDecryptArgs args)
    {
        return FILE_OFFSET;
    }
}
public class TestFileMemoryDecryption : IBundleMemoryDecryptor
{
    byte[] IBundleMemoryDecryptor.GetDecryptData(BundleDecryptArgs args)
    {
        byte[] data = args.FileData;

        // 注意：如果数据为空，自行加载文件数据。
        if (data == null)
            data = FileUtility.ReadAllBytes(args.FilePath);

        for (int i = 0; i < data.Length; i++)
        {
            data[i] ^= BundleStream.KEY;
        }
        return data;
    }
}
public class TestFileStreamDecryption : IBundleStreamDecryptor
{
    Stream IBundleStreamDecryptor.GetDecryptStream(BundleDecryptArgs args)
    {
        var fileStream = new BundleStream(args.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return fileStream;
    }

    uint IBundleStreamDecryptor.GetReadBufferSize(BundleDecryptArgs args)
    {
        return 1024;
    }
}