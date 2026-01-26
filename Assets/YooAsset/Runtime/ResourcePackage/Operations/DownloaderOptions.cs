
namespace YooAsset
{
    /// <summary>
    /// 资源下载选项
    /// </summary>
    public struct BundleDownloaderOptions
    {
        /// <summary>
        /// 最大并发数量
        /// </summary>
        public int MaximumConcurrency { get; set; }

        /// <summary>
        /// 失败后的重试次数
        /// </summary>
        public int FailedTryAgain { get; set; }

        /// <summary>
        /// 下载资源对象所属资源包内所有资源对象依赖的资源包
        /// </summary>
        public bool DownloadBundleDependencies { get; set; }

        /// <summary>
        /// 资源信息列表
        /// 说明：如果列表为NULL，则下载所有资产
        /// </summary>
        public AssetInfo[] AssetInfos { get; set; }

        public BundleDownloaderOptions(AssetInfo assetInfo, bool downloadDependencies, int maximumConcurrency, int failedTryAgain)
        {
            AssetInfos = new AssetInfo[] { assetInfo };
            DownloadBundleDependencies = downloadDependencies;
            MaximumConcurrency = maximumConcurrency;
            FailedTryAgain = failedTryAgain;
        }
        public BundleDownloaderOptions(AssetInfo[] assetInfos, bool downloadDependencies, int maximumConcurrency, int failedTryAgain)
        {
            AssetInfos = assetInfos;
            DownloadBundleDependencies = downloadDependencies;
            MaximumConcurrency = maximumConcurrency;
            FailedTryAgain = failedTryAgain;
        }
    }

    /// <summary>
    /// 资源下载选项
    /// </summary>
    public struct ResourceDownloaderOptions
    {
        /// <summary>
        /// 最大并发数量
        /// </summary>
        public int MaximumConcurrency { get; set; }

        /// <summary>
        /// 失败后的重试次数
        /// </summary>
        public int FailedTryAgain { get; set; }

        /// <summary>
        /// 资源标签列表
        /// 说明：如果列表为NULL，则下载所有资产
        /// </summary>
        public string[] Tags { get; set; }

        public ResourceDownloaderOptions(int maximumConcurrency, int failedTryAgain)
        {
            Tags = null;
            MaximumConcurrency = maximumConcurrency;
            FailedTryAgain = failedTryAgain;
        }
        public ResourceDownloaderOptions(string tag, int maximumConcurrency, int failedTryAgain)
        {
            Tags = new string[] { tag };
            MaximumConcurrency = maximumConcurrency;
            FailedTryAgain = failedTryAgain;
        }
        public ResourceDownloaderOptions(string[] tags, int maximumConcurrency, int failedTryAgain)
        {
            Tags = tags;
            MaximumConcurrency = maximumConcurrency;
            FailedTryAgain = failedTryAgain;
        }
    }

    /// <summary>
    /// 资源解压选项
    /// </summary>
    public struct ResourceUnpackerOptions
    {
        /// <summary> 
        /// 最大并发数量
        /// </summary>
        public int MaximumConcurrency { get; set; }

        /// <summary>
        /// 失败后的重试次数
        /// </summary>
        public int FailedTryAgain { get; set; }

        /// <summary>
        /// 资源标签列表
        /// 说明：如果列表为NULL，则解压所有资产
        /// </summary>
        public string[] Tags { get; set; }

        public ResourceUnpackerOptions(int maximumConcurrency, int failedTryAgain)
        {
            Tags = null;
            MaximumConcurrency = maximumConcurrency;
            FailedTryAgain = failedTryAgain;
        }
        public ResourceUnpackerOptions(string tag, int maximumConcurrency, int failedTryAgain)
        {
            Tags = new string[] { tag };
            MaximumConcurrency = maximumConcurrency;
            FailedTryAgain = failedTryAgain;
        }
        public ResourceUnpackerOptions(string[] tags, int maximumConcurrency, int failedTryAgain)
        {
            Tags = tags;
            MaximumConcurrency = maximumConcurrency;
            FailedTryAgain = failedTryAgain;
        }
    }

    /// <summary>
    /// 资源导入选项
    /// </summary>
    public struct BundleImporterOptions
    {
        /// <summary> 
        /// 最大并发数量
        /// </summary>
        public int MaximumConcurrency { get; set; }

        /// <summary>
        /// 失败后的重试次数
        /// </summary>
        public int FailedTryAgain { get; set; }

        /// <summary>
        /// 资源包信息列表
        /// </summary>
        public ImportBundleInfo[] BundleInfos { get; set; }

        public BundleImporterOptions(ImportBundleInfo[] bundleInfos, int maximumConcurrency, int failedTryAgain)
        {
            BundleInfos = bundleInfos;
            MaximumConcurrency = maximumConcurrency;
            FailedTryAgain = failedTryAgain;
        }
    }

    /// <summary>
    /// 导入的资源包信息
    /// </summary>
    public struct ImportBundleInfo
    {
        /// <summary>
        /// 本地文件路径
        /// </summary>
        public string FilePath;

        /// <summary>
        /// 资源包名称
        /// </summary>
        public string BundleName;

        /// <summary>
        /// 资源包GUID
        /// </summary>
        public string BundleGUID;
    }
}