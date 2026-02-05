
namespace YooAsset
{
    internal class EFSLoadBundleOperation : FSLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            Prepare,
            DownloadFile,
            AbortDownload,
            LoadVirtualBundle,
            CheckResult,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private readonly FCLoadBundleOptions _options;
        private FSDownloadFileOperation _downloadFileOp;
        private FCLoadBundleOperation _loadBundleOp;
        private ESteps _steps = ESteps.None;

        internal EFSLoadBundleOperation(EditorFileSystem fileSystem, FCLoadBundleOptions options)
        {
            _fileSystem = fileSystem;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.Prepare;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.Prepare)
            {
                if (_fileSystem.FileCache.IsCached(_options.Bundle.BundleGUID))
                {
                    DownloadProgress = 1f;
                    DownloadedBytes = _options.Bundle.FileSize;
                    _steps = ESteps.LoadVirtualBundle;
                }
                else
                {
                    _steps = ESteps.DownloadFile;
                }
            }

            if (_steps == ESteps.DownloadFile)
            {
                // 中断下载
                if (AbortDownloadFile)
                {
                    if (_downloadFileOp != null)
                        _downloadFileOp.AbortOperation();
                    _steps = ESteps.AbortDownload;
                }
            }

            if (_steps == ESteps.DownloadFile)
            {
                if (_downloadFileOp == null)
                {
                    FSDownloadFileOptions options = new FSDownloadFileOptions(_options.Bundle, int.MaxValue);
                    _downloadFileOp = _fileSystem.DownloadFileAsync(options);
                    _downloadFileOp.StartOperation();
                    AddChildOperation(_downloadFileOp);
                }

                if (IsWaitForCompletion)
                    _downloadFileOp.WaitForCompletion();

                _downloadFileOp.UpdateOperation();
                DownloadProgress = _downloadFileOp.DownloadProgress;
                DownloadedBytes = _downloadFileOp.DownloadedBytes;
                if (_downloadFileOp.IsDone == false)
                    return;

                if (_downloadFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.LoadVirtualBundle;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _downloadFileOp.Error;
                }
            }

            if (_steps == ESteps.AbortDownload)
            {
                if (_downloadFileOp != null)
                {
                    if (IsWaitForCompletion)
                        _downloadFileOp.WaitForCompletion();

                    _downloadFileOp.UpdateOperation();
                    if (_downloadFileOp.IsDone == false)
                        return;
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Abort download file.";
            }

            if (_steps == ESteps.LoadVirtualBundle)
            {
                _loadBundleOp = _fileSystem.FileCache.LoadBundleAsync(_options);
                _loadBundleOp.StartOperation();
                AddChildOperation(_loadBundleOp);
                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (IsWaitForCompletion)
                    _loadBundleOp.WaitForCompletion();

                _loadBundleOp.UpdateOperation();
                if (_loadBundleOp.IsDone == false)
                    return;

                if (_loadBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadBundleOp.BundleResult == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "Loaded bundle result is null.";
                        YooLogger.Error(Error);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        Result = _loadBundleOp.BundleResult;
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadBundleOp.Error;
                    YooLogger.Error(Error);
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}