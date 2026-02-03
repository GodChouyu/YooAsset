using System;
using System.IO;

namespace YooAsset
{
    public struct BundleDecryptArgs
    {
        internal PackageBundle Bundle { get; set; }
        public byte[] FileData { get; set; }
        public string FilePath { get; set; }
    }

    public interface IBundleDecryptor
    {
    }
    public interface IBundleOffsetDecryptor : IBundleDecryptor
    {
        uint GetFileOffset(BundleDecryptArgs args);
    }
    public interface IBundleMemoryDecryptor : IBundleDecryptor
    {
        byte[] GetDecryptData(BundleDecryptArgs args);
    }
    public interface IBundleStreamDecryptor : IBundleDecryptor
    {
        uint GetReadBufferSize(BundleDecryptArgs args);
        Stream GetDecryptStream(BundleDecryptArgs args);
    }
}
