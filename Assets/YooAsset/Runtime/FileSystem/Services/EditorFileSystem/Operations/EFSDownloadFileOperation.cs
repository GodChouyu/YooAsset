
namespace YooAsset
{
    /// <summary>
    /// 编辑器文件系统的下载文件操作
    /// </summary>
    internal class EFSDownloadFileOperation : FSDownloadFileOperation
    {
        protected enum ESteps
        {
            None,
            CheckExists,
            CreateDownload,
            CheckDownload,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private readonly FSDownloadFileOptions _options;
        private DownloadFileBaseOperation _downloadFileOp;
        private ESteps _steps = ESteps.None;

        internal EFSDownloadFileOperation(EditorFileSystem fileSystem, FSDownloadFileOptions options) : base(options.Bundle)
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
                if (_fileSystem.FileCache.IsCached(_options.Bundle.BundleGUID))
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
                    string editorFilePath = EditorFileSystemTools.GetEditorFilePath(Bundle);
                    _downloadFileOp = new SimulateAndCacheFileOperation(_fileSystem, Bundle, editorFilePath);
                    _fileSystem.DownloadScheduler.AddDownloadFile(_downloadFileOp);
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