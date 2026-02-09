using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 资源包加载操作
    /// </summary>
    internal class LoadBundleOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            CheckConcurrency,
            LoadBundleFile,
            Done,
        }

        private readonly ResourceManager _resourceManager;
        private readonly List<ProviderBase> _providers = new List<ProviderBase>(100);
        private readonly List<ProviderBase> _removeList = new List<ProviderBase>(100);
        private FSLoadPackageBundleOperation _loadPackageBundleOp;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 资源包文件信息
        /// </summary>
        public BundleInfo LoadBundleInfo { private set; get; }

        /// <summary>
        /// 是否已经销毁
        /// </summary>
        public bool IsDestroyed { private set; get; } = false;

        /// <summary>
        /// 引用计数
        /// </summary>
        public int RefCount { private set; get; } = 0;

        /// <summary>
        /// 资源包句柄
        /// </summary>
        public IBundleHandle BundleHandle { set; get; }

        internal LoadBundleOperation(ResourceManager resourceManager, BundleInfo bundleInfo)
        {
            _resourceManager = resourceManager;
            LoadBundleInfo = bundleInfo;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckConcurrency;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckConcurrency)
            {
                if (IsWaitForCompletion)
                {
                    _steps = ESteps.LoadBundleFile;
                }
                else
                {
                    if (_resourceManager.IsBundleLoadingBusy())
                        return;
                    _steps = ESteps.LoadBundleFile;
                }
            }

            if (_steps == ESteps.LoadBundleFile)
            {
                if (_loadPackageBundleOp == null)
                {
                    // 统计计数增加
                    _resourceManager.IncrementBundleLoadingCounter();
                    _loadPackageBundleOp = LoadBundleInfo.CreateBundleLoader();
                    _loadPackageBundleOp.StartOperation();
                    AddChildOperation(_loadPackageBundleOp);
                }

                if (IsWaitForCompletion)
                    _loadPackageBundleOp.WaitForCompletion();

                _loadPackageBundleOp.UpdateOperation();
                if (_loadPackageBundleOp.IsDone == false)
                    return;

                if (_loadPackageBundleOp.Status == EOperationStatus.Succeeded)
                {
                    if (_loadPackageBundleOp.BundleHandle == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"The bundle handle is null. Bundle: {LoadBundleInfo.Bundle.BundleName}";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        BundleHandle = _loadPackageBundleOp.BundleHandle;
                        Status = EOperationStatus.Succeeded;
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadPackageBundleOp.Error;
                }

                // 统计计数减少
                _resourceManager.DecrementBundleLoadingCounter();
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
        internal override string InternalGetDescription()
        {
            return $"BundleName: {LoadBundleInfo.Bundle.BundleName}";
        }

        /// <summary>
        /// 引用（引用计数递加）
        /// </summary>
        public void Reference()
        {
            RefCount++;
        }

        /// <summary>
        /// 释放（引用计数递减）
        /// </summary>
        public void Release()
        {
            RefCount--;
        }

        /// <summary>
        /// 销毁
        /// </summary>
        public void DestroyLoader()
        {
            IsDestroyed = true;

            // 注意：正在加载中的任务不可以销毁
            if (_steps == ESteps.LoadBundleFile)
                throw new YooInternalException($"Cannot destroy loader while loading bundle: {LoadBundleInfo.Bundle.BundleName}");

            if (RefCount > 0)
                throw new YooInternalException($"Cannot destroy loader with non-zero ref count {RefCount}: {LoadBundleInfo.Bundle.BundleName}");

            if (BundleHandle != null)
                BundleHandle.UnloadBundleFile();

            if (IsDone == false)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "Bundle loader destroyed.";
            }
        }

        /// <summary>
        /// 是否可以销毁
        /// </summary>
        public bool CanDestroyLoader()
        {
            if (IsReleasable() == false)
                return false;

            // YOOASSET_LEGACY_DEPENDENCY
            // 检查引用链上的资源包是否已经全部销毁
            // 注意：互相引用的资源包无法卸载！
            if (LoadBundleInfo.Bundle.ReferenceBundleIDs.Count > 0)
            {
                foreach (var bundleID in LoadBundleInfo.Bundle.ReferenceBundleIDs)
                {
#if YOOASSET_EXPERIMENTAL
                    if (_resourceManager.CheckBundleReleasable(bundleID) == false)
                        return false;
#else
                    if (_resourceManager.CheckBundleDestroyed(bundleID) == false)
                        return false;
#endif
                }
            }

            return true;
        }

        /// <summary>
        /// 是否可以释放
        /// </summary>
        public bool IsReleasable()
        {
            // 注意：正在加载中的任务不可以销毁
            if (_steps == ESteps.LoadBundleFile)
                return false;

            if (RefCount > 0)
                return false;

            return true;
        }

        /// <summary>
        /// 添加附属的资源提供者
        /// </summary>
        public void AddProvider(ProviderBase provider)
        {
            if (_providers.Contains(provider) == false)
                _providers.Add(provider);
        }

        /// <summary>
        /// 尝试销毁资源提供者
        /// </summary>
        public void TryDestroyProviders()
        {
            // 获取移除列表
            _removeList.Clear();
            foreach (var provider in _providers)
            {
                if (provider.CanDestroyProvider())
                {
                    _removeList.Add(provider);
                }
            }

            // 销毁资源提供者
            foreach (var provider in _removeList)
            {
                _providers.Remove(provider);
                provider.DestroyProvider();
            }

            // 移除资源提供者
            if (_removeList.Count > 0)
            {
                _resourceManager.RemoveBundleProviders(_removeList);
                _removeList.Clear();
            }
        }

        /// <summary>
        /// 尝试终止加载器
        /// </summary>
        public void TryAbortLoader()
        {
            if (IsDone == false)
            {
                if (_steps == ESteps.CheckConcurrency)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Bundle loader aborted.";
                }

                if (_steps == ESteps.LoadBundleFile)
                {
                    // 注意：终止下载器
                    if (_loadPackageBundleOp != null)
                        _loadPackageBundleOp.AbortDownloadFile = true;
                }
            }
        }
    }
}