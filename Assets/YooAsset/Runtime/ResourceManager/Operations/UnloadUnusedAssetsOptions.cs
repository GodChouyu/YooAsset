
namespace YooAsset
{
    /// <summary>
    /// 卸载未使用资源的选项配置
    /// </summary>
    public struct UnloadUnusedAssetsOptions
    {
        /// <summary>
        /// 循环迭代次数
        /// </summary>
        public int LoopCount { set; get; }

        /// <summary>
        /// 创建卸载未使用资源的选项
        /// </summary>
        /// <param name="loopCount">循环迭代次数</param>
        public UnloadUnusedAssetsOptions(int loopCount)
        {
            LoopCount = loopCount;
        }
    }
}