
namespace YooAsset
{
    /// <summary>
    /// 下载文件操作的抽象基类
    /// </summary>
    internal abstract class FSDownloadFileOperation : AsyncOperationBase
    {
        /// <summary>
        /// 关联的资源包信息
        /// </summary>
        public PackageBundle Bundle { get; private set; }

        /// <summary>
        /// 下载报告
        /// </summary>
        public DownloadReport Report { get; protected set; }

        public FSDownloadFileOperation(PackageBundle bundle)
        {
            Bundle = bundle;
        }
    }
}