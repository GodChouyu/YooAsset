using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 沙盒文件系统的下载文件操作
    /// </summary>
    internal class SFSDownloadFileOperation : FSDownloadFileOperation
    {
        protected enum ESteps
        {
            None,
            CheckExists,
            CreateDownload,
            CheckDownload,
            TryAgain,
            Done,
        }

        private readonly SandboxFileSystem _fileSystem;
        private readonly FSDownloadFileOptions _options;
        private readonly DownloadRetry _downloadRetry;
        private DownloadFileBaseOperation _downloadFileOp;
        private ESteps _steps = ESteps.None;

        internal SFSDownloadFileOperation(SandboxFileSystem fileSystem, FSDownloadFileOptions options) : base(options.Bundle)
        {
            _fileSystem = fileSystem;
            _options = options;
            _downloadRetry = new DownloadRetry(options.RetryCount, _fileSystem.DownloadRetryPolicy);
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckExists;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            // 检测文件是否存在
            if (_steps == ESteps.CheckExists)
            {
                if (_fileSystem.FileCache.IsCached(Bundle.BundleGUID))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.CreateDownload;
                }
            }

            // 创建下载器
            if (_steps == ESteps.CreateDownload)
            {
                _downloadFileOp = _fileSystem.DownloadScheduler.TryGetDownloadFile(Bundle);
                if (_downloadFileOp == null)
                {
                    if (string.IsNullOrEmpty(_options.ImportFilePath))
                    {
                        // 下载远端文件
                        string url = GetRequestURL(Bundle.FileName);
                        _downloadFileOp = new DownloadAndCacheFileOperation(_fileSystem, Bundle, url);
                        _fileSystem.DownloadScheduler.AddDownloadFile(_downloadFileOp);
                    }
                    else
                    {
                        // 导入本地文件
                        _downloadFileOp = new ImportAndCacheFileOperation(_fileSystem, Bundle, _options.ImportFilePath);
                        _fileSystem.DownloadScheduler.AddDownloadFile(_downloadFileOp);
                    }
                }

                _steps = ESteps.CheckDownload;
            }

            // 检测结果
            if (_steps == ESteps.CheckDownload)
            {
                if (IsWaitForCompletion)
                    _downloadFileOp.WaitForCompletion();

                _downloadFileOp.UpdateOperation();
                Progress = _downloadFileOp.Progress;
                Report = _downloadFileOp.Report;
                if (_downloadFileOp.IsDone == false)
                    return;

                if (_downloadFileOp.Status == EOperationStatus.Succeeded)
                {
                    _fileSystem.DownloadURLPolicy.OnSuccess(_downloadFileOp.Url);
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    string url = _downloadFileOp.Url;
                    long httpCode = _downloadFileOp.Report.HttpCode;
                    string httpError = _downloadFileOp.Report.HttpError;
                    _fileSystem.DownloadURLPolicy.OnFailure(url, httpCode, httpError);
                    if (IsWaitForCompletion == false && _downloadRetry.CanRetry(url, httpCode, httpError))
                    {
                        _downloadRetry.BeginWait();
                        _steps = ESteps.TryAgain;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _downloadFileOp.Error;
                        YooLogger.Error(Error);
                    }
                }
            }

            // 重新尝试下载
            if (_steps == ESteps.TryAgain)
            {
                if (_downloadRetry.Tick())
                {
                    Progress = 0f;
                    Report = DownloadReport.Default;
                    _steps = ESteps.CreateDownload;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
        internal override void InternalAbort()
        {
            // 注意：取消下载任务的时候引用计数减一
            if (_steps != ESteps.Done)
            {
                if (_downloadFileOp != null)
                {
                    _downloadFileOp.Release();
                }
            }
        }

        /// <summary>
        /// 获取网络请求地址
        /// </summary>
        private string GetRequestURL(string fileName)
        {
            var urls = _fileSystem.RemoteServices.GetRemoteURLs(fileName);
            return _fileSystem.DownloadURLPolicy.SelectURL(urls);
        }
    }
}