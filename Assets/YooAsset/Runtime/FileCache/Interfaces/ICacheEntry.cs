
namespace YooAsset
{
    /// <summary>
    /// 缓存记录接口
    /// </summary>
    internal interface ICacheEntry
    {
        /// <summary>
        /// Bundle唯一标识
        /// </summary>
        string BundleGUID { get; }
    }
}
