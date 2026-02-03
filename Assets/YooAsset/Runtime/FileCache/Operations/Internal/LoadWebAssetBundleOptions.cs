
namespace YooAsset
{
    /// <summary>
    /// 加载 AssetBundle 的上下文信息
    /// </summary>
    internal struct LoadWebAssetBundleOptions
    {
        /// <summary>
        /// 文件缓存名称
        /// </summary>
        public string CacheName { get; set; }

        /// <summary>
        /// 资源包信息
        /// </summary>
        public PackageBundle Bundle { get; set; }

        /// <summary>
        /// 请求地址
        /// </summary>
        public string MainURL { get; set; }

        /// <summary>
        /// 请求地址
        /// </summary>
        public string FallbackURL { get; set; }

        /// <summary>
        /// 解密接口
        /// </summary>
        public IBundleDecryptor Decryptor { get; set; }

        /// <summary>
        /// 下载后台接口
        /// </summary>
        public IDownloadBackend DownloadBackend { get; set; }

        /// <summary>
        /// 看门狗超时时间
        /// </summary>
        public int WatchdogTimeout { get; set; }

        /// <summary>
        /// 禁用Unity的网络缓存
        /// </summary>
        public bool DisableUnityWebCache { get; set; }
    }
}
