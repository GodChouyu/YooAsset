using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 下载包裹哈希文件操作
    /// </summary>
    internal class DownloadPackageHashOperation : AsyncOperationBase
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


        internal DownloadPackageHashOperation(SandboxFileSystem fileSystem, string packageVersion, int timeout)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
            _timeout = timeout;
        }
        internal override void InternalStart()
        {
            _savePath = _fileSystem.GetCachePackageHashFilePath(_packageVersion);
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

                    string fileName = YooAssetSettingsData.GetPackageHashFileName(_fileSystem.PackageName, _packageVersion);
                    string webURL = GetWebRequestURL(fileName);
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
                // 严格校验临时文件内容
                string content = FileUtility.ReadAllText(_tempPath);
                if (TextUtility.ValidateContent(content, out string validateError) == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Downloaded package hash file validate failed: {validateError}";
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
                    Error = $"Failed to move hash temp file to cache path: {ex.Message}";
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
        private string GetWebRequestURL(string fileName)
        {
            var urls = _fileSystem.RemoteServices.GetRemoteURLs(fileName);
            return _fileSystem.DownloadURLPolicy.SelectURL(urls);
        }
    }
}