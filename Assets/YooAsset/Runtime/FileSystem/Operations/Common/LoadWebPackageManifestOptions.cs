namespace YooAsset
{
    internal struct LoadWebPackageManifestOptions
    {
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public string PackageHash { get; set; }
        public int Timeout { get; set; }

        public IRemoteServices RemoteServices { get; set; }
        public IManifestDecryptor ManifestDecryptor { get; set; }
        public IDownloadBackend DownloadBackend { get; set; }
        public IDownloadURLPolicy URLPolicy { get; set; }
    }
}
