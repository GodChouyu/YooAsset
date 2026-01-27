
namespace YooAsset
{
    internal class EFCLoadVirtualBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            LoadVirtualBundle,
            CheckResult,
            Done,
        }

        private readonly EditorFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private int _asyncSimulateFrame;
        private ESteps _steps = ESteps.None;

        public EFCLoadVirtualBundleOperation(EditorFileCache fileCache, PackageBundle bundle)
        {
            _fileCache = fileCache;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadVirtualBundle;
            _asyncSimulateFrame = GetAsyncSimulateFrame();
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadVirtualBundle)
            {
                var entry = _fileCache.GetEntry(_bundle.BundleGUID);
                if (entry == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Not found file cache entry: {_bundle.BundleGUID}";
                    return;
                }

                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (IsWaitForCompletion)
                {
                    if (_fileCache.Config.VirtualWebGLMode)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "WebGL mode only support asyn load method.";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                    }
                }
                else
                {
                    _asyncSimulateFrame--;
                    if (_asyncSimulateFrame <= 0)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                    }
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }

        private int GetAsyncSimulateFrame()
        {
            return UnityEngine.Random.Range(_fileCache.Config.AsyncSimulateMinFrame, _fileCache.Config.AsyncSimulateMaxFrame + 1);
        }
    }
}