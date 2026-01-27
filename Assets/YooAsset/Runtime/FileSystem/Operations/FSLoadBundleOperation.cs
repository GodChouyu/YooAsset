
namespace YooAsset
{
    internal abstract class FSLoadBundleOperation : AsyncOperationBase
    {
        /// <summary>
        /// 加载结果
        /// </summary>
        public IBundleResult Result { protected set; get; }

        /// <summary>
        /// 下载进度
        /// </summary>
        public float DownloadProgress { protected set; get; }

        /// <summary>
        /// 下载大小
        /// </summary>
        public long DownloadedBytes { protected set; get; }

        /// <summary>
        /// 终止下载文件
        /// </summary>
        public bool AbortDownloadFile = false;
    }
}