using System.IO;

namespace YooAsset
{
    internal sealed class DownloadAndCacheFileOperation : DownloadFileBaseOperation
    {
        private enum ESteps
        {
            None,
            CreateRequest,
            CheckRequest,
            CacheFile,
            Done,
        }

        private readonly CacheFileSystem _fileSystem;
        private readonly string _tempFilePath;
        private bool _enableResume = false;
        private long _fileOriginLength = 0;
        private IDownloadRequest _downloadRequest;
        private FCWriteCacheOperation _writeCacheOp;
        private ESteps _steps = ESteps.None;

        internal DownloadAndCacheFileOperation(CacheFileSystem fileSystem, PackageBundle bundle, string url) : base(bundle, url)
        {
            _fileSystem = fileSystem;
            _tempFilePath = _fileSystem.GetTempFilePath(bundle);
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CreateRequest;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            // 创建下载请求
            if (_steps == ESteps.CreateRequest)
            {
                FileUtility.EnsureFileDirectory(_tempFilePath);

                _enableResume = Bundle.FileSize >= _fileSystem.ResumeDownloadMinimumSize;
                if (_enableResume)
                {
                    _downloadRequest = CreateResumeRequest();
                    _downloadRequest.SendRequest();
                    _steps = ESteps.CheckRequest;
                }
                else
                {
                    _downloadRequest = CreateNormalRequest();
                    _downloadRequest.SendRequest();
                    _steps = ESteps.CheckRequest;
                }
            }

            // 检测下载结果
            if (_steps == ESteps.CheckRequest)
            {
                DownloadProgress = _downloadRequest.DownloadProgress;
                DownloadedBytes = _fileOriginLength + _downloadRequest.DownloadedBytes;
                Progress = DownloadProgress;
                if (_downloadRequest.IsDone == false)
                    return;

                // 检查网络错误
                if (_downloadRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    _steps = ESteps.CacheFile;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _downloadRequest.Error;
                }

                // 在遇到特殊错误的时候删除文件
                if (_enableResume)
                    ClearTempFileWhenError(_downloadRequest.HttpCode);

                // 最终释放请求器
                _downloadRequest.Dispose();
            }

            // 缓存文件
            if (_steps == ESteps.CacheFile)
            {
                if (_writeCacheOp == null)
                {
                    var options = new WriteCacheOptions();
                    options.Bundle = Bundle;
                    options.FilePath = _tempFilePath;
                    _writeCacheOp = _fileSystem.FileCache.WriteCacheAsync(options);
                    _writeCacheOp.StartOperation();
                    AddChildOperation(_writeCacheOp);
                }

                _writeCacheOp.UpdateOperation();
                if (_writeCacheOp.IsDone == false)
                    return;

                if (_writeCacheOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _writeCacheOp.Error;
                }

                // 注意：缓存完成后直接删除临时文件
                if (File.Exists(_tempFilePath))
                    File.Delete(_tempFilePath);
            }
        }
        internal override void InternalAbort()
        {
            if (_downloadRequest != null)
                _downloadRequest.Dispose();
        }
        internal override void InternalWaitForCompletion()
        {
            if (_steps != ESteps.Done)
            {
                // 注意：不中断下载任务，保持后台继续下载
                YooLogger.Error($"Try load bundle {Bundle.BundleName} from remote : {Url}");
            }
        }

        private IDownloadRequest CreateResumeRequest()
        {
            // 获取下载起始位置
            if (File.Exists(_tempFilePath))
            {
                FileInfo fileInfo = new FileInfo(_tempFilePath);
                if (fileInfo.Length >= Bundle.FileSize)
                {
                    File.Delete(_tempFilePath);
                }
                else
                {
                    _fileOriginLength = fileInfo.Length;
                }
            }

            int watchdogTime = _fileSystem.DownloadWatchDogTimeout;
            int timeout = 0; //注意：文件下载不做超时检测
            bool appendToFile = true;
            bool removeFileOnAbort = false;
            long resumeOffset = _fileOriginLength;
            var args = new DownloadFileRequestArgs(Url, _tempFilePath, timeout, watchdogTime, appendToFile, removeFileOnAbort, resumeOffset);
            return _fileSystem.DownloadBackend.CreateFileRequest(args);
        }
        private IDownloadRequest CreateNormalRequest()
        {
            // 删除历史缓存文件
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);

            int watchdogTime = _fileSystem.DownloadWatchDogTimeout;
            int timeout = 0; //注意：文件下载不做超时检测
            var args = new DownloadFileRequestArgs(Url, _tempFilePath, timeout, watchdogTime);
            return _fileSystem.DownloadBackend.CreateFileRequest(args);
        }
        private void ClearTempFileWhenError(long httpCode)
        {
            if (_fileSystem.ResumeDownloadResponseCodes == null)
                return;

            //说明：如果遇到以下错误返回码，验证失败直接删除文件
            if (_fileSystem.ResumeDownloadResponseCodes.Contains(httpCode))
            {
                if (File.Exists(_tempFilePath))
                    File.Delete(_tempFilePath);
            }
        }
    }
}