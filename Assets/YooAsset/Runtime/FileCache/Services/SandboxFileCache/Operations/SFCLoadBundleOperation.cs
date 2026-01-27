using System;
using System.IO;
using UnityEngine;

namespace YooAsset
{
    internal class SFCLoadAssetBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadAssetBundle,
            CheckResult,
            TryFallback,
            Done,
        }

        private readonly SandboxFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private FCVerifyCacheOperation _verifyCacheOp;
        private AssetBundleCreateRequest _createRequest;
        private AssetBundle _assetBundle;
        private string _filePath;
        private ESteps _steps = ESteps.None;

        public SFCLoadAssetBundleOperation(SandboxFileCache fileCache, PackageBundle bundle)
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

                _filePath = entry.DataFilePath;
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
                    _steps = ESteps.TryFallback;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleResult = new AssetBundleResult(_filePath, _bundle, _assetBundle, null);
                }
            }

            if (_steps == ESteps.TryFallback)
            {
                // 注意：当缓存文件的校验等级为Low的时候，并不能保证缓存文件的完整性。
                // 说明：在AssetBundle文件加载失败的情况下，我们需要重新验证文件的完整性！
                if (_verifyCacheOp == null)
                {
                    var options = new VerifyCacheOptions();
                    options.Bundle = _bundle;
                    options.FailedDeleteCache = true;
                    _verifyCacheOp = _fileCache.VerifyCacheAsync(options);
                    _verifyCacheOp.StartOperation();
                    AddChildOperation(_verifyCacheOp);
                }

                if (IsWaitForCompletion)
                    _verifyCacheOp.WaitForCompletion();

                _verifyCacheOp.UpdateOperation();
                if (_verifyCacheOp.IsDone == false)
                    return;

                if (_verifyCacheOp.Status == EOperationStatus.Succeeded)
                {
                    // 调用后备加载方法
                    // 注意：在安卓移动平台，华为和三星真机上有极小概率加载资源包失败。
                    // 说明：大多数情况在首次安装下载资源到沙盒内，游戏过程中切换到后台再回到游戏内有很大概率触发！
                    AssetBundle assetBundle = LoadFromMemory();
                    if (assetBundle != null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        BundleResult = new AssetBundleResult(_filePath, _bundle, assetBundle, null);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Failed to load asset bundle from memory : {_bundle.BundleName}";
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _verifyCacheOp.Error;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        private AssetBundle LoadFromMemory()
        {
            byte[] fileData = FileUtility.ReadAllBytes(_filePath);
            if (fileData == null || fileData.Length == 0)
                return null;
            return AssetBundle.LoadFromMemory(fileData);
        }
    }

    internal class SFCLoadRawBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadRawBundle,
            Done,
        }

        private readonly SandboxFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private ESteps _steps = ESteps.None;

        public SFCLoadRawBundleOperation(SandboxFileCache fileCache, PackageBundle bundle)
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

                string filePath = entry.DataFilePath;
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
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }

    internal abstract class SFCLoadAssetBundleFromOperation : FCLoadBundleOperation
    {
        internal abstract AssetBundle LoadFromOffset();
        internal abstract AssetBundleCreateRequest LoadFromOffsetAsync();

        internal abstract AssetBundle LoadFromMemory();
        internal abstract AssetBundleCreateRequest LoadFromMemoryAsync();

        internal abstract AssetBundle LoadFromStream();
        internal abstract AssetBundleCreateRequest LoadFromStreamAsync();
    }
}