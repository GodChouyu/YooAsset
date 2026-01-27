
namespace YooAsset
{
    internal struct LoadBundleOptions
    {
        /// <summary>
        /// 资源包
        /// </summary>
        public readonly PackageBundle Bundle;

        public LoadBundleOptions(PackageBundle bundle)
        {
            Bundle = bundle;
        }
    }
}