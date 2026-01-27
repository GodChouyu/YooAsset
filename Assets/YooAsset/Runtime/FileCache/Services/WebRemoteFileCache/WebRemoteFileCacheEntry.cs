
namespace YooAsset
{
    internal class WebRemoteFileCacheEntry : ICacheEntry
    {
        public string BundleGUID { get; private set; }
        public string MainURL { get; private set; }
        public string FallbackURL { get; private set; }

        public WebRemoteFileCacheEntry(string bundleGUID, string mainURL, string fallbackURL)
        {
            BundleGUID = bundleGUID;
            MainURL = mainURL;
            FallbackURL = fallbackURL;
        }
    }
}