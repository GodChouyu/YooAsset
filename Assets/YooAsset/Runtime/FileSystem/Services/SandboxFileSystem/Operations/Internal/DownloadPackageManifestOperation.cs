using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 下载包裹清单文件操作
    /// </summary>
    internal class DownloadPackageManifestOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            CheckExist,
            DownloadFile,
            VerifyFile,
            Done,
        }

        private readonly SandboxFileSystem _fileSystem;
        private readonly string _packageVersion;
        private readonly int _timeout;
        private IDownloadFileRequest _downloadFileRequest;
        private string _savePath;
        private string _tempPath;
        private ESteps _steps = ESteps.None;


        internal DownloadPackageManifestOperation(SandboxFileSystem fileSystem, string packageVersion, int timeout)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
            _timeout = timeout;
        }
        internal override void InternalStart()
        {
            _savePath = _fileSystem.GetCachePackageManifestFilePath(_packageVersion);
            _tempPath = _savePath + ".tmp";
            _steps = ESteps.CheckExist;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckExist)
            {
                if (File.Exists(_savePath))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.DownloadFile;
                }
            }

            if (_steps == ESteps.DownloadFile)
            {
                if (_downloadFileRequest == null)
                {
                    // 删除历史临时文件
                    if (File.Exists(_tempPath))
                        File.Delete(_tempPath);

                    string fileName = YooAssetSettingsData.GetManifestBinaryFileName(_fileSystem.PackageName, _packageVersion);
                    string webURL = GetDownloadRequestURL(fileName);
                    int watchdogTime = _fileSystem.DownloadWatchdogTimeout;
                    var args = new DownloadFileRequestArgs(webURL, _tempPath, _timeout, watchdogTime);
                    _downloadFileRequest = _fileSystem.DownloadBackend.CreateFileRequest(args);
                    _downloadFileRequest.SendRequest();
                }

                if (_downloadFileRequest.IsDone == false)
                    return;

                if (_downloadFileRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    _steps = ESteps.VerifyFile;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _downloadFileRequest.Error;
                    _fileSystem.DownloadURLPolicy.OnFailure(_downloadFileRequest.Url, _downloadFileRequest.HttpCode, _downloadFileRequest.HttpError);
                    DeleteTempFile();
                }
            }

            if (_steps == ESteps.VerifyFile)
            {
                // 验证临时文件存在且大小有效
                FileInfo fileInfo = new FileInfo(_tempPath);
                if (fileInfo.Exists == false || fileInfo.Length == 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Downloaded package manifest temp file is invalid.";
                    DeleteTempFile();
                    return;
                }

                // 原子移动到最终缓存路径
                try
                {
                    if (File.Exists(_savePath))
                        File.Delete(_savePath);
                    File.Move(_tempPath, _savePath);
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                catch (System.Exception ex)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to move manifest temp file to cache path: {ex.Message}";
                    DeleteTempFile();
                }
            }
        }
        internal override void InternalDispose()
        {
            if (_downloadFileRequest != null)
            {
                _downloadFileRequest.Dispose();
                _downloadFileRequest = null;
            }
        }

        private void DeleteTempFile()
        {
            if (File.Exists(_tempPath))
                File.Delete(_tempPath);
        }
        private string GetDownloadRequestURL(string fileName)
        {
            var urls = _fileSystem.RemoteServices.GetRemoteURLs(fileName);
            return _fileSystem.DownloadURLPolicy.SelectURL(urls);
        }
    }
}