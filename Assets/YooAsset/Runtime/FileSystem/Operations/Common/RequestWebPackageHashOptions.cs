namespace YooAsset
{
    internal struct RequestWebPackageHashOptions
    {
        public string PackageName { get; set; }
        public string PackageVersion { get; set; }
        public int Timeout { get; set; }

        public IRemoteServices RemoteServices { get; set; }
        public IDownloadBackend DownloadBackend { get; set; }
    }
}