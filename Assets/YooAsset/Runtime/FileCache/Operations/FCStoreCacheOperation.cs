using System;
using System.IO;

namespace YooAsset
{
    internal class FCStoreCacheOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            Check,
            VerifyFile,
            CacheFile,
            Done,
        }

        private BundleCache _cache;
        private readonly FCStoreCacheOptions _options;
        private VerifyTempFileOperation _verifyOperation;
        private ESteps _steps = ESteps.None;

        public FCStoreCacheOperation(BundleCache cache, FCStoreCacheOptions options)
        {
            _cache = cache;
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
                if (_cache.IsReadOnly)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"{nameof(BundleCache)} is readonly.";
                    return;
                }

                if (_cache.IsCached(_options.Bundle.BundleGUID))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "The bundle is cached.";
                }
                else
                {
                    _steps = ESteps.VerifyFile;
                }
            }

            if (_steps == ESteps.VerifyFile)
            {
                if (_verifyOperation == null)
                {
                    var element = new TempFileInfo(_options.FilePath, _options.Bundle.FileCRC, _options.Bundle.FileSize);
                    _verifyOperation = new VerifyTempFileOperation(element);
                    _verifyOperation.StartOperation();
                    AddChildOperation(_verifyOperation);
                }

                if (IsWaitForCompletion)
                    _verifyOperation.WaitForCompletion();

                _verifyOperation.UpdateOperation();
                if (_verifyOperation.IsDone == false)
                    return;

                if (_verifyOperation.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.CacheFile;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _verifyOperation.Error;
                }
            }

            if (_steps == ESteps.CacheFile)
            {
                string infoFilePath = _cache.GetInfoFilePath(_options.Bundle);
                string dataFilePath = _cache.GetDataFilePath(_options.Bundle);

                try
                {
                    if (File.Exists(infoFilePath))
                        File.Delete(infoFilePath);
                    if (File.Exists(dataFilePath))
                        File.Delete(dataFilePath);

                    // 拷贝数据文件
                    FileUtility.CreateFileDirectory(dataFilePath);
                    FileInfo fileInfo = new FileInfo(_options.FilePath);
                    fileInfo.CopyTo(dataFilePath, true);

                    // 写入信息文件
                    FileUtility.CreateFileDirectory(infoFilePath);
                    using (FileStream fs = new FileStream(infoFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        _cache.SharedBuffer.Clear();
                        _cache.SharedBuffer.WriteUInt32(_options.Bundle.FileCRC);
                        _cache.SharedBuffer.WriteInt64(_options.Bundle.FileSize);
                        _cache.SharedBuffer.WriteToStream(fs);
                        fs.Flush();
                    }
                }
                catch (Exception ex)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to write cache file. Error: {ex.Message}";
                    YooLogger.Error(Error);
                    return; //失败后直接返回
                }

                var cacheEntry = new BundleCacheEntry(_options.Bundle.BundleGUID, infoFilePath, dataFilePath, _options.Bundle.FileCRC, _options.Bundle.FileSize);
                _cache.AddEntry(_options.Bundle.BundleGUID, cacheEntry);
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}
