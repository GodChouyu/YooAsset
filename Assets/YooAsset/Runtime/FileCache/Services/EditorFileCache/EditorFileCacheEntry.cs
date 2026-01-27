
namespace YooAsset
{
    internal class EditorFileCacheEntry : ICacheEntry
    {
        public string BundleGUID { get; private set; }
        public string FilePath { get; private set; }

        public EditorFileCacheEntry(string bundleGUID, string filePath)
        {
            BundleGUID = bundleGUID;
            FilePath = filePath;
        }
    }
}