using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 请求内置包裹哈希操作
    /// </summary>
    internal class RequestBuiltinPackageHashOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            TryLoadPackageHash,
            RequestPackageHash,
            CheckResult,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private readonly string _packageVersion;
        private IDownloadTextRequest _downloadTextRequest;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹哈希值
        /// </summary>
        public string PackageHash { get; private set; }


        internal RequestBuiltinPackageHashOperation(BuiltinFileSystem fileSystem, string packageVersion)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.TryLoadPackageHash;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.TryLoadPackageHash)
            {
                string filePath = _fileSystem.GetBuiltinPackageHashFilePath(_packageVersion);
                if (File.Exists(filePath))
                {
                    PackageHash = File.ReadAllText(filePath);
                    _steps = ESteps.CheckResult;
                }
                else
                {
                    _steps = ESteps.RequestPackageHash;
                }
            }

            if (_steps == ESteps.RequestPackageHash)
            {
                if (_downloadTextRequest == null)
                {
                    string filePath = _fileSystem.GetBuiltinPackageHashFilePath(_packageVersion);
                    string url = DownloadSystemTools.ToLocalUrl(filePath);
                    var args = new DownloadDataRequestArgs(url, 60, 0);
                    _downloadTextRequest = _fileSystem.DownloadBackend.CreateTextRequest(args);
                    _downloadTextRequest.SendRequest();
                }

                if (_downloadTextRequest.IsDone == false)
                    return;

                if (_downloadTextRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    PackageHash = _downloadTextRequest.Result;
                    _steps = ESteps.CheckResult;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _downloadTextRequest.Error;
                }
            }

            if (_steps == ESteps.CheckResult)
            {
                if (TextUtility.ValidateContent(PackageHash, out string validateError) == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Builtin package hash file validate failed: {validateError}";
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }
        }
        internal override void InternalDispose()
        {
            if (_downloadTextRequest != null)
            {
                _downloadTextRequest.Dispose();
                _downloadTextRequest = null;
            }
        }
    }
}