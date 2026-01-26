using System.IO;

namespace YooAsset
{
    internal sealed class DownloadAndCacheLocalFileOperation : DownloadAndCacheFileOperation
    {
        private enum ESteps
        {
            None,
            CheckCopy,
            CopyLocalFile,
            CreateRequest,
            CheckRequest,
            CacheFile,
            Done,
        }

        private readonly CacheFileSystem _fileSystem;
        private readonly PackageBundle _bundle;
        private readonly string _tempFilePath;
        private IDownloadRequest _request;
        private FCStoreCacheOperation _bundleCacheOp;
        private ESteps _steps = ESteps.None;

        internal DownloadAndCacheLocalFileOperation(CacheFileSystem fileSystem, PackageBundle bundle, string url) : base(url)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
            _tempFilePath = _fileSystem.GetTempFilePath(_bundle);
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckCopy;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            // 检测文件拷贝
            if (_steps == ESteps.CheckCopy)
            {
                // 删除历史缓存文件
                FileUtility.CreateFileDirectory(_tempFilePath);
                if (File.Exists(_tempFilePath))
                    File.Delete(_tempFilePath);

                if (_fileSystem.CopyLocalFileServices != null)
                    _steps = ESteps.CopyLocalFile;
                else
                    _steps = ESteps.CreateRequest;
            }

            // 拷贝本地文件
            if (_steps == ESteps.CopyLocalFile)
            {
                try
                {
                    //TODO 团结引擎，在某些机型（红米），拷贝包内文件会小概率失败！需要借助其它方式来拷贝包内文件。
                    var localFileInfo = new LocalFileInfo();
                    localFileInfo.PackageName = _fileSystem.PackageName;
                    localFileInfo.BundleName = _bundle.BundleName;
                    localFileInfo.SourceFileURL = URL;
                    _fileSystem.CopyLocalFileServices.CopyFile(localFileInfo, _tempFilePath);
                    if (File.Exists(_tempFilePath))
                    {
                        DownloadProgress = 1f;
                        DownloadedBytes = _bundle.FileSize;
                        _steps = ESteps.CacheFile;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Failed copy local file : {URL}";
                    }
                }
                catch (System.Exception ex)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed copy local file : {ex.Message}";
                }
            }

            // 创建下载请求
            if (_steps == ESteps.CreateRequest)
            {
                int watchdogTime = _fileSystem.DownloadWatchDogTimeout;
                int timeout = 0; //注意：文件下载不做超时检测
                var args = new DownloadFileRequestArgs(URL, _tempFilePath, timeout, watchdogTime);
                _request = _fileSystem.DownloadBackend.CreateFileRequest(args);
                _request.SendRequest();
                _steps = ESteps.CheckRequest;
            }

            // 检测下载结果
            if (_steps == ESteps.CheckRequest)
            {
                //TODO 更新下载后台，防止无限挂起
                if (IsWaitForCompletion)
                    _fileSystem.DownloadBackend.Update();

                DownloadProgress = _request.DownloadProgress;
                DownloadedBytes = _request.DownloadedBytes;
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

                if (IsWaitForCompletion)
                    _bundleCacheOp.WaitForCompletion();

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
            //TODO 等待导入或解压本地文件完毕，该操作会挂起主线程！
            ExecuteUntilComplete();
        }
    }
}