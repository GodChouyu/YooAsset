using System;
using System.IO;
using UnityEngine;

namespace YooAsset
{
    internal class BFCLoadAssetBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadAssetBundle,
            CheckResult,
            Done,
        }

        private readonly BuiltinFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private AssetBundleCreateRequest _createRequest;
        private AssetBundle _assetBundle;
        private string _filePath;
        private ESteps _steps = ESteps.None;

        public BFCLoadAssetBundleOperation(BuiltinFileCache fileCache, PackageBundle bundle)
        {
            _fileCache = fileCache;
            _bundle = bundle;
        }
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
                var entry = _fileCache.GetEntry(_bundle.BundleGUID);
                if (entry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Not found file cache entry: {_bundle.BundleGUID}";
                    return;
                }

                _filePath = entry.FilePath;
                if (IsWaitForCompletion)
                    _assetBundle = AssetBundle.LoadFromFile(_filePath);
                else
                    _createRequest = AssetBundle.LoadFromFileAsync(_filePath);

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
                    Error = $"Failed to load asset bundle : {_bundle.BundleName}";
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleResult = new AssetBundleResult(_filePath, _bundle, _assetBundle, null);
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }

    internal class BFCLoadRawBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadRawBundle,
            Done,
        }

        private readonly BuiltinFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private ESteps _steps = ESteps.None;

        public BFCLoadRawBundleOperation(BuiltinFileCache fileCache, PackageBundle bundle)
        {
            _fileCache = fileCache;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadRawBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadRawBundle)
            {
                var entry = _fileCache.GetEntry(_bundle.BundleGUID);
                if (entry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Not found file cache entry: {_bundle.BundleGUID}";
                    return;
                }

                string filePath = entry.FilePath;
                if (IsSupportFileIO(filePath) == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"FileIO not supported for builtin path : {filePath}";
                }
                else
                {
                    if (File.Exists(filePath))
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;

                        byte[] data = File.ReadAllBytes(filePath);
                        var rawBundle = new RawBundle(data);
                        BundleResult = new RawBundleResult(filePath, _bundle, rawBundle);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Can not found raw bundle file : {filePath}";
                    }
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }

    internal abstract class BFCLoadAssetBundleFromOperation : FCLoadBundleOperation
    {
        internal abstract AssetBundle LoadFromOffset();
        internal abstract AssetBundleCreateRequest LoadFromOffsetAsync();

        internal abstract AssetBundle LoadFromMemory();
        internal abstract AssetBundleCreateRequest LoadFromMemoryAsync();

        internal abstract AssetBundle LoadFromStream();
        internal abstract AssetBundleCreateRequest LoadFromStreamAsync();
    }
}