using System;
using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 从本地加载 RawBundle 操作
    /// </summary>
    internal class LoadLocalRawBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadBundle,
            CheckResult,
            Done,
        }

        protected readonly LoadLocalRawBundleOptions _options;
        private RawBundle _rawBundle;
        private ESteps _steps = ESteps.None;

        public LoadLocalRawBundleOperation(LoadLocalRawBundleOptions options)
        {
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadBundle)
            {
                if (_options.Bundle.IsEncrypted == false)
                {
                    if (SupportsFileIO(_options.FilePath) == false)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"FileIO not supported for builtin path : {_options.FilePath}";
                        return;
                    }

                    LoadFromFile();
                }
                else
                {
                    var decryptor = _options.RawBundleDecryptor;
                    if (decryptor == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{_options.CacheName} decryptor is null.";
                        return;
                    }

                    LoadResult result;
                    if (decryptor is IBundleMemoryDecryptor memoryDecryptor)
                    {
                        result = LoadFromMemory(memoryDecryptor);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{_options.CacheName} not support {decryptor.GetType().Name}";
                        return;
                    }

                    if (result.Succeeded == false)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = result.Error;
                        return;
                    }
                }

                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (_rawBundle == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Loaded raw bundle is null.";
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleResult = new RawBundleResult(_options.FilePath, _options.Bundle, _rawBundle);
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        private void LoadFromFile()
        {
            byte[] data = File.ReadAllBytes(_options.FilePath);
            _rawBundle = new RawBundle(data);
        }
        private LoadResult LoadFromMemory(IBundleMemoryDecryptor decryptor)
        {
            var args = new BundleDecryptArgs();
            args.Bundle = _options.Bundle;
            args.FilePath = _options.FilePath;
            var binaryData = decryptor.GetDecryptData(args);
            if (binaryData == null)
                return LoadResult.Failure($"{_options.CacheName} decryptor returned null data.");

            _rawBundle = new RawBundle(binaryData);
            return LoadResult.Default();
        }
    }
}
