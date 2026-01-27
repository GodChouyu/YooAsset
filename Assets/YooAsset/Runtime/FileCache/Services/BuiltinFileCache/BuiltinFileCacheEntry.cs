
namespace YooAsset
{
    internal class BuiltinFileCacheEntry : ICacheEntry
    {
        public string BundleGUID { get; private set; }
        public string FilePath { get; private set; }

        public BuiltinFileCacheEntry(string bundleGUID, string filePath)
        {
            BundleGUID = bundleGUID;
            FilePath = filePath;
        }
    }
}