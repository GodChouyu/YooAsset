using System.IO;

namespace YooAsset
{
    internal sealed class UnpackAndCacheFileOperation : DownloadFileBaseOperation
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

        private readonly BuiltinFileSystem _fileSystem;
        private readonly string _builtinFilePath;
        private readonly string _tempFilePath;
        private CopyBuiltinFileOperation _copyBuiltinFileOp;
        private FCWriteCacheOperation _bundleCacheOp;
        private ESteps _steps = ESteps.None;

        internal UnpackAndCacheFileOperation(BuiltinFileSystem fileSystem, PackageBundle bundle, string builtinFilePath) : base(bundle, builtinFilePath)
        {
            _fileSystem = fileSystem;
            _builtinFilePath = builtinFilePath;
            _tempFilePath = _fileSystem.GetUnpackTempFilePath(bundle);
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
                FileUtility.EnsureFileDirectory(_tempFilePath);
                if (File.Exists(_tempFilePath))
                    File.Delete(_tempFilePath);

                _steps = ESteps.CopyLocalFile;
            }

            // 拷贝本地文件
            if (_steps == ESteps.CopyLocalFile)
            {
                if (_copyBuiltinFileOp == null)
                {
                    _copyBuiltinFileOp = new CopyBuiltinFileOperation(_fileSystem, _builtinFilePath, _tempFilePath);
                    _copyBuiltinFileOp.StartOperation();
                    AddChildOperation(_copyBuiltinFileOp);
                }

                if (IsWaitForCompletion)
                    _copyBuiltinFileOp.WaitForCompletion();

                _copyBuiltinFileOp.UpdateOperation();
                if (_copyBuiltinFileOp.IsDone == false)
                    return;

                if (_copyBuiltinFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.CacheFile;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _copyBuiltinFileOp.Error;
                }
            }

            // 缓存文件
            if (_steps == ESteps.CacheFile)
            {
                if (_bundleCacheOp == null)
                {
                    var options = new WriteCacheOptions();
                    options.Bundle = Bundle;
                    options.FilePath = _tempFilePath;
                    _bundleCacheOp = _fileSystem.UnpackFileCache.WriteCacheAsync(options);
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