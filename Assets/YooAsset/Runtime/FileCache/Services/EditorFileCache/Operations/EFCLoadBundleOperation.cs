
namespace YooAsset
{
    /// <summary>
    /// 编辑器文件缓存加载资源包操作
    /// </summary>
    internal class EFCLoadBundleOperation : FCLoadBundleOperation
    {
        private enum ESteps
        {
            None,
            CheckCache,
            CheckFilePath,
            LoadBundle,
            Done,
        }

        private readonly EditorFileCache _fileCache;
        private readonly PackageBundle _bundle;
        private int _asyncSimulateFrame;
        private string _editorFilePath;
        private ESteps _steps = ESteps.None;

        public EFCLoadBundleOperation(EditorFileCache fileCache, PackageBundle bundle)
        {
            _fileCache = fileCache;
            _bundle = bundle;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckCache;
            _asyncSimulateFrame = GetAsyncSimulateFrame();
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckCache)
            {
                if (_fileCache.IsCached(_bundle.BundleGUID) == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"File cache entry not found: {_bundle.BundleGUID}";
                    return;
                }

                _steps = ESteps.CheckFilePath;
            }

            if (_steps == ESteps.CheckFilePath)
            {
                _editorFilePath = EditorFileSystemTools.GetEditorFilePath(_bundle);
                if (string.IsNullOrEmpty(_editorFilePath))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Editor file path is null. Bundle: {_bundle.BundleName}";
                }
                else
                {
                    _steps = ESteps.LoadBundle;
                }
            }

            if (_steps == ESteps.LoadBundle)
            {
                if (IsWaitForCompletion)
                {
                    if (_fileCache.Config.VirtualWebGLMode)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "WebGL mode only supports async load method.";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        BundleHandle = new VirtualBundleHandle(_editorFilePath, _bundle);
                    }
                }
                else
                {
                    _asyncSimulateFrame--;
                    if (_asyncSimulateFrame <= 0)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        BundleHandle = new VirtualBundleHandle(_editorFilePath, _bundle);
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