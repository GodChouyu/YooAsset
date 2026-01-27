
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
        public int RetryCount { get; set; }

        /// <summary>
        /// 下载资源对象所属资源包内所有资源对象依赖的资源包
        /// </summary>
        public bool DownloadBundleDependencies { get; set; }

        /// <summary>
        /// 资源信息列表
        /// 说明：如果列表为NULL，则下载所有资产
        /// </summary>
        public AssetInfo[] AssetInfos { get; set; }

        /// <summary>
        /// 创建资源下载选项（单个资源）
        /// </summary>
        /// <param name="assetInfo">资源信息</param>
        /// <param name="downloadDependencies">是否下载依赖资源包</param>
        /// <param name="maximumConcurrency">最大并发数量</param>
        /// <param name="retryCount">失败后的重试次数</param>
        public BundleDownloaderOptions(AssetInfo assetInfo, bool downloadDependencies, int maximumConcurrency, int retryCount)
        {
            AssetInfos = new AssetInfo[] { assetInfo };
            DownloadBundleDependencies = downloadDependencies;
            MaximumConcurrency = maximumConcurrency;
            RetryCount = retryCount;
        }

        /// <summary>
        /// 创建资源下载选项（多个资源）
        /// </summary>
        /// <param name="assetInfos">资源信息数组</param>
        /// <param name="downloadDependencies">是否下载依赖资源包</param>
        /// <param name="maximumConcurrency">最大并发数量</param>
        /// <param name="retryCount">失败后的重试次数</param>
        public BundleDownloaderOptions(AssetInfo[] assetInfos, bool downloadDependencies, int maximumConcurrency, int retryCount)
        {
            AssetInfos = assetInfos;
            DownloadBundleDependencies = downloadDependencies;
            MaximumConcurrency = maximumConcurrency;
            RetryCount = retryCount;
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
        public int RetryCount { get; set; }

        /// <summary>
        /// 资源标签列表
        /// 说明：如果列表为NULL，则下载所有资产
        /// </summary>
        public string[] Tags { get; set; }

        /// <summary>
        /// 创建资源下载选项（下载所有资源）
        /// </summary>
        /// <param name="maximumConcurrency">最大并发数量</param>
        /// <param name="retryCount">失败后的重试次数</param>
        public ResourceDownloaderOptions(int maximumConcurrency, int retryCount)
        {
            Tags = null;
            MaximumConcurrency = maximumConcurrency;
            RetryCount = retryCount;
        }

        /// <summary>
        /// 创建资源下载选项（按标签下载）
        /// </summary>
        /// <param name="tag">资源标签</param>
        /// <param name="maximumConcurrency">最大并发数量</param>
        /// <param name="retryCount">失败后的重试次数</param>
        public ResourceDownloaderOptions(string tag, int maximumConcurrency, int retryCount)
        {
            Tags = new string[] { tag };
            MaximumConcurrency = maximumConcurrency;
            RetryCount = retryCount;
        }

        /// <summary>
        /// 创建资源下载选项（按多个标签下载）
        /// </summary>
        /// <param name="tags">资源标签数组</param>
        /// <param name="maximumConcurrency">最大并发数量</param>
        /// <param name="retryCount">失败后的重试次数</param>
        public ResourceDownloaderOptions(string[] tags, int maximumConcurrency, int retryCount)
        {
            Tags = tags;
            MaximumConcurrency = maximumConcurrency;
            RetryCount = retryCount;
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
        public int RetryCount { get; set; }

        /// <summary>
        /// 资源标签列表
        /// 说明：如果列表为NULL，则解压所有资产
        /// </summary>
        public string[] Tags { get; set; }

        /// <summary>
        /// 创建资源解压选项（解压所有资源）
        /// </summary>
        /// <param name="maximumConcurrency">最大并发数量</param>
        /// <param name="retryCount">失败后的重试次数</param>
        public ResourceUnpackerOptions(int maximumConcurrency, int retryCount)
        {
            Tags = null;
            MaximumConcurrency = maximumConcurrency;
            RetryCount = retryCount;
        }

        /// <summary>
        /// 创建资源解压选项（按标签解压）
        /// </summary>
        /// <param name="tag">资源标签</param>
        /// <param name="maximumConcurrency">最大并发数量</param>
        /// <param name="retryCount">失败后的重试次数</param>
        public ResourceUnpackerOptions(string tag, int maximumConcurrency, int retryCount)
        {
            Tags = new string[] { tag };
            MaximumConcurrency = maximumConcurrency;
            RetryCount = retryCount;
        }

        /// <summary>
        /// 创建资源解压选项（按多个标签解压）
        /// </summary>
        /// <param name="tags">资源标签数组</param>
        /// <param name="maximumConcurrency">最大并发数量</param>
        /// <param name="retryCount">失败后的重试次数</param>
        public ResourceUnpackerOptions(string[] tags, int maximumConcurrency, int retryCount)
        {
            Tags = tags;
            MaximumConcurrency = maximumConcurrency;
            RetryCount = retryCount;
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
        public int RetryCount { get; set; }

        /// <summary>
        /// 资源包信息列表
        /// </summary>
        public ImportBundleInfo[] BundleInfos { get; set; }

        /// <summary>
        /// 创建资源导入选项
        /// </summary>
        /// <param name="bundleInfos">资源包信息数组</param>
        /// <param name="maximumConcurrency">最大并发数量</param>
        /// <param name="retryCount">失败后的重试次数</param>
        public BundleImporterOptions(ImportBundleInfo[] bundleInfos, int maximumConcurrency, int retryCount)
        {
            BundleInfos = bundleInfos;
            MaximumConcurrency = maximumConcurrency;
            RetryCount = retryCount;
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