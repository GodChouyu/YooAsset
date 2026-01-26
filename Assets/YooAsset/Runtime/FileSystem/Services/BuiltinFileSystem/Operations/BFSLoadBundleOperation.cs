using System.IO;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 加载 AssetBundle 文件
    /// </summary>
    internal class BFSLoadAssetBundleOperation : FSLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadBuiltinAssetBundle,
            CheckResult,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private readonly PackageBundle _bundle;
        private LoadAssetBundleOperation _loadAssetBundleOp;
        private ESteps _steps = ESteps.None;


        internal BFSLoadAssetBundleOperation(BuiltinFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            DownloadProgress = 1f;
            DownloadedBytes = _bundle.FileSize;
            _steps = ESteps.LoadBuiltinAssetBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadBuiltinAssetBundle)
            {
                var options = new LoadAssetBundleOptions();
                options.FileLoadPath = _fileSystem.GetBuiltinFileLoadPath(_bundle);
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
                        Error = "Loaded builtin asset bundle is null.";
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
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadAssetBundleOp.Error;
                    YooLogger.Error(Error);
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
    }

    /// <summary>
    /// 加载 RawBundle 文件
    /// </summary>
    internal class BFSLoadRawBundleOperation : FSLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadBuiltinRawBundle,
            CheckResult,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private readonly PackageBundle _bundle;
        private LoadRawBundleOperation _loadRawBundleOp;
        private ESteps _steps = ESteps.None;


        internal BFSLoadRawBundleOperation(BuiltinFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            DownloadProgress = 1f;
            DownloadedBytes = _bundle.FileSize;
            _steps = ESteps.LoadBuiltinRawBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadBuiltinRawBundle)
            {
                var options = new LoadRawBundleOptions();
                options.FileLoadPath = _fileSystem.GetBuiltinFileLoadPath(_bundle);
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
                        Error = "Loaded builtin raw bundle is null.";
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

#if TUANJIE_1_7_OR_NEWER
    /// <summary>
    /// 加载团结文件
    /// </summary>
    internal class BFSLoadInstantBundleOperation : FSLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadInstantBundle,
            CheckResult,
            Done,
        }

        private readonly DefaultBuildinFileSystem _fileSystem;
        private readonly PackageBundle _bundle;
        private AssetBundleCreateRequest _createRequest;
        private AssetBundle _assetBundle;
        private Stream _managedStream;
        private ESteps _steps = ESteps.None;


        internal BFSLoadInstantBundleOperation(DefaultBuildinFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            DownloadProgress = 1f;
            DownloadedBytes = _bundle.FileSize;
            _steps = ESteps.LoadInstantBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadInstantBundle)
            {
                if (_bundle.Encrypted)
                {
                    if (_fileSystem.DecryptionServices == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"The {nameof(IDecryptionServices)} is null.";
                        YooLogger.Error(Error);
                        return;
                    }
                }

                if (IsWaitingForAsyncComplete)
                {
                    if (_bundle.Encrypted)
                    {
                        var decryptResult = _fileSystem.LoadEncryptedAssetBundle(_bundle);
                        _assetBundle = decryptResult.Result;
                        _managedStream = decryptResult.ManagedStream;
                    }
                    else
                    {
                        string filePath = _fileSystem.GetBuildinFileLoadPath(_bundle);
                        _assetBundle = AssetBundle.LoadFromFile(filePath);
                    }
                }
                else
                {
                    if (_bundle.Encrypted)
                    {
                        var decryptResult = _fileSystem.LoadEncryptedAssetBundleAsync(_bundle);
                        _createRequest = decryptResult.CreateRequest;
                        _managedStream = decryptResult.ManagedStream;
                    }
                    else
                    {
                        string filePath = _fileSystem.GetBuildinFileLoadPath(_bundle);
                        _createRequest = AssetBundle.LoadFromFileAsync(filePath);
                    }
                }

                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (_createRequest != null)
                {
                    if (IsWaitingForAsyncComplete)
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
                    if (_bundle.Encrypted)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Failed to load encrypted buildin asset bundle file : {_bundle.BundleName}";
                        YooLogger.Error(Error);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Failed to load buildin asset bundle file : {_bundle.BundleName}";
                        YooLogger.Error(Error);
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Result = new AssetBundleResult(_fileSystem, _bundle, _assetBundle, _managedStream);
                    Status = EOperationStatus.Succeed;
                }
            }
        }
        internal override void InternalWaitForAsyncComplete()
        {
            RunBatchExecution();
        }
    }
#endif
}