
namespace YooAsset
{
    /// <summary>
    /// 卸载所有资源的选项配置
    /// </summary>
    public struct UnloadAllAssetsOptions
    {
        /// <summary>
        /// 释放所有资源句柄，防止卸载过程中触发完成回调！
        /// </summary>
        public bool ReleaseAllHandles { set; get; }

        /// <summary>
        /// 卸载过程中锁定加载操作，防止新的任务请求！
        /// </summary>
        public bool LockLoadOperation { set; get; }

        /// <summary>
        /// 创建卸载所有资源的选项
        /// </summary>
        /// <param name="releaseAllHandles">是否释放所有句柄</param>
        /// <param name="lockLoadOperation">是否锁定加载操作</param>
        public UnloadAllAssetsOptions(bool releaseAllHandles, bool lockLoadOperation)
        {
            ReleaseAllHandles = releaseAllHandles;
            LockLoadOperation = lockLoadOperation;
        }
    }
}