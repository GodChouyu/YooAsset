
namespace YooAsset
{
    /// <summary>
    /// 加载资源包操作选项
    /// </summary>
    internal readonly struct FSLoadPackageBundleOptions
    {
        /// <summary>
        /// 资源包
        /// </summary>
        public readonly PackageBundle Bundle;

        public FSLoadPackageBundleOptions(PackageBundle bundle)
        {
            Bundle = bundle;
        }

        /// <summary>
        /// 转换为 FileCache 的加载选项
        /// </summary>
        public FCLoadBundleOptions ConvertTo()
        {
            return new FCLoadBundleOptions(Bundle);
        }
    }
}
