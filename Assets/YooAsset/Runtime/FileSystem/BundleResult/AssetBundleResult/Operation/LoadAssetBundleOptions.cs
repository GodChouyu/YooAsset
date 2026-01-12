
namespace YooAsset
{
    /// <summary>
    /// 加载 AssetBundle 的上下文信息
    /// </summary>
    public struct LoadAssetBundleOptions
    {
        /// <summary>
        /// 文件加载路径
        /// </summary>
        internal string FileLoadPath { get; set; }

        /// <summary>
        /// 资源包信息
        /// </summary>
        internal PackageBundle Bundle { get; set; }
    }
}
