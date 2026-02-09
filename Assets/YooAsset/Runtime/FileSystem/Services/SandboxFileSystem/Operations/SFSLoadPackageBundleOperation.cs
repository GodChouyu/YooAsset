
namespace YooAsset
{
    /// <summary>
    /// 沙盒文件系统的加载资源包操作
    /// </summary>
    internal class SFSLoadPackageBundleOperation : FSLoadPackageBundleOperation
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

        private readonly SandboxFileSystem _fileSystem;
        private readonly FSLoadPackageBundleOptions _options;
        private FSDownloadFileOperation _downloadFileOp;
        private FCLoadBundleOperation _loadBundleOp;
        private ESteps _steps = ESteps.None;

        internal SFSLoadPackageBundleOperation(SandboxFileSystem fileSystem, FSLoadPackageBundleOptions options)
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
                    _steps = ESteps.LoadBundle;
                }
                else
                {
                    if (_fileSystem.DisableOnDemandDownload)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"The bundle is not cached: {_options.Bundle.BundleName}";
                        YooLogger.Warning(Error);
                    }
                    else
                    {
                        _steps = ESteps.DownloadFile;
                    }
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
                    // 注意：网络原因失败后，重新尝试直到成功
                    var options = new FSDownloadFileOptions(_options.Bundle, int.MaxValue);
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
                        Error = "Fatal error: loaded bundle handle is null.";
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