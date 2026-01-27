using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 卸载未使用资源的异步操作
    /// </summary>
    public sealed class UnloadUnusedAssetsOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            UnloadUnused,
            Done,
        }

        private readonly ResourceManager _resourceManager;
        private readonly UnloadUnusedAssetsOptions _options;
        private int _loopCounter = 0;
        private ESteps _steps = ESteps.None;

        internal UnloadUnusedAssetsOperation(ResourceManager resourceManager, UnloadUnusedAssetsOptions options)
        {
            _resourceManager = resourceManager;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.UnloadUnused;
            _loopCounter = _options.LoopCount;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.UnloadUnused)
            {
                while (_loopCounter > 0)
                {
                    _loopCounter--;
                    LoopUnloadUnused();

                    if (IsBusy)
                        break;
                }

                if (_loopCounter <= 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }
        }
        internal override void InternalWaitForCompletion()
        {
            ExecuteBatch();
        }
        internal override string InternalGetDescription()
        {
            return $"LoopCount : {_options.LoopCount}";
        }

        /// <summary>
        /// 说明：资源包之间会有深层的依赖链表，需要多次迭代才可以在单帧内卸载！
        /// </summary>
        private void LoopUnloadUnused()
        {
            var removeList = new List<LoadBundleOperation>(_resourceManager.BundleLoaderDict.Count);

            // 注意：优先销毁资源提供者
            foreach (var loader in _resourceManager.BundleLoaderDict.Values)
            {
                loader.TryDestroyProviders();
            }

            // 获取销毁列表
            foreach (var loader in _resourceManager.BundleLoaderDict.Values)
            {
                if (loader.CanDestroyLoader())
                {
                    removeList.Add(loader);
                }
            }

            // 销毁文件加载器
            foreach (var loader in removeList)
            {
                string bundleName = loader.LoadBundleInfo.Bundle.BundleName;
                loader.DestroyLoader();
                _resourceManager.BundleLoaderDict.Remove(bundleName);
            }
        }
    }
}