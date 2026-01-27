
namespace YooAsset
{
    internal class TempFileInfo
    {
        public string TempFilePath { private set; get; }
        public uint TempFileCRC { private set; get; }
        public long TempFileSize { private set; get; }

        /// <summary>
        /// 注意：原子操作对象
        /// </summary>
        public volatile int Result = 0;

        public TempFileInfo(string filePath, uint fileCRC, long fileSize)
        {
            TempFilePath = filePath;
            TempFileCRC = fileCRC;
            TempFileSize = fileSize;
        }
    }
}