
namespace YooAsset
{
    internal class WebServerFileCacheEntry : ICacheEntry
    {
        public string BundleGUID { get; private set; }
        public string FilePath { get; private set; }

        public WebServerFileCacheEntry(string bundleGUID, string filePath)
        {
            BundleGUID = bundleGUID;
            FilePath = filePath;
        }
    }
}