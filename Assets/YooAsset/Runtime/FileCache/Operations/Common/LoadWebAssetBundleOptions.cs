
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
        /// 备用请求地址
        /// </summary>
        public string FallbackURL { get; set; }

        /// <summary>
        /// AssetBundle 解密器
        /// </summary>
        public IBundleDecryptor AssetBundleDecryptor { get; set; }

        /// <summary>
        /// 下载后台接口
        /// </summary>
        public IDownloadBackend DownloadBackend { get; set; }

        /// <summary>
        /// 下载数据校验级别
        /// </summary>
        public EFileVerifyLevel DownloadVerifyLevel { get; set; }

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
