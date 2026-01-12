
namespace YooAsset
{
    public struct UnloadUnusedAssetsOptions
    {
        /// <summary>
        /// 循环迭代次数
        /// </summary>
        public int LoopCount { set; get; }

        public UnloadUnusedAssetsOptions(int loopCount)
        {
            LoopCount = loopCount;
        }
    }
}