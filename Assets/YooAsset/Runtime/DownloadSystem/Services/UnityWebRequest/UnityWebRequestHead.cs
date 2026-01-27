using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace YooAsset
{
    /// <summary>
    /// UnityWebRequest HEAD 请求下载器
    /// </summary>
    /// <remarks>
    /// 仅获取响应头信息，不下载实际内容。
    /// 用于检查资源是否存在、获取资源大小、检查缓存有效性等场景。
    /// </remarks>
    internal sealed class UnityWebRequestHead : UnityWebRequestBase, IDownloadHeadRequest
    {
        /// <summary>
        /// 缓存的响应头（请求完成后从 WebRequest 复制）
        /// </summary>
        /// <remarks>
        /// WebRequest 释放后无法获取响应头，因此需要提前缓存。
        /// </remarks>
        private Dictionary<string, string> _cachedResponseHeaders;

        /// <summary>
        /// 数据下载参数
        /// </summary>
        private readonly DownloadDataRequestArgs _args;

        /// <summary>
        /// 获取 ETag 响应头
        /// </summary>
        public string ETag
        {
            get { return GetResponseHeader("ETag"); }
        }

        /// <summary>
        /// 获取 Last-Modified 响应头
        /// </summary>
        public string LastModified
        {
            get { return GetResponseHeader("Last-Modified"); }
        }

        /// <summary>
        /// 获取 Content-Type 响应头
        /// </summary>
        public string ContentType
        {
            get { return GetResponseHeader("Content-Type"); }
        }

        /// <summary>
        /// 获取 Content-Length 响应头
        /// 预期下载的总字节数
        /// </summary>
        public long ContentLength
        {
            get
            {
                string contentLengthStr = GetResponseHeader("Content-Length");
                if (string.IsNullOrEmpty(contentLengthStr))
                    return -1;

                if (long.TryParse(contentLengthStr, out long contentLength))
                {
                    return contentLength;
                }
                else
                {
                    return -1;
                }
            }
        }

        /// <summary>
        /// 构造 HEAD 请求下载器
        /// </summary>
        /// <param name="args">数据下载参数</param>
        /// <param name="webRequestCreator">UnityWebRequest 创建器（可选）</param>
        public UnityWebRequestHead(DownloadDataRequestArgs args, UnityWebRequestCreator webRequestCreator)
                            : base(args.Url, webRequestCreator)
        {
            _args = args;
        }

        /// <summary>
        /// 获取响应头信息
        /// </summary>
        /// <param name="name">响应头名称（不区分大小写）</param>
        /// <returns>响应头的值，如果不存在或请求未完成则返回 null</returns>
        public string GetResponseHeader(string name)
        {
            if (_cachedResponseHeaders == null)
                return null;

            // 注意：UnityWebRequest 的响应头 key 是小写的
            string lowerName = name.ToLowerInvariant();
            if (_cachedResponseHeaders.TryGetValue(lowerName, out string value))
                return value;

            return null;
        }

        /// <summary>
        /// 创建 UnityWebRequest
        /// </summary>
        protected override void CreateWebRequest()
        {
            _webRequest = CreateHeadWebRequest(Url);
            _webRequest.downloadHandler = null; // HEAD 请求不需要 DownloadHandler
            ConfigureRequest(_args.Timeout, _args.WatchdogTimeout, _args.Headers);
        }

        /// <summary>
        /// 请求成功时的回调
        /// </summary>
        protected override void OnRequestSucceeded()
        {
            var headers = _webRequest.GetResponseHeaders();
            if (headers != null)
            {
                _cachedResponseHeaders = new Dictionary<string, string>(headers.Count, StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in headers)
                {
                    string name = kvp.Key.ToLowerInvariant();
                    string value = kvp.Value;
                    _cachedResponseHeaders[name] = value;
                }
            }
        }
    }
}
