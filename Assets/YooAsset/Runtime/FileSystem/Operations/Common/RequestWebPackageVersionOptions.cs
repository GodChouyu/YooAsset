namespace YooAsset
{
    internal struct RequestWebPackageVersionOptions
    {
        public string PackageName { get; set; }
        public bool AppendTimeTicks { get; set; }
        public int Timeout { get; set; }

        public IRemoteServices RemoteServices { get; set; }
        public IDownloadBackend DownloadBackend { get; set; }
    }
}
