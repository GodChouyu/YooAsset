
namespace YooAsset
{
    /// <summary>
    /// 加载资源包操作选项
    /// </summary>
    internal readonly struct FCLoadBundleOptions
    {
        /// <summary>
        /// 资源包
        /// </summary>
        public readonly PackageBundle Bundle;

        public FCLoadBundleOptions(PackageBundle bundle)
        {
            Bundle = bundle;
        }
    }
}