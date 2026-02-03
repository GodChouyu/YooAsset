using System.IO;
using UnityEngine;

namespace YooAsset
{
    internal class LoadLocalAssetBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadBundle,
            CheckResult,
            Done,
        }

        private readonly PackageBundle _bundle;
        private readonly LoadLocalAssetBundleOptions _options;
        private AssetBundleCreateRequest _createRequest;
        private AssetBundle _assetBundle;
        private Stream _loadStream;
        private ESteps _steps = ESteps.None;

        public bool UnityEngineLoadFailed = false;

        public LoadLocalAssetBundleOperation(LoadLocalAssetBundleOptions options)
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
                if (_bundle.IsEncrypted == false)
                {
                    LoadFromFile();
                }
                else
                {
                    var decryptor = _options.Decryptor;
                    if (decryptor == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{_options.CacheName} decryptor is null.";
                        return;
                    }

                    if (decryptor is IBundleOffsetDecryptor offsetDecryptor)
                    {
                        LoadFromFileWithOffset(offsetDecryptor);
                    }
                    else if (decryptor is IBundleMemoryDecryptor memoryDecryptor)
                    {
                        LoadFromMemory(memoryDecryptor);
                    }
                    else if (decryptor is IBundleStreamDecryptor streamDecryptor)
                    {
                        LoadFromStream(streamDecryptor);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{_options.CacheName} not support {decryptor.GetType().Name}";
                        return;
                    }
                }

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
                        _assetBundle = _createRequest.assetBundle;
                    }
                    else
                    {
                        if (_createRequest.isDone == false)
                            return;
                        _assetBundle = _createRequest.assetBundle;
                    }
                }

                if (_assetBundle == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Unity engine load failed.";
                    UnityEngineLoadFailed = true;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleResult = new AssetBundleResult(_options.FilePath, _options.Bundle, _assetBundle, _loadStream);
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        private void LoadFromFile()
        {
            if (IsWaitForCompletion)
                _assetBundle = AssetBundle.LoadFromFile(_options.FilePath);
            else
                _createRequest = AssetBundle.LoadFromFileAsync(_options.FilePath);
        }
        private void LoadFromFileWithOffset(IBundleOffsetDecryptor decryptor)
        {
            var args = new BundleDecryptArgs();
            args.Bundle = _bundle;
            args.FilePath = _options.FilePath;
            uint offset = decryptor.GetFileOffset(args);

            if (IsWaitForCompletion)
                _assetBundle = AssetBundle.LoadFromFile(_options.FilePath, 0, offset);
            else
                _createRequest = AssetBundle.LoadFromFileAsync(_options.FilePath, 0, offset);
        }
        private void LoadFromMemory(IBundleMemoryDecryptor decryptor)
        {
            var args = new BundleDecryptArgs();
            args.Bundle = _bundle;
            args.FilePath = _options.FilePath;
            var binaryData = decryptor.GetDecryptData(args);

            if (IsWaitForCompletion)
                _assetBundle = AssetBundle.LoadFromMemory(binaryData);
            else
                _createRequest = AssetBundle.LoadFromMemoryAsync(binaryData);
        }
        private void LoadFromStream(IBundleStreamDecryptor decryptor)
        {
            var args = new BundleDecryptArgs();
            args.Bundle = _bundle;
            args.FilePath = _options.FilePath;
            uint bufferSize = decryptor.GetReadBufferSize(args);
            _loadStream = decryptor.GetDecryptStream(args);

            if (IsWaitForCompletion)
                _assetBundle = AssetBundle.LoadFromStream(_loadStream, 0, bufferSize);
            else
                _createRequest = AssetBundle.LoadFromStreamAsync(_loadStream, 0, bufferSize);
        }
    }
}
