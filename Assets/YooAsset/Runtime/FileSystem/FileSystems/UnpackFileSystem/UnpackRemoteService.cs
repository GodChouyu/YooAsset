using System.Collections.Generic;

namespace YooAsset
{
    internal class UnpackRemoteService : IRemoteServices
    {
        private readonly string _buildinPackageRoot;
        protected readonly Dictionary<string, string> _mapping = new Dictionary<string, string>(10000);

        public UnpackRemoteService(string buildinPackRoot)
        {
            _buildinPackageRoot = buildinPackRoot;
        }
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return GetFileLoadURL(fileName);
        }
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return GetFileLoadURL(fileName);
        }

        private string GetFileLoadURL(string fileName)
        {
            if (_mapping.TryGetValue(fileName, out string url) == false)
            {
                string filePath = PathUtility.Combine(_buildinPackageRoot, fileName);
                url = DownloadSystemTools.ToLocalURL(filePath);
                _mapping.Add(fileName, url);
            }
            return url;
        }
    }
}