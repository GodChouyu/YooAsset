
namespace YooAsset
{
    internal abstract class DownloadFileBaseOperation : AsyncOperationBase
    {
        /// <summary>
        /// 资源包对象
        /// </summary>
        public readonly PackageBundle Bundle;

        /// <summary>
        /// 下载地址
        /// </summary>
        public readonly string Url;

        /// <summary>
        /// 下载进度
        /// </summary>
        public float DownloadProgress { get; protected set; }

        /// <summary>
        /// 下载字节
        /// </summary>
        public long DownloadedBytes { get; protected set; }

        /// <summary>
        /// 引用计数
        /// </summary>
        public int RefCount { private set; get; }

        public DownloadFileBaseOperation(PackageBundle bundle, string url)
        {
            Bundle = bundle;
            Url = url;
        }
        internal override string InternalGetDescription()
        {
            return $"RefCount : {RefCount}";
        }

        /// <summary>
        /// 减少引用计数
        /// </summary>
        public void Release()
        {
            RefCount--;
        }

        /// <summary>
        /// 增加引用计数
        /// </summary>
        public void Reference()
        {
            RefCount++;
        }
    }
}