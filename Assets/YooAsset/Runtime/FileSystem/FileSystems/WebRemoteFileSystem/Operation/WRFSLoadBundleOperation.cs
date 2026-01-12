
namespace YooAsset
{
    internal class WRFSLoadAssetBundleOperation : FSLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadWebAssetBundle,
            Done,
        }

        private readonly WebRemoteFileSystem _fileSystem;
        private readonly PackageBundle _bundle;
        private LoadWebAssetBundleOperation _loadWebAssetBundleOp;
        private ESteps _steps = ESteps.None;


        internal WRFSLoadAssetBundleOperation(WebRemoteFileSystem fileSystem, PackageBundle bundle)
        {
            _fileSystem = fileSystem;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadWebAssetBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadWebAssetBundle)
            {
                if (_loadWebAssetBundleOp == null)
                {
                    var options = new LoadWebAssetBundleOptions();
                    options.Bundle = _bundle;
                    options.FailedTryAgain = int.MaxValue;
                    options.WatchdogTimeout = _fileSystem.DownloadWatchDogTimeout;
                    options.DownloadBackend = _fileSystem.DownloadBackend;
                    options.DisableUnityWebCache = _fileSystem.DisableUnityWebCache;
                    options.MainURL = _fileSystem.RemoteServices.GetRemoteMainURL(_bundle.FileName);
                    options.FallbackURL = _fileSystem.RemoteServices.GetRemoteFallbackURL(_bundle.FileName);
                    _loadWebAssetBundleOp = _fileSystem.LoadAssetBundleFactory.Invoke(_bundle.Encrypted, options);
                    _loadWebAssetBundleOp.StartOperation();
                    AddChildOperation(_loadWebAssetBundleOp);
                }

                _loadWebAssetBundleOp.UpdateOperation();
                DownloadProgress = _loadWebAssetBundleOp.DownloadProgress;
                DownloadedBytes = _loadWebAssetBundleOp.DownloadedBytes;
                Progress = _loadWebAssetBundleOp.Progress;
                if (_loadWebAssetBundleOp.IsDone == false)
                    return;

                if (_loadWebAssetBundleOp.Status == EOperationStatus.Succeed)
                {
                    if (_loadWebAssetBundleOp.Result == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Loaded asset bundle object is null.";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Result = new AssetBundleResult(_fileSystem, _bundle, _loadWebAssetBundleOp.Result, null);
                        Status = EOperationStatus.Succeed;
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadWebAssetBundleOp.Error;
                }
            }
        }
        internal override void InternalWaitForAsyncComplete()
        {
            if (_steps != ESteps.Done)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "WebGL platform not support sync load method.";
                YooLogger.Error(Error);
            }
        }
    }
}