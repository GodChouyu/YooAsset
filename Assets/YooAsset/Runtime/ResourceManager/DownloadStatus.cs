
namespace YooAsset
{
    /// <summary>
    /// 下载状态信息结构体
    /// </summary>
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

        /// <summary>
        /// 创建默认的下载状态实例
        /// </summary>
        public static DownloadStatus CreateDefault()
        {
            DownloadStatus status = new DownloadStatus();
            return status;
        }
    }
}