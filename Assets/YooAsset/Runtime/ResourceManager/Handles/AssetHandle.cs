using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 资源句柄，用于管理单个资源对象的加载和访问
    /// </summary>
    public sealed class AssetHandle : HandleBase
    {
        private System.Action<AssetHandle> _callback;

        internal AssetHandle(ProviderBase provider) : base(provider)
        {
        }
        internal override void InvokeCallback()
        {
            _callback?.Invoke(this);
        }

        /// <summary>
        /// 完成委托
        /// </summary>
        public event System.Action<AssetHandle> Completed
        {
            add
            {
                if (IsValidWithWarning == false)
                    throw new YooHandleException($"{nameof(AssetHandle)} is invalid. It may have been released or the provider was destroyed.");
                if (Provider.IsDone)
                    value.Invoke(this);
                else
                    _callback += value;
            }
            remove
            {
                if (IsValidWithWarning == false)
                    throw new YooHandleException($"{nameof(AssetHandle)} is invalid. It may have been released or the provider was destroyed.");
                _callback -= value;
            }
        }

        /// <summary>
        /// 等待异步执行完毕
        /// </summary>
        public void WaitForAsyncComplete()
        {
            if (IsValidWithWarning == false)
                return;
            Provider.WaitForCompletion();
        }


        /// <summary>
        /// 资源对象
        /// </summary>
        public UnityEngine.Object AssetObject
        {
            get
            {
                if (IsValidWithWarning == false)
                    return null;
                return Provider.AssetObject;
            }
        }

        /// <summary>
        /// 获取资源对象
        /// </summary>
        /// <typeparam name="TAsset">资源类型</typeparam>
        public TAsset GetAssetObject<TAsset>() where TAsset : UnityEngine.Object
        {
            if (IsValidWithWarning == false)
                return null;
            return Provider.AssetObject as TAsset;
        }

        /// <summary>
        /// 同步初始化游戏对象
        /// </summary>
        public GameObject InstantiateSync()
        {
            var options = new InstantiateOptions(true);
            return InstantiateSyncInternal(options);
        }

        /// <summary>
        /// 同步初始化游戏对象
        /// </summary>
        public GameObject InstantiateSync(InstantiateOptions options)
        {
            return InstantiateSyncInternal(options);
        }

        /// <summary>
        /// 异步初始化游戏对象
        /// </summary>
        public InstantiateOperation InstantiateAsync()
        {
            var options = new InstantiateOptions(true);
            return InstantiateAsyncInternal(options);
        }

        /// <summary>
        /// 异步初始化游戏对象
        /// </summary>
        public InstantiateOperation InstantiateAsync(InstantiateOptions options)
        {
            return InstantiateAsyncInternal(options);
        }

        private GameObject InstantiateSyncInternal(InstantiateOptions options)
        {
            if (IsValidWithWarning == false)
                return null;
            if (Provider.AssetObject == null)
                return null;

            return InstantiateOperation.InstantiateInternal(Provider.AssetObject, options);
        }
        private InstantiateOperation InstantiateAsyncInternal(InstantiateOptions options)
        {
            string packageName = GetAssetInfo().PackageName;
            InstantiateOperation operation = new InstantiateOperation(this, options);
            AsyncOperationSystem.StartOperation(packageName, operation);
            return operation;
        }
    }
}
