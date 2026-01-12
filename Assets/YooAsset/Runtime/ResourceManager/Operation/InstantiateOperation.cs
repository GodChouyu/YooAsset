using UnityEngine;

namespace YooAsset
{
    public sealed class InstantiateOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            LoadObject,
            CloneSync,
            CloneAsync,
            Done,
        }

        private readonly AssetHandle _handle;
        private readonly InstantiateOptions _options;
        private ESteps _steps = ESteps.None;

#if UNITY_2023_3_OR_NEWER
        private AsyncInstantiateOperation _instantiateAsync;
#endif

        /// <summary>
        /// 实例化的游戏对象
        /// </summary>
        public GameObject Result = null;


        internal InstantiateOperation(AssetHandle handle, InstantiateOptions options)
        {
            _handle = handle;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadObject;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadObject)
            {
                if (_handle.IsValidWithWarning == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"{nameof(AssetHandle)} is invalid.";
                    return;
                }

                if (IsWaitingForAsyncComplete)
                    _handle.WaitForAsyncComplete();

                if (_handle.IsDone == false)
                    return;

                if (_handle.AssetObject == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"{nameof(AssetHandle.AssetObject)} is null.";
                    return;
                }

#if UNITY_2023_3_OR_NEWER
                //TODO 官方BUG
                // BUG环境：Windows平台，Unity2022.3.41f1版本，编辑器模式。
                // BUG描述：异步实例化Prefab预制体，有概率丢失Mono脚本里序列化的数组里某个成员！
                //_steps = ESteps.CloneAsync;
                _steps = ESteps.CloneSync;
#else
                _steps = ESteps.CloneSync;
#endif
            }

            if (_steps == ESteps.CloneSync)
            {
                // 实例化游戏对象
                Result = InstantiateInternal(_handle.AssetObject, _options);
                if (_options.Actived == false)
                    Result.SetActive(false);

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }

#if UNITY_2023_3_OR_NEWER
            if (_steps == ESteps.CloneAsync)
            {
                if (_instantiateAsync == null)
                {
                    _instantiateAsync = InstantiateAsyncInternal(_handle.AssetObject, _options);
                }

                if (IsWaitingForAsyncComplete)
                    _instantiateAsync.WaitForCompletion();

                if (_instantiateAsync.isDone == false)
                    return;

                if (_instantiateAsync.Result != null && _instantiateAsync.Result.Length > 0)
                {
                    Result = _instantiateAsync.Result[0] as GameObject;
                    if (Result != null)
                    {
                        if (_options.Actived == false)
                            Result.SetActive(false);

                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeed;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Instantiate game object is null.";
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Instantiate async results is null.";
                }
            }
#endif
        }
        internal override void InternalWaitForAsyncComplete()
        {
            RunBatchExecution();
        }
        internal override string InternalGetDescription()
        {
            var assetInfo = _handle.GetAssetInfo();
            return $"AssetPath : {assetInfo.AssetPath}";
        }

        /// <summary>
        /// 取消实例化对象操作
        /// </summary>
        public void Cancel()
        {
#if UNITY_2023_3_OR_NEWER
            if (_instantiateAsync != null && _instantiateAsync.isDone == false)
                _instantiateAsync.Cancel();
#endif

            AbortOperation();
        }

        /// <summary>
        /// 同步实例化
        /// </summary>
        internal static GameObject InstantiateInternal(UnityEngine.Object assetObject, InstantiateOptions options)
        {
            if (assetObject == null)
                return null;

            if (options.SetPositionAndRotation)
            {
                if (options.Parent != null)
                    return UnityEngine.Object.Instantiate(assetObject as GameObject, options.Position, options.Rotation, options.Parent);
                else
                    return UnityEngine.Object.Instantiate(assetObject as GameObject, options.Position, options.Rotation);
            }
            else
            {
                if (options.Parent != null)
                    return UnityEngine.Object.Instantiate(assetObject as GameObject, options.Parent, options.InWorldSpace);
                else
                    return UnityEngine.Object.Instantiate(assetObject as GameObject);
            }
        }

#if UNITY_2023_3_OR_NEWER
        /// <summary>
        /// 异步实例化
        /// 注意：Unity2022.3.20f1及以上版本生效
        /// https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Object.InstantiateAsync.html
        /// </summary>
        internal static AsyncInstantiateOperation InstantiateAsyncInternal(UnityEngine.Object assetObject, InstantiateOptions options)
        {
            if (options.SetPositionAndRotation)
            {
                if (options.Parent != null)
                    return UnityEngine.Object.InstantiateAsync(assetObject as GameObject, options.Parent, options.Position, options.Rotation);
                else
                    return UnityEngine.Object.InstantiateAsync(assetObject as GameObject, options.Position, options.Rotation);
            }
            else
            {
                if (options.Parent != null)
                    return UnityEngine.Object.InstantiateAsync(assetObject as GameObject, options.Parent);
                else
                    return UnityEngine.Object.InstantiateAsync(assetObject as GameObject);
            }
        }
#endif
    }
}