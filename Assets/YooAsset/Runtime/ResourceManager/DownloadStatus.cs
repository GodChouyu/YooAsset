
namespace YooAsset
{
    public struct DownloadStatus
    {
        /// <summary>
        /// 下载是否已经完成
        /// </summary>
        public bool IsDone { get; set; }

        /// <summary>
        /// 下载进度（0-1f)
        /// </summary>
        public float Progress { get; set; }

        /// <summary>
        /// 下载文件的总大小
        /// </summary>
        public long TotalBytes { get; set; }

        /// <summary>
        /// 已经下载的文件大小
        /// </summary>
        public long DownloadedBytes { get; set; }

        public static DownloadStatus CreateDefaultStatus()
        {
            DownloadStatus status = new DownloadStatus();
            return status;
        }
    }
}