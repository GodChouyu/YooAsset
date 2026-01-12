using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 默认的 RawBundle 加载操作（非加密）
    /// 通用实现，适用于 BuiltinFileSystem 和 CacheFileSystem
    /// </summary>
    public class DefaultLoadRawBundleOperation : LoadRawBundleOperation
    {
        private enum ESteps
        {
            None,
            CheckFilePath,
            LoadRawBundle,
            Done
        }

        private ESteps _steps = ESteps.None;

        public DefaultLoadRawBundleOperation(LoadRawBundleOptions options) : base(options) { }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckFilePath;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckFilePath)
            {
                string filePath = _options.FileLoadPath;
                if (IsSupportFileIO(filePath) == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"FileIO not supported for builtin path : {filePath}";
                }
                else
                {
                    _steps = ESteps.LoadRawBundle;
                }
            }

            if (_steps == ESteps.LoadRawBundle)
            {
                string filePath = _options.FileLoadPath;
                if (File.Exists(filePath))
                {
                    byte[] data = File.ReadAllBytes(filePath);
                    Result = new RawBundle(data);
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Can not found raw bundle file : {filePath}";
                }
            }
        }
        internal override void InternalWaitForAsyncComplete()
        {
            RunBatchExecution();
        }
    }

    /// <summary>
    /// 默认的 RawBundle 加载操作（加密）
    /// 通用实现，适用于 CacheFileSystem
    /// </summary>
    public abstract class DefaultLoadRawBundleFromMemoryOperation : LoadRawBundleOperation
    {
        private enum ESteps
        {
            None,
            CheckFilePath,
            LoadRawBundle,
            Done
        }

        private ESteps _steps = ESteps.None;

        public DefaultLoadRawBundleFromMemoryOperation(LoadRawBundleOptions options) : base(options) { }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckFilePath;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckFilePath)
            {
                string filePath = _options.FileLoadPath;
                if (IsSupportFileIO(filePath) == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"FileIO not supported for builtin path : {filePath}";
                }
                else
                {
                    _steps = ESteps.LoadRawBundle;
                }
            }

            if (_steps == ESteps.LoadRawBundle)
            {
                string filePath = _options.FileLoadPath;
                if (File.Exists(filePath))
                {
                    byte[] fileData = File.ReadAllBytes(filePath);
                    byte[] rawData = DecryptData(fileData);
                    if (rawData == null || rawData.Length == 0)
                    {
                        _steps = ESteps.None;
                        Status = EOperationStatus.Failed;
                        Error = "Decrypted raw data is null or empty.";
                    }
                    else
                    {
                        Result = new RawBundle(rawData);
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeed;
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Can not found raw bundle file : {filePath}";
                }
            }
        }
        internal override void InternalWaitForAsyncComplete()
        {
            RunBatchExecution();
        }

        /// <summary>
        /// 文件数据解密
        /// </summary>
        protected abstract byte[] DecryptData(byte[] data);
    }
}
