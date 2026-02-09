using System;
using System.IO;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 沙盒文件缓存加载 AssetBundle 操作
    /// </summary>
    internal class SFCLoadAssetBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            GetEntry,
            LoadBundle,
            VerifyFile,
            TryFallback,
            Done,
        }

        private readonly SandboxFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private LoadLocalAssetBundleOperation _loadLocalAssetBundleOp;
        private FCVerifyCacheOperation _verifyCacheOp;
        private SandboxFileCacheEntry _cacheEntry;
        private ESteps _steps = ESteps.None;

        public SFCLoadAssetBundleOperation(SandboxFileCache fileCache, PackageBundle bundle)
        {
            _fileCache = fileCache;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.GetEntry;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.GetEntry)
            {
                _cacheEntry = _fileCache.GetEntry(_bundle.BundleGUID);
                if (_cacheEntry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"File cache entry not found: {_bundle.BundleGUID}";
                }
                else
                {
                    _steps = ESteps.LoadBundle;
                }
            }

            if (_steps == ESteps.LoadBundle)
            {
                if (_loadLocalAssetBundleOp == null)
                {
                    var options = new LoadLocalAssetBundleOptions();
                    options.CacheName = _fileCache.GetType().Name;
                    options.Bundle = _bundle;
                    options.FilePath = _cacheEntry.DataFilePath;
                    options.AssetBundleDecryptor = _fileCache.Config.AssetBundleDecryptor;
                    _loadLocalAssetBundleOp = new LoadLocalAssetBundleOperation(options);
                    _loadLocalAssetBundleOp.StartOperation();
                    AddChildOperation(_loadLocalAssetBundleOp);
                }

                if (IsWaitForCompletion)
                    _loadLocalAssetBundleOp.WaitForCompletion();

                _loadLocalAssetBundleOp.UpdateOperation();
                if (_loadLocalAssetBundleOp.IsDone == false)
                    return;

                if (_loadLocalAssetBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadLocalAssetBundleOp.BundleHandle == null)
                        throw new YooInternalException("Loaded asset bundle handle is null.");

                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleHandle = _loadLocalAssetBundleOp.BundleHandle;
                }
                else
                {
                    // 注意：如果引擎加载失败，需要重新验证文件
                    if (_loadLocalAssetBundleOp.UnityEngineLoadFailed)
                    {
                        _steps = ESteps.VerifyFile;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _loadLocalAssetBundleOp.Error;
                    }
                }
            }

            if (_steps == ESteps.VerifyFile)
            {
                // 注意：当缓存文件的校验等级为Low的时候，并不能保证缓存文件的完整性。
                // 说明：在AssetBundle文件加载失败的情况下，我们需要重新验证文件的完整性！
                if (_verifyCacheOp == null)
                {
                    var options = new FCVerifyCacheOptions();
                    options.Bundle = _bundle;
                    options.DeleteCacheEntryOnFailure = true;
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
                    _steps = ESteps.TryFallback;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _verifyCacheOp.Error;
                }
            }

            if (_steps == ESteps.TryFallback)
            {
                // 调用后备加载方法
                // 注意：在安卓移动平台，华为和三星真机上有极小概率加载资源包失败。
                // 说明：大多数情况在首次安装下载资源到沙盒内，游戏过程中切换到后台再回到游戏内有很大概率触发！
                AssetBundle assetBundle;
                if (_bundle.IsEncrypted)
                {
                    if (_fileCache.Config.AssetBundleFallbackDecryptor == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{nameof(SandboxFileCache)} fallback decryptor is null.";
                        return;
                    }

                    assetBundle = FallbackLoadEncryptedAssetBundle(_fileCache.Config.AssetBundleFallbackDecryptor);
                    if (assetBundle == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Failed fallback load encrypted asset bundle: {_bundle.BundleName}";
                    }
                }
                else
                {
                    assetBundle = FallbackLoadAssetBundle();
                    if (assetBundle == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Failed fallback load asset bundle: {_bundle.BundleName}";
                    }
                }

                if (assetBundle != null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleHandle = new AssetBundleHandle(_cacheEntry.DataFilePath, _bundle, assetBundle, null);
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        private AssetBundle FallbackLoadAssetBundle()
        {
            byte[] fileData = FileUtility.ReadAllBytes(_cacheEntry.DataFilePath);
            return AssetBundle.LoadFromMemory(fileData);
        }
        private AssetBundle FallbackLoadEncryptedAssetBundle(IBundleMemoryDecryptor decryptor)
        {
            var args = new BundleDecryptArgs();
            args.Bundle = _bundle;
            args.FilePath = _cacheEntry.DataFilePath;
            var fileData = decryptor.GetDecryptData(args);
            return AssetBundle.LoadFromMemory(fileData);
        }
    }

    /// <summary>
    /// 沙盒文件缓存加载 RawBundle 操作
    /// </summary>
    internal class SFCLoadRawBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            GetEntry,
            LoadBundle,
            Done,
        }

        private readonly SandboxFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private LoadLocalRawBundleOperation _loadLocalRawBundleOp;
        private SandboxFileCacheEntry _cacheEntry;

        private ESteps _steps = ESteps.None;

        public SFCLoadRawBundleOperation(SandboxFileCache fileCache, PackageBundle bundle)
        {
            _fileCache = fileCache;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.GetEntry;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.GetEntry)
            {
                _cacheEntry = _fileCache.GetEntry(_bundle.BundleGUID);
                if (_cacheEntry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"File cache entry not found: {_bundle.BundleGUID}";
                }
                else
                {
                    _steps = ESteps.LoadBundle;
                }
            }

            if (_steps == ESteps.LoadBundle)
            {
                if (_loadLocalRawBundleOp == null)
                {
                    var options = new LoadLocalRawBundleOptions();
                    options.CacheName = _fileCache.GetType().Name;
                    options.Bundle = _bundle;
                    options.FilePath = _cacheEntry.DataFilePath;
                    options.RawBundleDecryptor = _fileCache.Config.RawBundleDecryptor;
                    _loadLocalRawBundleOp = new LoadLocalRawBundleOperation(options);
                    _loadLocalRawBundleOp.StartOperation();
                    AddChildOperation(_loadLocalRawBundleOp);
                }

                if (IsWaitForCompletion)
                    _loadLocalRawBundleOp.WaitForCompletion();

                _loadLocalRawBundleOp.UpdateOperation();
                if (_loadLocalRawBundleOp.IsDone == false)
                    return;

                if (_loadLocalRawBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadLocalRawBundleOp.BundleHandle == null)
                        throw new YooInternalException("Loaded raw bundle handle is null.");

                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    BundleHandle = _loadLocalRawBundleOp.BundleHandle;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadLocalRawBundleOp.Error;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }

#if TUANJIE_1_7_OR_NEWER
    internal class SFCLoadInstantBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            GetEntry,
            LoadBundle,
            Done,
        }

        internal override void InternalStart()
        {
            throw new NotImplementedException();
        }
        internal override void InternalUpdate()
        {
            throw new NotImplementedException();
        }
    }
#endif
}