
namespace YooAsset
{
    internal abstract class FSDownloadFileOperation : AsyncOperationBase
    {
        public PackageBundle Bundle { private set; get; }

        /// <summary>
        /// 当前下载的字节数
        /// </summary>
        public long DownloadedBytes { protected set; get; }

        /// <summary>
        /// 当前下载进度（0f - 1f）
        /// </summary>
        public float DownloadProgress { protected set; get; }


        public FSDownloadFileOperation(PackageBundle bundle)
        {
            Bundle = bundle;
            DownloadedBytes = 0;
            DownloadProgress = 0;
        }
    }
}