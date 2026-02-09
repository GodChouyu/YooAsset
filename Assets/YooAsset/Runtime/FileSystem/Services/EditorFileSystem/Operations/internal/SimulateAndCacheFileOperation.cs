
namespace YooAsset
{
    /// <summary>
    /// 模拟下载并缓存文件操作
    /// </summary>
    internal class SimulateAndCacheFileOperation : DownloadFileBaseOperation
    {
        protected enum ESteps
        {
            None,
            CreateRequest,
            CheckRequest,
            CacheFile,
            Done,
        }

        protected readonly EditorFileSystem _fileSystem;
        protected IDownloadRequest _downloadRequest;
        private FCWriteCacheOperation _writeCacheOp;
        private ESteps _steps = ESteps.None;

        internal SimulateAndCacheFileOperation(EditorFileSystem fileSystem, PackageBundle bundle, string filePath) : base(bundle, filePath)
        {
            _fileSystem = fileSystem;
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
                int speed = _fileSystem.VirtualDownloadSpeed;
                var args = new SimulateDownloadRequestArgs(Url, Bundle.FileSize, speed);
                _downloadRequest = _fileSystem.DownloadBackend.CreateSimulateRequest(args);
                _downloadRequest.SendRequest();
                _steps = ESteps.CheckRequest;
            }

            // 检测下载结果
            if (_steps == ESteps.CheckRequest)
            {
                Report.DownloadedBytes = _downloadRequest.DownloadedBytes;
                Report.DownloadProgress = _downloadRequest.DownloadProgress;
                Progress = _downloadRequest.DownloadProgress;
                if (_downloadRequest.IsDone == false)
                    return;

                // 更新下载报告
                Report.DownloadedBytes = _downloadRequest.DownloadedBytes;
                Report.DownloadProgress = _downloadRequest.DownloadProgress;
                Report.HttpCode = _downloadRequest.HttpCode;
                Report.HttpError = _downloadRequest.HttpError;

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
            }

            // 缓存文件
            if (_steps == ESteps.CacheFile)
            {
                if (_writeCacheOp == null)
                {
                    var options = new FCWriteCacheOptions();
                    options.Bundle = Bundle;
                    options.FilePath = Url;
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
            }
        }
        internal override void InternalDispose()
        {
            if (_downloadRequest != null)
            {
                _downloadRequest.Dispose();
                _downloadRequest = null;
            }
        }
        internal override void InternalWaitForCompletion()
        {
            if (_steps != ESteps.Done)
            {
                // 注意：不中断下载任务，保持下载后台继续下载
                // 注意：上层异步操作会被动失败
                YooLogger.Error($"Attempting to load bundle {Bundle.BundleName} from remote: {Url}");
            }
        }
    }
}