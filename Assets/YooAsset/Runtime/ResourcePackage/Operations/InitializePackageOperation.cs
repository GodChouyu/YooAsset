using System;

namespace YooAsset
{
    /// <summary>
    /// 初始化资源包裹操作
    /// </summary>
    public class InitializePackageOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            SetPlayMode,
            CheckOptions,
            CheckPlatform,
            CreateCore,
            InitFileSystem,
            Done,
        }

        private readonly ResourcePackage _package;
        private readonly InitializePackageOptions _options;
        private FileSystemHost _fileSystemHost;
        private InitializeFileSystemOperation _initializeFileSystemOp;
        private EPlayMode _playMode;
        private ESteps _steps = ESteps.None;

        internal InitializePackageOperation(ResourcePackage package, InitializePackageOptions options)
        {
            _package = package;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.SetPlayMode;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.SetPlayMode)
            {
                if (_options is EditorSimulateModeOptions)
                    _playMode = EPlayMode.EditorSimulateMode;
                else if (_options is OfflinePlayModeOptions)
                    _playMode = EPlayMode.OfflinePlayMode;
                else if (_options is HostPlayModeOptions)
                    _playMode = EPlayMode.HostPlayMode;
                else if (_options is WebPlayModeOptions)
                    _playMode = EPlayMode.WebPlayMode;
                else if (_options is CustomPlayModeOptions)
                    _playMode = EPlayMode.CustomPlayMode;
                else
                    throw new NotImplementedException($"{_options.GetType().Name}");

                _steps = ESteps.CheckOptions;
            }

            if (_steps == ESteps.CheckOptions)
            {
                // 检测初始化参数
                if (_options.BundleLoadingMaxConcurrency <= 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"{nameof(_options.BundleLoadingMaxConcurrency)} value must be greater than zero.";
                    YooLogger.Error(Error);
                    return;
                }

                _steps = ESteps.CheckPlatform;
            }

            if (_steps == ESteps.CheckPlatform)
            {
#if !UNITY_EDITOR
                if (_playMode == EPlayMode.EditorSimulateMode)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Editor simulate mode only support unity editor.";
                    YooLogger.Error(Error);
                    return;
                }
#endif

#if UNITY_WEBGL
                if (_playMode != EPlayMode.EditorSimulateMode)
                {
                    if (_playMode != EPlayMode.WebPlayMode)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{_playMode} does not support WebGL platform.";
                        YooLogger.Error(Error);
                        return;
                    }
                }
#endif

#if !UNITY_WEBGL
                if (_playMode != EPlayMode.EditorSimulateMode)
                {
                    if (_playMode == EPlayMode.WebPlayMode)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"{nameof(EPlayMode.WebPlayMode)} only supports WebGL platform.";
                        YooLogger.Error(Error);
                        return;
                    }
                }
#endif

                _steps = ESteps.CreateCore;
            }

            if (_steps == ESteps.CreateCore)
            {
                string packageName = _package.PackageName;
                var resourceManager = new ResourceManager(packageName);
                var fileSystemHost = new FileSystemHost(packageName);
                resourceManager.Initialize(_options, fileSystemHost);

                _fileSystemHost = fileSystemHost;
                _package.InternalInitialize(resourceManager, fileSystemHost);
                _steps = ESteps.InitFileSystem;
            }

            if (_steps == ESteps.InitFileSystem)
            {
                if (_initializeFileSystemOp == null)
                {
                    if (_playMode == EPlayMode.EditorSimulateMode)
                    {
                        var initializeParameters = _options as EditorSimulateModeOptions;
                        _initializeFileSystemOp = _fileSystemHost.InitializeAsync(initializeParameters.EditorFileSystemParameters);
                    }
                    else if (_playMode == EPlayMode.OfflinePlayMode)
                    {
                        var initializeParameters = _options as OfflinePlayModeOptions;
                        _initializeFileSystemOp = _fileSystemHost.InitializeAsync(initializeParameters.BuildinFileSystemParameters);
                    }
                    else if (_playMode == EPlayMode.HostPlayMode)
                    {
                        var initializeParameters = _options as HostPlayModeOptions;
                        _initializeFileSystemOp = _fileSystemHost.InitializeAsync(initializeParameters.BuildinFileSystemParameters, initializeParameters.CacheFileSystemParameters);
                    }
                    else if (_playMode == EPlayMode.WebPlayMode)
                    {
                        var initializeParameters = _options as WebPlayModeOptions;
                        _initializeFileSystemOp = _fileSystemHost.InitializeAsync(initializeParameters.WebServerFileSystemParameters, initializeParameters.WebRemoteFileSystemParameters);
                    }
                    else if (_playMode == EPlayMode.CustomPlayMode)
                    {
                        var initializeParameters = _options as CustomPlayModeOptions;
                        _initializeFileSystemOp = _fileSystemHost.InitializeAsync(initializeParameters.FileSystemParameterList);
                    }
                    else
                    {
                        throw new NotImplementedException(_playMode.ToString());
                    }

                    _initializeFileSystemOp.StartOperation();
                    AddChildOperation(_initializeFileSystemOp);
                }

                _initializeFileSystemOp.UpdateOperation();
                if (_initializeFileSystemOp.IsDone == false)
                    return;

                if (_initializeFileSystemOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _initializeFileSystemOp.Error;
                    YooLogger.Error(Error);
                }
            }
        }
        internal override string InternalGetDescription()
        {
            return $"PlayMode : {_playMode}";
        }
    }
}