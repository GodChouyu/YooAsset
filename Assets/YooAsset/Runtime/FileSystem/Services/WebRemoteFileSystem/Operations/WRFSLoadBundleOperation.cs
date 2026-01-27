
namespace YooAsset
{
    internal class WRFSLoadBundleOperation : FSLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadWebBundle,
            Done,
        }

        private readonly WebRemoteFileSystem _fileSystem;
        private readonly LoadBundleOptions _options;
        private FCLoadBundleOperation _loadBundleOp;
        private ESteps _steps = ESteps.None;


        internal WRFSLoadBundleOperation(WebRemoteFileSystem fileSystem, LoadBundleOptions options)
        {
            _fileSystem = fileSystem;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadWebBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadWebBundle)
            {
                if (_loadBundleOp == null)
                {
                    _loadBundleOp = _fileSystem.FileCache.LoadBundleAsync(_options);
                    _loadBundleOp.StartOperation();
                    AddChildOperation(_loadBundleOp);
                }

                _loadBundleOp.UpdateOperation();
                Progress = _loadBundleOp.Progress;
                DownloadProgress = Progress;
                DownloadedBytes = 0;
                if (_loadBundleOp.IsDone == false)
                    return;

                if (_loadBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadBundleOp.BundleResult == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Loaded bundle result is null.";
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
                }
            }
        }
        internal override void InternalWaitForCompletion()
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