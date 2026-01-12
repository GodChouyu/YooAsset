
namespace YooAsset
{
    /// <summary>
    /// 下载请求状态
    /// </summary>
    internal enum EDownloadRequestStatus
    {
        /// <summary>
        /// 未开始
        /// </summary>
        None,

        /// <summary>
        /// 进行中
        /// </summary>
        Running,

        /// <summary>
        /// 已成功
        /// </summary>
        Succeed,

        /// <summary>
        /// 已失败
        /// </summary>
        Failed,

        /// <summary>
        /// 已中止
        /// </summary>
        /// <remarks>
        /// 可能由用户主动调用 AbortRequest() 或看门狗超时触发。
        /// </remarks>
        Aborted,
    }
}
