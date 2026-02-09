using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YooAsset
{
    /// <summary>
    /// 场景提供者，负责场景资源的加载
    /// </summary>
    internal sealed class SceneProvider : ProviderBase
    {
        private readonly LoadSceneParameters _loadSceneParams;
        private bool _suspendLoad;
        private BHLoadSceneOperation _loadSceneOp;

        public SceneProvider(ResourceManager manager, string providerGUID, AssetInfo assetInfo, LoadSceneParameters loadSceneParams, bool suspendLoad) : base(manager, providerGUID, assetInfo)
        {
            _loadSceneParams = loadSceneParams;
            _suspendLoad = suspendLoad;
            LoadedSceneName = Path.GetFileNameWithoutExtension(assetInfo.AssetPath);
        }
        protected override void ProcessBundleHandle()
        {
            if (_loadSceneOp == null)
            {
                _loadSceneOp = LoadedBundleHandle.LoadSceneAsync(MainAssetInfo, _loadSceneParams, _suspendLoad);
                _loadSceneOp.StartOperation();
                AddChildOperation(_loadSceneOp);
            }

            if (IsWaitForCompletion)
                _loadSceneOp.WaitForCompletion();

            // 注意：场景加载中途可以取消挂起
            if (_suspendLoad == false)
                _loadSceneOp.ResumeLoad();

            _loadSceneOp.UpdateOperation();
            Progress = _loadSceneOp.Progress;
            if (_loadSceneOp.IsDone == false)
                return;

            if (_loadSceneOp.Status != EOperationStatus.Succeeded)
            {
                InvokeCompletion(_loadSceneOp.Error, EOperationStatus.Failed);
            }
            else
            {
                SceneObject = _loadSceneOp.Result;
                InvokeCompletion(string.Empty, EOperationStatus.Succeeded);
            }
        }

        /// <summary>
        /// 解除场景加载挂起操作
        /// </summary>
        public void UnSuspendLoad()
        {
            _suspendLoad = false;
        }
    }
}