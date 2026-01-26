
namespace YooAsset
{
    /// <summary>
    /// 解压文件系统
    /// </summary>
    internal class UnpackFileSystem : CacheFileSystem
    {
        public UnpackFileSystem()
        {
        }
        public override void OnCreate(string packageName, string rootDirectory)
        {
            base.OnCreate(packageName, rootDirectory);

            // 注意：重写保存根目录和临时目录
            _cacheBundleFilesRoot = PathUtility.Combine(_packageRoot, UnpackFileSystemConstants.SaveBundleFilesFolderName);
            _cacheManifestFilesRoot = PathUtility.Combine(_packageRoot, UnpackFileSystemConstants.SaveManifestFilesFolderName);
            _tempFilesRoot = PathUtility.Combine(_packageRoot, UnpackFileSystemConstants.TempFilesFolderName);
        }
    }
}