using System;
using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 沙盒文件缓存写入操作
    /// </summary>
    internal class SFCWriteCacheOperation : FCWriteCacheOperation
    {
        private enum ESteps
        {
            None,
            Check,
            VerifyFile,
            CacheFile,
            Done,
        }

        private readonly SandboxFileCache _fileCache;
        private readonly FCWriteCacheOptions _options;
        private VerifyTempFileOperation _verifyTempFileOp;
        private ESteps _steps = ESteps.None;

        public SFCWriteCacheOperation(SandboxFileCache fileCache, FCWriteCacheOptions options)
        {
            _fileCache = fileCache;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.Check;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.Check)
            {
                if (_fileCache.IsCached(_options.Bundle.BundleGUID))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "The bundle is already cached.";
                }
                else
                {
                    _steps = ESteps.VerifyFile;
                }
            }

            if (_steps == ESteps.VerifyFile)
            {
                if (_verifyTempFileOp == null)
                {
                    var element = new TempFileInfo(_options.FilePath, _options.Bundle.FileCRC, _options.Bundle.FileSize);
                    _verifyTempFileOp = new VerifyTempFileOperation(element);
                    _verifyTempFileOp.StartOperation();
                    AddChildOperation(_verifyTempFileOp);
                }

                if (IsWaitForCompletion)
                    _verifyTempFileOp.WaitForCompletion();

                _verifyTempFileOp.UpdateOperation();
                if (_verifyTempFileOp.IsDone == false)
                    return;

                if (_verifyTempFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.CacheFile;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _verifyTempFileOp.Error;
                }
            }

            if (_steps == ESteps.CacheFile)
            {
                string dataFilePath = _fileCache.GetDataFilePath(_options.Bundle);
                string infoFilePath = _fileCache.GetInfoFilePath(_options.Bundle);
                string dataTempPath = _fileCache.GetDataTempFilePath(_options.Bundle);
                string infoTempPath = _fileCache.GetInfoTempFilePath(_options.Bundle);

                try
                {
                    // 阶段A：准备目标目录，清理可能存在的残留文件
                    FileUtility.EnsureFileDirectory(dataFilePath);
                    DeleteFileSafely(dataTempPath);
                    DeleteFileSafely(infoTempPath);

                    // 阶段B：写入临时文件
                    FileInfo fileInfo = new FileInfo(_options.FilePath);
                    fileInfo.CopyTo(dataTempPath, true);

                    using (FileStream fs = new FileStream(infoTempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        var buffer = new BufferWriter(128);
                        buffer.WriteUInt32(_options.Bundle.FileCRC);
                        buffer.WriteInt64(_options.Bundle.FileSize);
                        buffer.WriteToStream(fs);
                        fs.Flush();
                    }

                    // 阶段C：原子提交
                    if (File.Exists(dataFilePath))
                        File.Delete(dataFilePath);
                    File.Move(dataTempPath, dataFilePath);

                    if (File.Exists(infoFilePath))
                        File.Delete(infoFilePath);
                    File.Move(infoTempPath, infoFilePath);
                }
                catch (Exception ex)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to write cache file. Error: {ex.Message}";
                    YooLogger.Error(Error);

                    // 回滚：清理临时文件，正式文件不受影响
                    DeleteFileSafely(dataTempPath);
                    DeleteFileSafely(infoTempPath);
                    return;
                }

                // 阶段D：注册内存缓存条目
                var cacheEntry = new SandboxFileCacheEntry(_options.Bundle.BundleGUID, infoFilePath, dataFilePath);
                _fileCache.AddEntry(_options.Bundle.BundleGUID, cacheEntry);
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        private static void DeleteFileSafely(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                YooLogger.Warning($"Failed to delete file: {filePath} Error: {ex.Message}");
            }
        }
    }
}
