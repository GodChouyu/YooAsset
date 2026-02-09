using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 导入并缓存文件操作
    /// </summary>
    internal sealed class ImportAndCacheFileOperation : DownloadFileBaseOperation
    {
        private enum ESteps
        {
            None,
            CheckTempFile,
            CopyLocalFile,
            CacheFile,
            Done,
        }

        private readonly SandboxFileSystem _fileSystem;
        private readonly string _sourceFilePath;
        private readonly string _tempFilePath;
        private FCWriteCacheOperation _bundleCacheOp;
        private ESteps _steps = ESteps.None;

        internal ImportAndCacheFileOperation(SandboxFileSystem fileSystem, PackageBundle bundle, string sourceFilePath) : base(bundle, sourceFilePath)
        {
            _fileSystem = fileSystem;
            _sourceFilePath = sourceFilePath;
            _tempFilePath = _fileSystem.GetTempFilePath(bundle);
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckTempFile;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            // 检测临时文件
            if (_steps == ESteps.CheckTempFile)
            {
                // 注意：删除历史临时文件
                FileUtility.EnsureFileDirectory(_tempFilePath);
                if (File.Exists(_tempFilePath))
                    File.Delete(_tempFilePath);

                _steps = ESteps.CopyLocalFile;
            }

            // 拷贝本地文件
            if (_steps == ESteps.CopyLocalFile)
            {
                try
                {
                    File.Copy(_sourceFilePath, _tempFilePath, true);

                    // 更新下载报告
                    Report.DownloadedBytes = Bundle.FileSize;
                    Report.DownloadProgress = 1f;
                    _steps = ESteps.CacheFile;
                }
                catch (System.Exception ex)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to copy local file: {ex.Message}";
                }
            }

            // 缓存文件
            if (_steps == ESteps.CacheFile)
            {
                if (_bundleCacheOp == null)
                {
                    var options = new FCWriteCacheOptions();
                    options.Bundle = Bundle;
                    options.FilePath = _tempFilePath;
                    _bundleCacheOp = _fileSystem.FileCache.WriteCacheAsync(options);
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
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}