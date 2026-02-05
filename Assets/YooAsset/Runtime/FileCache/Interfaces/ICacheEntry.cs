
namespace YooAsset
{
    /// <summary>
    /// 缓存条目接口
    /// </summary>
    internal interface ICacheEntry
    {
        /// <summary>
        /// Bundle唯一标识
        /// </summary>
        string BundleGUID { get; }
    }
}
