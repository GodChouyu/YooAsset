using System;
using UnityEngine.Networking;

namespace YooAsset
{
    /// <summary>
    /// UnityWebRequest 字节下载器
    /// </summary>
    /// <remarks>
    /// 将下载内容保存到内存中的字节数组。
    /// </remarks>
    internal sealed class UnityWebRequestBytes : UnityWebRequestBase, IDownloadBytesRequest
    {
        private readonly DownloadDataRequestArgs _args;

        /// <summary>
        /// 下载结果（字节数组）
        /// </summary>
        public byte[] Result { get; private set; }

        /// <summary>
        /// 构造字节数组下载器
        /// </summary>
        /// <param name="args">数据下载参数</param>
        /// <param name="webRequestCreator">UnityWebRequest 创建器（可选）</param>
        public UnityWebRequestBytes(DownloadDataRequestArgs args, UnityWebRequestCreator webRequestCreator)
            : base(args.URL, webRequestCreator)
        {
            _args = args;
        }

        /// <summary>
        /// 创建 UnityWebRequest
        /// </summary>
        protected override void CreateWebRequest()
        {
            var handler = new DownloadHandlerBuffer();
            _webRequest = CreateGetRequest(URL);
            _webRequest.downloadHandler = handler;
            _webRequest.disposeDownloadHandlerOnDispose = true;
            ConfigureRequest(_args.Timeout, _args.WatchdogTimeout, _args.Headers);
        }

        /// <summary>
        /// 请求成功时的回调
        /// </summary>
        protected override void OnRequestSucceed()
        {
            Result = _webRequest.downloadHandler.data;
        }
    }
}
