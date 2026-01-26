using System.IO;

namespace YooAsset
{
    internal sealed class DownloadAndCacheRemoteFileOperation : DownloadAndCacheFileOperation
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
        private readonly PackageBundle _bundle;
        private readonly string _tempFilePath;
        private bool _enableResume = false;
        private long _fileOriginLength = 0;
        private IDownloadRequest _request;
        private FCStoreCacheOperation _bundleCacheOp;
        private ESteps _steps = ESteps.None;

        internal DownloadAndCacheRemoteFileOperation(CacheFileSystem fileSystem, PackageBundle bundle, string url) : base(url)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
            _tempFilePath = _fileSystem.GetTempFilePath(_bundle);
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
                FileUtility.CreateFileDirectory(_tempFilePath);

                _enableResume = _bundle.FileSize >= _fileSystem.ResumeDownloadMinimumSize;
                if (_enableResume)
                {
                    _request = CreateResumeRequest();
                    _request.SendRequest();
                    _steps = ESteps.CheckRequest;
                }
                else
                {
                    _request = CreateNormalRequest();
                    _request.SendRequest();
                    _steps = ESteps.CheckRequest;
                }
            }

            // 检测下载结果
            if (_steps == ESteps.CheckRequest)
            {
                DownloadProgress = _request.DownloadProgress;
                DownloadedBytes = _fileOriginLength + _request.DownloadedBytes;
                Progress = DownloadProgress;
                if (_request.IsDone == false)
                    return;

                // 检查网络错误
                if (_request.Status == EDownloadRequestStatus.Succeed)
                {
                    _steps = ESteps.CacheFile;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _request.Error;
                }

                // 在遇到特殊错误的时候删除文件
                if (_enableResume)
                    ClearTempFileWhenError(_request.HttpCode);

                // 最终释放请求器
                _request.Dispose();
            }

            // 缓存文件
            if (_steps == ESteps.CacheFile)
            {
                if (_bundleCacheOp == null)
                {
                    var options = new FCStoreCacheOptions();
                    options.Bundle = _bundle;
                    options.FilePath = _tempFilePath;
                    _bundleCacheOp = _fileSystem.Cache.StoreCacheAsync(options);
                    _bundleCacheOp.StartOperation();
                    AddChildOperation(_bundleCacheOp);
                }

                _bundleCacheOp.UpdateOperation();
                if (_bundleCacheOp.IsDone == false)
                    return;

                if (_bundleCacheOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _bundleCacheOp.Error;
                }

                // 注意：缓存完成后直接删除临时文件
                if (File.Exists(_tempFilePath))
                    File.Delete(_tempFilePath);
            }
        }
        internal override void InternalAbort()
        {
            if (_request != null)
                _request.Dispose();
        }
        internal override void InternalWaitForCompletion()
        {
            if (_steps != ESteps.Done)
            {
                // 注意：不中断下载任务，保持后台继续下载
                YooLogger.Error($"Try load bundle {_bundle.BundleName} from remote : {URL}");
            }
        }

        private IDownloadRequest CreateResumeRequest()
        {
            // 获取下载起始位置
            if (File.Exists(_tempFilePath))
            {
                FileInfo fileInfo = new FileInfo(_tempFilePath);
                if (fileInfo.Length >= _bundle.FileSize)
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
            var args = new DownloadFileRequestArgs(URL, _tempFilePath, timeout, watchdogTime, appendToFile, removeFileOnAbort, resumeOffset);
            return _fileSystem.DownloadBackend.CreateFileRequest(args);
        }
        private IDownloadRequest CreateNormalRequest()
        {
            // 删除历史缓存文件
            if (File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);

            int watchdogTime = _fileSystem.DownloadWatchDogTimeout;
            int timeout = 0; //注意：文件下载不做超时检测
            var args = new DownloadFileRequestArgs(URL, _tempFilePath, timeout, watchdogTime);
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