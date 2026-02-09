
namespace YooAsset
{
    /// <summary>
    /// Web服务端文件系统的初始化操作
    /// </summary>
    internal class WSFSInitializeOperation : FSInitializeOperation
    {
        private enum ESteps
        {
            None,
            CheckPlatform,
            InitializeFileCache,
            Done,
        }

        private readonly WebServerFileSystem _fileSystem;
        private FCInitializeOperation _initializeFileCacheOp;
        private ESteps _steps = ESteps.None;

        public WSFSInitializeOperation(WebServerFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckPlatform;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckPlatform)
            {
#if !UNITY_WEBGL
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = $"{nameof(WebServerFileSystem)} only support the WebGL platform.";
#else
                _steps = ESteps.InitializeFileCache;
#endif
            }

            if (_steps == ESteps.InitializeFileCache)
            {
                if (_initializeFileCacheOp == null)
                {
                    _initializeFileCacheOp = _fileSystem.FileCache.InitializeAsync();
                    _initializeFileCacheOp.StartOperation();
                    AddChildOperation(_initializeFileCacheOp);
                }

                _initializeFileCacheOp.UpdateOperation();
                Progress = _initializeFileCacheOp.Progress;
                if (_initializeFileCacheOp.IsDone == false)
                    return;

                if (_initializeFileCacheOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _initializeFileCacheOp.Error;
                }
            }
        }
    }
}