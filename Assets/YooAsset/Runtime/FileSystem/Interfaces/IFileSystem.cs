
namespace YooAsset
{
    /// <summary>
    /// 文件系统接口
    /// </summary>
    internal interface IFileSystem
    {
        /// <summary>
        /// 包裹名称
        /// </summary>
        string PackageName { get; }

        /// <summary>
        /// 初始化文件系统
        /// </summary>
        FSInitializeOperation InitializeAsync();

        /// <summary>
        /// 查询包裹版本
        /// </summary>
        FSRequestPackageVersionOperation RequestPackageVersionAsync(FSRequestPackageVersionOptions options);

        /// <summary>
        /// 加载包裹清单
        /// </summary>
        FSLoadPackageManifestOperation LoadPackageManifestAsync(FSLoadPackageManifestOptions options);

        /// <summary>
        /// 加载Bundle文件
        /// </summary>
        FSLoadPackageBundleOperation LoadPackageBundleAsync(FSLoadPackageBundleOptions options);

        /// <summary>
        /// 下载Bundle文件
        /// </summary>
        FSDownloadFileOperation DownloadFileAsync(FSDownloadFileOptions options);

        /// <summary>
        /// 清理缓存文件
        /// </summary>
        FSClearCacheOperation ClearCacheAsync(FSClearCacheOptions options);


        /// <summary>
        /// 设置自定义参数
        /// </summary>
        void SetParameter(string name, object value);

        /// <summary>
        /// 创建文件系统
        /// </summary>
        void OnCreate(string packageName, string packageRoot);

        /// <summary>
        /// 销毁文件系统
        /// </summary>
        void OnDestroy();


        /// <summary>
        /// 查询文件归属
        /// </summary>
        bool Belong(PackageBundle bundle);

        /// <summary>
        /// 是否需要下载
        /// </summary>
        bool NeedDownload(PackageBundle bundle);

        /// <summary>
        /// 是否需要解压
        /// </summary>
        bool NeedUnpack(PackageBundle bundle);

        /// <summary>
        /// 是否需要导入
        /// </summary>
        bool NeedImport(PackageBundle bundle);
    }
}