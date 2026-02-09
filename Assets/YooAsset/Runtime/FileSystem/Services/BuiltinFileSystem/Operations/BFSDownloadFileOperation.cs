
namespace YooAsset
{
    /// <summary>
    /// 内置文件系统的解压文件操作
    /// </summary>
    internal class BFSDownloadFileOperation : FSDownloadFileOperation
    {
        private enum ESteps
        {
            None,
            CheckExists,
            CreateUnpack,
            CheckUnpack,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private readonly FSDownloadFileOptions _options;
        private DownloadFileBaseOperation _downloadFileOp;
        private ESteps _steps = ESteps.None;

        internal BFSDownloadFileOperation(BuiltinFileSystem fileSystem, FSDownloadFileOptions options) : base(options.Bundle)
        {
            _fileSystem = fileSystem;
            _options = options;
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
                if (_fileSystem.UnpackFileCache.IsCached(Bundle.BundleGUID))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.CreateUnpack;
                }
            }

            // 创建解压器
            if (_steps == ESteps.CreateUnpack)
            {
                _downloadFileOp = _fileSystem.UnpackScheduler.TryGetDownloadFile(Bundle);
                if (_downloadFileOp == null)
                {
                    string builtinFilePath = _fileSystem.GetBuiltinBundleFilePath(Bundle);
                    _downloadFileOp = new UnpackAndCacheFileOperation(_fileSystem, Bundle, builtinFilePath);
                    _fileSystem.UnpackScheduler.AddDownloadFile(_downloadFileOp);
                }

                _steps = ESteps.CheckUnpack;
            }

            // 检测结果
            if (_steps == ESteps.CheckUnpack)
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
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
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
