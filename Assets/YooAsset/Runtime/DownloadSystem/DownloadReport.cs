
namespace YooAsset
{
    public struct DownloadReport
    {
        /// <summary>
        /// HTTP 返回码
        /// </summary>
        public long HttpCode { get; set; }

        /// <summary>
        /// HTTP 错误信息
        /// </summary>
        public string HttpError { get; set; }

        /// <summary>
        /// 当前下载的字节数
        /// </summary>
        public long DownloadedBytes { get; set; }

        /// <summary>
        /// 当前下载进度（0f - 1f）
        /// </summary>
        public float DownloadProgress { get; set; }

        /// <summary>
        /// 创建默认的下载进度实例
        /// </summary>
        public static DownloadReport Default
        {
            get
            {
                var result = new DownloadReport();
                return result;
            }
        }
    }
}