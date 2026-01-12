
namespace YooAsset
{
    /// <summary>
    /// 下载完成事件参数
    /// </summary>
    public struct DownloadFinishedEventArgs
    {
        /// <summary>
        /// 所属包裹名称
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 如果下载失败，获取的错误信息。
        /// </summary>
        public string ErrorInfo { get; set; }

        /// <summary>
        /// 创建表示成功下载的事件参数
        /// </summary>
        internal static DownloadFinishedEventArgs CreateSuccess(string packageName)
        {
            var args = new DownloadFinishedEventArgs();
            args.PackageName = packageName;
            args.IsSuccess = true;
            args.ErrorInfo = null;
            return args;
        }

        /// <summary>
        /// 创建表示失败下载的事件参数
        /// </summary>
        internal static DownloadFinishedEventArgs CreateFailure(string packageName, string errorInfo)
        {
            var args = new DownloadFinishedEventArgs();
            args.PackageName = packageName;
            args.IsSuccess = false;
            args.ErrorInfo = errorInfo;
            return args;
        }
    }

    /// <summary>
    /// 下载完成事件委托
    /// </summary>
    public delegate void DownloadFinishedEventHandler(DownloadFinishedEventArgs args);


    /// <summary>
    /// 下载进度更新事件参数
    /// </summary>
    public struct DownloadProgressChangedEventArgs
    {
        /// <summary>
        /// 所属包裹名称
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// 下载进度 (0-1f)
        /// </summary>
        public float Progress { get; set; }

        /// <summary>
        /// 下载文件总数
        /// </summary>
        public int TotalDownloadCount { get; set; }

        /// <summary>
        /// 下载数据总大小（单位：字节）
        /// </summary>
        public long TotalDownloadBytes { get; set; }

        /// <summary>
        /// 当前完成的下载文件数量
        /// </summary>
        public int CurrentDownloadCount { get; set; }

        /// <summary>
        /// 当前完成的下载数据大小（单位：字节）
        /// </summary>
        public long CurrentDownloadBytes { get; set; }
    }

    /// <summary>
    /// 下载进度更新事件委托
    /// </summary>
    public delegate void DownloadProgressChangedEventHandler(DownloadProgressChangedEventArgs args);


    /// <summary>
    /// 下载错误事件参数
    /// </summary>
    public struct DownloadErrorEventArgs
    {
        /// <summary>
        /// 所属包裹名称
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// 下载失败的文件名称
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string ErrorInfo { get; set; }
    }

    /// <summary>
    /// 下载错误事件委托
    /// </summary>
    public delegate void DownloadErrorEventHandler(DownloadErrorEventArgs args);


    /// <summary>
    /// 开始下载单个文件事件参数
    /// </summary>
    public struct DownloadFileStartedEventArgs
    {
        /// <summary>
        /// 所属包裹名称
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// 资源包名称
        /// </summary>
        public string BundleName { get; set; }

        /// <summary>
        /// 文件名称
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 文件大小
        /// </summary>
        public long FileSize { get; set; }
    }

    /// <summary>
    /// 开始下载单个文件事件委托
    /// </summary>
    public delegate void DownloadFileStartedEventHandler(DownloadFileStartedEventArgs args);
}