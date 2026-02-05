
namespace YooAsset
{
    internal class BFSLoadBundleOperation : FSLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            Prepare,
            UnpackFile,
            AbortUnpack,
            LoadUnpackBundle,
            LoadBuiltinBundle,
            CheckResult,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private readonly FCLoadBundleOptions _options;
        private FSDownloadFileOperation _unpackFileOp;
        private FCLoadBundleOperation _loadBundleOp;
        private ESteps _steps = ESteps.None;

        internal BFSLoadBundleOperation(BuiltinFileSystem fileSystem, FCLoadBundleOptions options)
        {
            _fileSystem = fileSystem;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.Prepare;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.Prepare)
            {
                if (_fileSystem.IsUnpackBundleFile(_options.Bundle))
                {
                    if (_fileSystem.UnpackFileCache.IsCached(_options.Bundle.BundleGUID))
                    {
                        DownloadProgress = 1f;
                        DownloadedBytes = _options.Bundle.FileSize;
                        _steps = ESteps.LoadUnpackBundle;
                    }
                    else
                    {
                        _steps = ESteps.UnpackFile;
                    }
                }
                else
                {
                    DownloadProgress = 1f;
                    DownloadedBytes = _options.Bundle.FileSize;
                    _steps = ESteps.LoadBuiltinBundle;
                }
            }

            if (_steps == ESteps.UnpackFile)
            {
                // 中断解压
                if (AbortDownloadFile)
                {
                    if (_unpackFileOp != null)
                        _unpackFileOp.AbortOperation();
                    _steps = ESteps.AbortUnpack;
                }
            }

            if (_steps == ESteps.UnpackFile)
            {
                if (_unpackFileOp == null)
                {
                    var options = new FSDownloadFileOptions(_options.Bundle, int.MaxValue);
                    _unpackFileOp = _fileSystem.DownloadFileAsync(options); // 注意：异步任务的开启由调度器统一控制
                    AddChildOperation(_unpackFileOp);
                }

                if (IsWaitForCompletion)
                    _unpackFileOp.WaitForCompletion();

                _unpackFileOp.UpdateOperation();
                DownloadProgress = _unpackFileOp.DownloadProgress;
                DownloadedBytes = _unpackFileOp.DownloadedBytes;
                if (_unpackFileOp.IsDone == false)
                    return;

                if (_unpackFileOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.LoadUnpackBundle;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _unpackFileOp.Error;
                }
            }

            if (_steps == ESteps.AbortUnpack)
            {
                if (_unpackFileOp != null)
                {
                    if (IsWaitForCompletion)
                        _unpackFileOp.WaitForCompletion();

                    _unpackFileOp.UpdateOperation();
                    if (_unpackFileOp.IsDone == false)
                        return;
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Abort download file.";
            }

            if (_steps == ESteps.LoadUnpackBundle)
            {
                _loadBundleOp = _fileSystem.UnpackFileCache.LoadBundleAsync(_options);
                _loadBundleOp.StartOperation();
                AddChildOperation(_loadBundleOp);
                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.LoadBuiltinBundle)
            {
                _loadBundleOp = _fileSystem.BuiltinFileCache.LoadBundleAsync(_options);
                _loadBundleOp.StartOperation();
                AddChildOperation(_loadBundleOp);
                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (IsWaitForCompletion)
                    _loadBundleOp.WaitForCompletion();

                _loadBundleOp.UpdateOperation();
                if (_loadBundleOp.IsDone == false)
                    return;

                if (_loadBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadBundleOp.BundleResult == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "Loaded bundle result is null.";
                        YooLogger.Error(Error);
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        Result = _loadBundleOp.BundleResult;
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadBundleOp.Error;
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
    internal class BFSLoadInstantBundleOperation
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