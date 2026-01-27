using System;
using System.IO;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 默认的 AssetBundle 加载操作（非加密）
    /// 通用实现，适用于 BuiltinFileSystem 和 CacheFileSystem
    /// </summary>
    public class DefaultLoadAssetBundleOperation : LoadAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadAssetBundle,
            CheckResult,
            Done,
        }

        private AssetBundleCreateRequest _createRequest;
        private ESteps _steps = ESteps.None;

        public DefaultLoadAssetBundleOperation(LoadAssetBundleOptions opionts) : base(opionts) { }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadAssetBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadAssetBundle)
            {
                if (IsWaitForCompletion)
                    Result = AssetBundle.LoadFromFile(_options.FileLoadPath);
                else
                    _createRequest = AssetBundle.LoadFromFileAsync(_options.FileLoadPath);

                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (_createRequest != null)
                {
                    if (IsWaitForCompletion)
                    {
                        // 强制挂起主线程（注意：该操作会很耗时）
                        YooLogger.Warning("Suspend the main thread to load unity bundle.");
                        Result = _createRequest.assetBundle;
                    }
                    else
                    {
                        if (_createRequest.isDone == false)
                            return;
                        Result = _createRequest.assetBundle;
                    }
                }

                if (Result == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to load asset bundle file : {_options.Bundle.BundleName}";
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        public override AssetBundle LoadFromMemory()
        {
            if (IsSupportFileIO(_options.FileLoadPath) == false)
                return null;

            byte[] fileData = FileUtility.ReadAllBytes(_options.FileLoadPath);
            if (fileData == null || fileData.Length == 0)
                return null;

            return AssetBundle.LoadFromMemory(fileData);
        }
    }

    /// <summary>
    /// 默认的 AssetBundle 加载操作（加密）
    /// 通用实现，适用于 BuiltinFileSystem 和 CacheFileSystem
    /// </summary>
    public abstract class DefaultLoadAssetBundleFromOffsetOperation : LoadAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadAssetBundle,
            CheckResult,
            Done,
        }

        private AssetBundleCreateRequest _createRequest;
        private ESteps _steps = ESteps.None;

        public DefaultLoadAssetBundleFromOffsetOperation(LoadAssetBundleOptions options) : base(options) { }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadAssetBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadAssetBundle)
            {
                ulong offset = GetFileOffset();
                if (IsWaitForCompletion)
                    Result = AssetBundle.LoadFromFile(_options.FileLoadPath, 0, offset);
                else
                    _createRequest = AssetBundle.LoadFromFileAsync(_options.FileLoadPath, 0, offset);

                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (_createRequest != null)
                {
                    if (IsWaitForCompletion)
                    {
                        // 强制挂起主线程（注意：该操作会很耗时）
                        YooLogger.Warning("Suspend the main thread to load unity bundle.");
                        Result = _createRequest.assetBundle;
                    }
                    else
                    {
                        if (_createRequest.isDone == false)
                            return;
                        Result = _createRequest.assetBundle;
                    }
                }

                if (Result == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to load asset bundle file : {_options.Bundle.BundleName}";
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        /// <summary>
        /// 获取偏移值
        /// </summary>
        protected abstract uint GetFileOffset();

        public override AssetBundle LoadFromMemory()
        {
            int offset = (int)GetFileOffset();
            byte[] fileData = File.ReadAllBytes(_options.FileLoadPath);
            if (fileData == null || fileData.Length <= offset)
                return null;

            // 跳过偏移量
            byte[] bundleData = new byte[fileData.Length - offset];
            Buffer.BlockCopy(fileData, offset, bundleData, 0, bundleData.Length);

            return AssetBundle.LoadFromMemory(bundleData);
        }
    }

    /// <summary>
    /// 默认的 AssetBundle 加载操作（加密）
    /// 通用实现，适用于 CacheFileSystem
    /// </summary>
    public abstract class DefaultLoadAssetBundleFromMemoryOperation : LoadAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            CheckFilePath,
            LoadAssetBundle,
            CheckResult,
            Done,
        }

        private AssetBundleCreateRequest _createRequest;
        private ESteps _steps = ESteps.None;

        public DefaultLoadAssetBundleFromMemoryOperation(LoadAssetBundleOptions options) : base(options) { }
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
                    _steps = ESteps.LoadAssetBundle;
                }
            }

            if (_steps == ESteps.LoadAssetBundle)
            {
                byte[] fileData = File.ReadAllBytes(_options.FileLoadPath);
                byte[] rawData = DecryptData(fileData);
                if (rawData == null || rawData.Length == 0)
                {
                    _steps = ESteps.None;
                    Status = EOperationStatus.Failed;
                    Error = "Decrypted raw data is null or empty.";
                    return;
                }

                if (IsWaitForCompletion)
                    Result = AssetBundle.LoadFromMemory(rawData);
                else
                    _createRequest = AssetBundle.LoadFromMemoryAsync(rawData);

                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (_createRequest != null)
                {
                    if (IsWaitForCompletion)
                    {
                        // 强制挂起主线程（注意：该操作会很耗时）
                        YooLogger.Warning("Suspend the main thread to load unity bundle.");
                        Result = _createRequest.assetBundle;
                    }
                    else
                    {
                        if (_createRequest.isDone == false)
                            return;
                        Result = _createRequest.assetBundle;
                    }
                }

                if (Result == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to load asset bundle file : {_options.Bundle.BundleName}";
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        /// <summary>
        /// 文件数据解密
        /// </summary>
        protected abstract byte[] DecryptData(byte[] data);

        public override AssetBundle LoadFromMemory()
        {
            byte[] fileData = File.ReadAllBytes(_options.FileLoadPath);
            byte[] rawData = DecryptData(fileData);
            if (rawData == null || rawData.Length == 0)
                return null;

            return AssetBundle.LoadFromMemory(rawData);
        }
    }

    /// <summary>
    /// 默认的 AssetBundle 加载操作（加密）
    /// 通用实现，适用于 CacheFileSystem
    /// </summary>
    public abstract class DefaultLoadAssetBundleFromStreamOperation : LoadAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            CheckFilePath,
            LoadAssetBundle,
            CheckResult,
            Done,
        }

        private AssetBundleCreateRequest _createRequest;
        private ESteps _steps = ESteps.None;

        public DefaultLoadAssetBundleFromStreamOperation(LoadAssetBundleOptions options) : base(options) { }
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
                    _steps = ESteps.LoadAssetBundle;
                }
            }

            if (_steps == ESteps.LoadAssetBundle)
            {
                ManagedStream = CreateManagedFileStream();
                uint bufferSize = GetManagedReadBufferSize();

                if (IsWaitForCompletion)
                    Result = AssetBundle.LoadFromStream(ManagedStream, 0, bufferSize);
                else
                    _createRequest = AssetBundle.LoadFromStreamAsync(ManagedStream, 0, bufferSize);

                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (_createRequest != null)
                {
                    if (IsWaitForCompletion)
                    {
                        // 强制挂起主线程（注意：该操作会很耗时）
                        YooLogger.Warning("Suspend the main thread to load unity bundle.");
                        Result = _createRequest.assetBundle;
                    }
                    else
                    {
                        if (_createRequest.isDone == false)
                            return;
                        Result = _createRequest.assetBundle;
                    }
                }

                if (Result == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to load asset bundle file : {_options.Bundle.BundleName}";
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        /// <summary>
        /// 获取文件流
        /// </summary>
        protected abstract FileStream CreateManagedFileStream();

        /// <summary>
        /// 获取缓冲池大小
        /// </summary>
        protected abstract uint GetManagedReadBufferSize();

        /// <summary>
        /// 文件数据解密
        /// </summary>
        protected abstract byte[] DecryptData(byte[] data);

        public override AssetBundle LoadFromMemory()
        {
            byte[] fileData = File.ReadAllBytes(_options.FileLoadPath);
            byte[] rawData = DecryptData(fileData);
            if (rawData == null || rawData.Length == 0)
                return null;

            return AssetBundle.LoadFromMemory(rawData);
        }
    }
}