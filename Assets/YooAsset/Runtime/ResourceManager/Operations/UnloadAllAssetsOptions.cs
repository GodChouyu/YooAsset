
namespace YooAsset
{
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

        public UnloadAllAssetsOptions(bool releaseAllHandles, bool lockLoadOperation)
        {
            ReleaseAllHandles = releaseAllHandles;
            LockLoadOperation = lockLoadOperation;
        }
    }
}