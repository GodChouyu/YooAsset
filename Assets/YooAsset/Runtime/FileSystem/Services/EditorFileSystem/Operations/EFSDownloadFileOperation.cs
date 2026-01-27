using UnityEngine;

namespace YooAsset
{
    internal class EFSDownloadFileOperation : FSDownloadFileOperation
    {
        protected enum ESteps
        {
            None,
            CheckExists,
            DownloadAndCache,
            TryAgain,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private readonly DownloadFileOptions _options;
        private DownloadFileBaseOperation _downloadFileOp;
        private ESteps _steps = ESteps.None;

        // 失败重试
        private float _tryAgainTimer = 0;
        private int _failedTryAgain;

        internal EFSDownloadFileOperation(EditorFileSystem fileSystem, DownloadFileOptions options) : base(options.Bundle)
        {
            _fileSystem = fileSystem;
            _options = options;
            _failedTryAgain = options.RetryCount;
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
                if (_fileSystem.FileCache.IsCached(_options.Bundle.BundleGUID))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.DownloadAndCache;
                }
            }

            // 下载并缓存文件
            if (_steps == ESteps.DownloadAndCache)
            {
                if (_downloadFileOp == null)
                {
                    _downloadFileOp = _fileSystem.DownloadScheduler.TryGetDownloadFile(Bundle);
                    if (_downloadFileOp == null)
                    {
                        string editorFilePath = _fileSystem.GetBundleFilePath(Bundle);
                        _downloadFileOp = new SimulateAndCacheFileOperation(_fileSystem, Bundle, editorFilePath);
                        _fileSystem.DownloadScheduler.AddDownloadFile(_downloadFileOp);
                    }
                }

                if (IsWaitForCompletion)
                    _downloadFileOp.WaitForCompletion();

                _downloadFileOp.UpdateOperation();
                Progress = _downloadFileOp.Progress;
                DownloadedBytes = _downloadFileOp.DownloadedBytes;
                DownloadProgress = _downloadFileOp.DownloadProgress;
                if (_downloadFileOp.IsDone == false)
                    return;

                if (_downloadFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    if (IsWaitForCompletion == false && _failedTryAgain > 0)
                    {
                        _steps = ESteps.TryAgain;
                        YooLogger.Warning($"Failed download : {_downloadFileOp.Url} Try again.");
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
                _tryAgainTimer += Time.unscaledDeltaTime;
                if (_tryAgainTimer > 1f)
                {
                    _tryAgainTimer = 0f;
                    _failedTryAgain--;
                    Progress = 0f;
                    DownloadProgress = 0f;
                    DownloadedBytes = 0;
                    _steps = ESteps.DownloadAndCache;
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
    }
}