using UnityEngine;
using UnityEngine.SceneManagement;

namespace YooAsset
{
    /// <summary>
    /// 虚拟资源包的场景加载操作
    /// </summary>
    internal class VBHLoadSceneOperation : BHLoadSceneOperation
    {
        protected enum ESteps
        {
            None,
            LoadScene,
            CheckResult,
            Done,
        }

        private readonly AssetInfo _assetInfo;
        private readonly LoadSceneParameters _loadParams;
        private bool _suspendLoad;
        private AsyncOperation _asyncOperation;
        private ESteps _steps = ESteps.None;

        public VBHLoadSceneOperation(AssetInfo assetInfo, LoadSceneParameters loadParams, bool suspendLoad)
        {
            _assetInfo = assetInfo;
            _loadParams = loadParams;
            _suspendLoad = suspendLoad;
        }
        internal override void InternalStart()
        {
#if UNITY_EDITOR
            _steps = ESteps.LoadScene;
#else
            _steps = ESteps.Done;
            Status = EOperationStatus.Failed;
            Error = $"{nameof(VirtualBundleLoadSceneOperation)} only support unity editor platform.";
#endif
        }
        internal override void InternalUpdate()
        {
#if UNITY_EDITOR
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadScene)
            {
                if (IsWaitForCompletion)
                {
                    Result = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(_assetInfo.AssetPath, _loadParams);
                    _steps = ESteps.CheckResult;
                }
                else
                {
                    _asyncOperation = UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(_assetInfo.AssetPath, _loadParams);
                    if (_asyncOperation != null)
                    {
                        _asyncOperation.allowSceneActivation = !_suspendLoad;
                        _asyncOperation.priority = 100;
                        Result = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
                        _steps = ESteps.CheckResult;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Failed to load scene: {_assetInfo.AssetPath}";
                        YooLogger.Error(Error);
                        return;
                    }
                }
            }

            if (_steps == ESteps.CheckResult)
            {
                if (_asyncOperation != null)
                {
                    if (IsWaitForCompletion)
                    {
                        // 注意：场景加载无法强制异步转同步
                        YooLogger.Error("The scene is already loading asynchronously.");
                    }
                    else
                    {
                        // 注意：在业务层中途可以取消挂起
                        if (_asyncOperation.allowSceneActivation == false)
                        {
                            if (_suspendLoad == false)
                                _asyncOperation.allowSceneActivation = true;
                        }

                        Progress = _asyncOperation.progress;
                        if (_asyncOperation.isDone == false)
                            return;
                    }
                }

                if (Result.IsValid())
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"The loaded scene is invalid: {_assetInfo.AssetPath}";
                    YooLogger.Error(Error);
                }
            }
#endif
        }
        internal override void InternalWaitForCompletion()
        {
            //注意：场景加载不支持异步转同步，为了支持同步加载方法需要实现该方法！
            ExecuteOnce();
        }
        public override void ResumeLoad()
        {
            _suspendLoad = false;
        }
    }
}