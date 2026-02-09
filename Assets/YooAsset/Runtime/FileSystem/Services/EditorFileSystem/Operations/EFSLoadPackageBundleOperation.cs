
namespace YooAsset
{
    /// <summary>
    /// 编辑器文件系统的加载资源包操作
    /// </summary>
    internal class EFSLoadPackageBundleOperation : FSLoadPackageBundleOperation
    {
        private enum ESteps
        {
            None,
            Prepare,
            DownloadFile,
            AbortDownload,
            LoadBundle,
            CheckResult,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private readonly FSLoadPackageBundleOptions _options;
        private FSDownloadFileOperation _downloadFileOp;
        private FCLoadBundleOperation _loadBundleOp;
        private ESteps _steps = ESteps.None;

        internal EFSLoadPackageBundleOperation(EditorFileSystem fileSystem, FSLoadPackageBundleOptions options)
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
                    _steps = ESteps.LoadBundle;
                else
                    _steps = ESteps.DownloadFile;
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
                    // 注意：模拟文件下载不做失败尝试
                    var options = new FSDownloadFileOptions(_options.Bundle, 0);
                    _downloadFileOp = _fileSystem.DownloadFileAsync(options);
                    _downloadFileOp.StartOperation();
                    AddChildOperation(_downloadFileOp);
                }

                if (IsWaitForCompletion)
                    _downloadFileOp.WaitForCompletion();

                _downloadFileOp.UpdateOperation();
                if (_downloadFileOp.IsDone == false)
                    return;

                if (_downloadFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.LoadBundle;
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
                Error = "File download aborted.";
            }

            if (_steps == ESteps.LoadBundle)
            {
                _loadBundleOp = _fileSystem.FileCache.LoadBundleAsync(_options.ConvertTo());
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
                    if (_loadBundleOp.BundleHandle == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "Loaded bundle handle is null.";
                        YooLogger.Error(Error);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        BundleHandle = _loadBundleOp.BundleHandle;
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