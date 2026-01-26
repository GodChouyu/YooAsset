using System;
using System.IO;
using UnityEngine;

namespace YooAsset
{
    internal class CFSLoadAssetBundleOperation : FSLoadBundleOperation
    {
        protected enum ESteps
        {
            None,
            CheckExist,
            DownloadFile,
            AbortDownload,
            LoadCacheAssetBundle,
            CheckResult,
            TryFallback,
            Done,
        }

        protected readonly CacheFileSystem _fileSystem;
        protected readonly PackageBundle _bundle;
        protected FSDownloadFileOperation _downloadFileOp;
        protected LoadAssetBundleOperation _loadAssetBundleOp;
        protected ESteps _steps = ESteps.None;


        internal CFSLoadAssetBundleOperation(CacheFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckExist;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckExist)
            {
                if (_fileSystem.Exists(_bundle))
                {
                    DownloadProgress = 1f;
                    DownloadedBytes = _bundle.FileSize;
                    _steps = ESteps.LoadCacheAssetBundle;
                }
                else
                {
                    if (_fileSystem.DisableOnDemandDownload)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"The bundle not cached : {_bundle.BundleName}";
                        YooLogger.Warning(Error);
                    }
                    else
                    {
                        _steps = ESteps.DownloadFile;
                    }
                }
            }

            if (_steps == ESteps.DownloadFile)
            {
                // 中断下载
                if (AbortDownloadFile)
                {
                    if (_downloadFileOp != null)
                        _downloadFileOp.AbortOperation();
                    _steps = ESteps.AbortDownload;
                }
            }

            if (_steps == ESteps.DownloadFile)
            {
                if (_downloadFileOp == null)
                {
                    DownloadFileOptions options = new DownloadFileOptions(_bundle, int.MaxValue);
                    _downloadFileOp = _fileSystem.DownloadFileAsync(options);
                    _downloadFileOp.StartOperation();
                    AddChildOperation(_downloadFileOp);
                }

                if (IsWaitForCompletion)
                    _downloadFileOp.WaitForCompletion();

                _downloadFileOp.UpdateOperation();
                DownloadProgress = _downloadFileOp.DownloadProgress;
                DownloadedBytes = _downloadFileOp.DownloadedBytes;
                if (_downloadFileOp.IsDone == false)
                    return;

                if (_downloadFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.LoadCacheAssetBundle;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _downloadFileOp.Error;
                }
            }

