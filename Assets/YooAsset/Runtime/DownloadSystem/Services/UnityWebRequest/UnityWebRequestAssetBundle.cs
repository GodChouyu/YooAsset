using System;
using UnityEngine;
using UnityEngine.Networking;

namespace YooAsset
{
    /// <summary>
    /// UnityWebRequest AssetBundle 下载器
    /// </summary>
    /// <remarks>
    /// 下载并加载 Unity AssetBundle 资源包。
    /// 支持 Unity 内置缓存机制和 CRC 校验。
    /// </remarks>
    internal sealed class UnityWebRequestAssetBundle : UnityWebRequestBase, IDownloadAssetBundleRequest
    {
        /// <summary>
        /// AssetBundle 下载参数
        /// </summary>
        private readonly DownloadAssetBundleRequestArgs _args;

        /// <summary>
        /// AssetBundle 下载处理器
        /// </summary>
        private DownloadHandlerAssetBundle _downloadHandler;

        /// <summary>
        /// 下载结果（AssetBundle 对象）
        /// </summary>
        public AssetBundle Result { get; private set; }

        /// <summary>
        /// 构造 AssetBundle 下载器
        /// </summary>
        /// <param name="args">AssetBundle 下载参数</param>
        /// <param name="webRequestCreator">UnityWebRequest 创建器（可选）</param>
        public UnityWebRequestAssetBundle(DownloadAssetBundleRequestArgs args, UnityWebRequestCreator webRequestCreator)
            : base(args.Url, webRequestCreator)
        {
            _args = args;
        }

        /// <summary>
        /// 创建 UnityWebRequest
        /// </summary>
        protected override void CreateWebRequest()
        {
            _downloadHandler = CreateAssetBundleDownloadHandler();
            _webRequest = CreateGetWebRequest(Url);
            _webRequest.downloadHandler = _downloadHandler;
            _webRequest.disposeDownloadHandlerOnDispose = true;
            ConfigureRequest(_args.Timeout, _args.WatchdogTimeout, _args.Headers);
        }

        /// <summary>
        /// 请求成功时的回调
        /// </summary>
        protected override void OnRequestSucceeded()
        {
            AssetBundle assetBundle = _downloadHandler.assetBundle;
            if (assetBundle == null)
            {
                Status = EDownloadRequestStatus.Failed;
                Error = $"[{GetType().Name}] Failed to load AssetBundle. URL: {Url}, Error: AssetBundle object is null";
            }
            else
            {
                Result = assetBundle;
            }
        }

        /// <summary>
        /// 创建 AssetBundle 下载处理器
        /// </summary>
        /// <remarks>
        /// 根据 DisableUnityWebCache 配置决定是否使用 Unity 内置缓存。
        /// 启用缓存时需要提供有效的 FileHash。
        /// </remarks>
        private DownloadHandlerAssetBundle CreateAssetBundleDownloadHandler()
        {
            DownloadHandlerAssetBundle handler;

            if (_args.DisableUnityWebCache)
            {
                // 禁用 Unity 缓存
                handler = new DownloadHandlerAssetBundle(Url, _args.UnityCrc);
            }
            else
            {
                if (string.IsNullOrEmpty(_args.FileHash))
                    throw new YooInternalException("FileHash is required when Unity web cache is enabled (DisableUnityWebCache = false).");

                // 使用 Unity 缓存
                // 说明：The file hash defining the version of the asset bundle.
                Hash128 fileHash = Hash128.Parse(_args.FileHash);
                handler = new DownloadHandlerAssetBundle(Url, fileHash, _args.UnityCrc);
            }

            return handler;
        }
    }
}
