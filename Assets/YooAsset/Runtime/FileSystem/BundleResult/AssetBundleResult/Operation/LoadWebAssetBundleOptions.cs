
namespace YooAsset
{
    /// <summary>
    /// 加载 AssetBundle 的上下文信息
    /// </summary>
    public struct LoadWebAssetBundleOptions
    {
        /// <summary>
        /// 资源包信息
        /// </summary>
        internal PackageBundle Bundle { get; set; }

        /// <summary>
        /// 失败后重试次数
        /// </summary>
        internal int FailedTryAgain { get; set; }

        /// <summary>
        /// 看门狗超时时间
        /// </summary>
        internal int WatchdogTimeout { get; set; }

        /// <summary>
        /// 下载后台接口
        /// </summary>
        internal IDownloadBackend DownloadBackend { get; set; }

        /// <summary>
        /// 禁用Unity的网络缓存
        /// </summary>
        internal bool DisableUnityWebCache { get; set; }

        /// <summary>
        /// 主资源地址
        /// </summary>
        internal string MainURL { get; set; }

        /// <summary>
        /// 备用资源地址
        /// </summary>
        internal string FallbackURL { get; set; }
    }
}
