
namespace YooAsset
{
    internal class BundleInfo
    {
        private readonly IFileSystem _fileSystem;
        private readonly string _importFilePath;

        /// <summary>
        /// 资源包对象
        /// </summary>
        public readonly PackageBundle Bundle;


        public BundleInfo(IFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            Bundle = bundle;
            _importFilePath = null;
        }
        public BundleInfo(IFileSystem fileSystem, PackageBundle bundle, string importFilePath)
        {
            _fileSystem = fileSystem;
            Bundle = bundle;
            _importFilePath = importFilePath;
        }

        /// <summary>
        /// 创建加载器
        /// </summary>
        public FSLoadBundleOperation CreateBundleLoader()
        {
            var options = new LoadBundleOptions(Bundle);
            return _fileSystem.LoadBundleAsync(options);
        }

        /// <summary>
        /// 创建下载器
        /// </summary>
        public FSDownloadFileOperation CreateBundleDownloader(int failedTryAgain)
        {
            DownloadFileOptions options = new DownloadFileOptions(Bundle, failedTryAgain);
            options.ImportFilePath = _importFilePath;
            return _fileSystem.DownloadFileAsync(options);
        }

        /// <summary>
        /// 是否需要从远端下载
        /// </summary>
        public bool IsNeedDownloadFromRemote()
        {
            return _fileSystem.NeedDownload(Bundle);
        }

        /// <summary>
        /// 下载器合并识别码
        /// </summary>
        public string GetDownloadCombineGUID()
        {
            return $"{_fileSystem.GetHashCode()}_{Bundle.BundleGUID}";
        }
    }
}