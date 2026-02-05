
namespace YooAsset
{
    /// <summary>
    /// 临时的文件信息，用于存储待验证的下载文件
    /// </summary>
    internal class TempFileInfo
    {
        /// <summary>
        /// 临时文件路径
        /// </summary>
        public string FilePath { private set; get; }

        /// <summary>
        /// 文件CRC校验值
        /// </summary>
        public uint FileCRC { private set; get; }

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { private set; get; }

        /// <summary>
        /// 验证结果码（原子操作对象，用于线程安全）
        /// </summary>
        public volatile int VerifyResultCode = 0;

        /// <summary>
        /// 创建临时文件信息
        /// </summary>
        /// <param name="filePath">临时文件路径</param>
        /// <param name="fileCRC">文件CRC校验值</param>
        /// <param name="fileSize">文件大小（字节）</param>
        public TempFileInfo(string filePath, uint fileCRC, long fileSize)
        {
            FilePath = filePath;
            FileCRC = fileCRC;
            FileSize = fileSize;
        }
    }
}