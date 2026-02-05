
namespace YooAsset
{
    /// <summary>
    /// 资源包信息类
    /// </summary>
    internal class BundleInfo
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _importFilePath;

        /// <summary>
        /// 资源包对象
        /// </summary>
        public readonly PackageBundle Bundle;


        /// <summary>
        /// 创建资源包信息
        /// </summary>
        /// <param name="fileSystem">所属文件系统</param>
        /// <param name="bundle">资源包对象</param>
        public BundleInfo(IFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            Bundle = bundle;
            _importFilePath = null;
        }

        /// <summary>
        /// 创建资源包信息（带导入路径）
        /// </summary>
        /// <param name="fileSystem">所属文件系统</param>
        /// <param name="bundle">资源包对象</param>
        /// <param name="importFilePath">导入文件路径</param>
        public BundleInfo(IFileSystem fileSystem, PackageBundle bundle, string importFilePath)
        {
            _fileSystem = fileSystem;
            Bundle = bundle;
            _importFilePath = importFilePath;
        }

        /// <summary>
        /// 创建资源包加载器
        /// </summary>
        /// <returns>返回资源包加载操作对象</returns>
        public FSLoadBundleOperation CreateBundleLoader()
        {
            var options = new FCLoadBundleOptions(Bundle);
            return _fileSystem.LoadBundleAsync(options);
        }

        /// <summary>
        /// 创建资源包下载器
        /// </summary>
        /// <param name="retryCount">下载失败后的重试次数</param>
        /// <returns>返回文件下载操作对象</returns>
        public FSDownloadFileOperation CreateBundleDownloader(int retryCount)
        {
            var options = new FSDownloadFileOptions(Bundle, retryCount, _importFilePath);
            return _fileSystem.DownloadFileAsync(options);
        }

        /// <summary>
        /// 是否需要从远端下载
        /// </summary>
        /// <returns>如果需要下载返回true，否则返回false</returns>
        public bool IsNeedDownloadFromRemote()
        {
            return _fileSystem.NeedDownload(Bundle);
        }

        /// <summary>
        /// 获取下载器合并识别码
        /// </summary>
        /// <returns>返回用于合并下载器的唯一标识符</returns>
        public string GetDownloadCombineGUID()
        {
            return $"{_fileSystem.GetHashCode()}_{Bundle.BundleGUID}";
        }
    }
}