            if (_steps == ESteps.AbortDownload)
            {
                if (_downloadFileOp != null)
                {
                    if (IsWaitForCompletion)
                        _downloadFileOp.WaitForCompletion();

                    _downloadFileOp.UpdateOperation();
                    if (_downloadFileOp.IsDone == false)
                        return;
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Abort download file.";
            }

            if (_steps == ESteps.LoadCacheAssetBundle)
            {
                var options = new LoadAssetBundleOptions();
                options.FileLoadPath = _fileSystem.GetCacheBundleFileLoadPath(_bundle);
                options.Bundle = _bundle;
                _loadAssetBundleOp = _fileSystem.LoadAssetBundleFactory.Invoke(_bundle.Encrypted, options);
                _loadAssetBundleOp.StartOperation();
                AddChildOperation(_loadAssetBundleOp);
                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (IsWaitForCompletion)
                    _loadAssetBundleOp.WaitForCompletion();

                _loadAssetBundleOp.UpdateOperation();
                if (_loadAssetBundleOp.IsDone == false)
                    return;

                if (_loadAssetBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadAssetBundleOp.Result == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "Loaded cache asset bundle is null.";
                        YooLogger.Error(Error);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Result = new AssetBundleResult(_fileSystem, _bundle, _loadAssetBundleOp.Result, _loadAssetBundleOp.ManagedStream);
                        Status = EOperationStatus.Succeeded;
                    }
                }
                else
                {
                    if (_loadAssetBundleOp is LoadAssetBundleCompleteOperation)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _loadAssetBundleOp.Error;
                        YooLogger.Error(Error);
                    }
                    else
                    {
                        // 加载失败，尝试后备加载
                        _steps = ESteps.TryFallback;
                    }
                }
            }

            if (_steps == ESteps.TryFallback)
            {
                var entry = _fileSystem.Cache.GetEntry(_bundle.BundleGUID);
                if (entry == null)
                    throw new YooInternalException();

                // 注意：当缓存文件的校验等级为Low的时候，并不能保证缓存文件的完整性。
                // 说明：在AssetBundle文件加载失败的情况下，我们需要重新验证文件的完整性！
                var verifyResult = FileVerifyTools.FileVerify(entry.DataFilePath, _bundle.FileSize, _bundle.FileCRC);
                if (verifyResult == EFileVerifyResult.Succeed)
                {
                    // 调用后备加载方法
                    // 注意：在安卓移动平台，华为和三星真机上有极小概率加载资源包失败。
                    // 说明：大多数情况在首次安装下载资源到沙盒内，游戏过程中切换到后台再回到游戏内有很大概率触发！
                    AssetBundle assetBundle = _loadAssetBundleOp.LoadFromMemory();
                    if (assetBundle != null)
                    {
                        _steps = ESteps.Done;
                        Result = new AssetBundleResult(_fileSystem, _bundle, assetBundle, null);
                        Status = EOperationStatus.Succeeded;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Failed to load asset bundle from memory : {_bundle.BundleName}";
                        YooLogger.Error(Error);
                    }
                }
                else
                {
                    // 文件损坏，删除缓存
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Find corrupted asset bundle file and delete : {_bundle.BundleName}";
                    YooLogger.Error(Error);
                    _fileSystem.Cache.RemoveEntry(_bundle.BundleGUID);
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }

    internal class CFSLoadRawBundleOperation : FSLoadBundleOperation
    {
        protected enum ESteps
        {
            None,
            CheckExist,
            DownloadFile,
            AbortDownload,
            LoadCacheRawBundle,
            CheckResult,
            Done,
        }

        protected readonly CacheFileSystem _fileSystem;
        protected readonly PackageBundle _bundle;
        protected FSDownloadFileOperation _downloadFileOp;
        protected LoadRawBundleOperation _loadRawBundleOp;
        protected ESteps _steps = ESteps.None;


        internal CFSLoadRawBundleOperation(CacheFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckExist;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckExist)
            {
                if (_fileSystem.Exists(_bundle))
                {
                    // 注意：缓存的原生文件的格式，可能会在业务端根据需求发生变动！
                    // 注意：这里需要校验文件格式，如果不一致对本地文件进行修正！
                    var entry = _fileSystem.Cache.GetEntry(_bundle.BundleGUID);
                    if (entry == null)
                        throw new YooInternalException();

                    if (File.Exists(entry.DataFilePath) == false)
                    {
                        try
                        {
                            string destFilePath = _fileSystem.Cache.GetDataFilePath(_bundle);
                            entry.MoveFile(destFilePath);
                            _steps = ESteps.LoadCacheRawBundle;
                        }
                        catch (Exception ex)
                        {
                            _steps = ESteps.Done;
                            Status = EOperationStatus.Failed;
                            Error = $"Faild rename cached data file : {ex.Message}";
                        }
                    }
                    else
                    {
                        DownloadProgress = 1f;
                        DownloadedBytes = _bundle.FileSize;
                        _steps = ESteps.LoadCacheRawBundle;
                    }
                }
                else
                {
                    _steps = ESteps.DownloadFile;
                }
            }

            if (_steps == ESteps.DownloadFile)
            {
                // 中断下载
                if (AbortDownloadFile)
                {
                    if (_downloadFileOp != null)
                        _downloadFileOp.AbortOperation();
                    _steps = ESteps.AbortDownload;
                }
            }

            if (_steps == ESteps.DownloadFile)
            {
                if (_downloadFileOp == null)
                {
                    DownloadFileOptions options = new DownloadFileOptions(_bundle, int.MaxValue);
                    _downloadFileOp = _fileSystem.DownloadFileAsync(options);
                    _downloadFileOp.StartOperation();
                    AddChildOperation(_downloadFileOp);
                }

                if (IsWaitForCompletion)
                    _downloadFileOp.WaitForCompletion();

                _downloadFileOp.UpdateOperation();
                DownloadProgress = _downloadFileOp.DownloadProgress;
                DownloadedBytes = _downloadFileOp.DownloadedBytes;
                if (_downloadFileOp.IsDone == false)
                    return;

                if (_downloadFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.LoadCacheRawBundle;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _downloadFileOp.Error;
                }
            }

            if (_steps == ESteps.AbortDownload)
            {
                if (_downloadFileOp != null)
                {
                    if (IsWaitForCompletion)
                        _downloadFileOp.WaitForCompletion();

                    _downloadFileOp.UpdateOperation();
                    if (_downloadFileOp.IsDone == false)
                        return;
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Abort download file.";
            }

            if (_steps == ESteps.LoadCacheRawBundle)
            {
                var options = new LoadRawBundleOptions();
                options.FileLoadPath = _fileSystem.GetCacheBundleFileLoadPath(_bundle);
                options.Bundle = _bundle;
                _loadRawBundleOp = _fileSystem.LoadRawBundleFactory.Invoke(_bundle.Encrypted, options);
                _loadRawBundleOp.StartOperation();
                AddChildOperation(_loadRawBundleOp);
                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (IsWaitForCompletion)
                    _loadRawBundleOp.WaitForCompletion();

                _loadRawBundleOp.UpdateOperation();
                if (_loadRawBundleOp.IsDone == false)
                    return;

                if (_loadRawBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadRawBundleOp.Result == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "Loaded cache raw bundle is null.";
                        YooLogger.Error(Error);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Result = new RawBundleResult(_fileSystem, _bundle, _loadRawBundleOp.Result);
                        Status = EOperationStatus.Succeeded;
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadRawBundleOp.Error;
                    YooLogger.Error(Error);
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }
}