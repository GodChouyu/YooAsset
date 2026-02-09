
namespace YooAsset
{
    /// <summary>
    /// 加载资源包文件操作的抽象基类
    /// </summary>
    internal abstract class FSLoadPackageBundleOperation : AsyncOperationBase
    {
        /// <summary>
        /// 资源包句柄
        /// </summary>
        public IBundleHandle BundleHandle { get; protected set; }

        /// <summary>
        /// 终止下载文件
        /// </summary>
        public bool AbortDownloadFile = false;
    }
}