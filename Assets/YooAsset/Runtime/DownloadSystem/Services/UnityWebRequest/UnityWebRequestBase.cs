using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace YooAsset
{
    /// <summary>
    /// UnityWebRequest 下载器基类
    /// </summary>
    /// <remarks>
    /// 封装 UnityWebRequest 的通用下载逻辑，包括状态管理、进度追踪等。
    /// 子类只需实现 CreateWebRequest 方法来创建特定类型的下载请求。
    /// </remarks>
    internal abstract class UnityWebRequestBase : IDownloadRequest
    {
        /// <summary>
        /// 自定义 UnityWebRequest 创建器
        /// </summary>
        private readonly UnityWebRequestCreator _webRequestCreator;

        /// <summary>
        /// UnityWebRequest 实例
        /// </summary>
        protected UnityWebRequest _webRequest;

        /// <summary>
        /// 看门狗超时时间（秒）
        /// </summary>
        private int _watchdogTimeout = 0;

        /// <summary>
        /// 是否已被看门狗中止
        /// </summary>
        private bool _watchdogAborted = false;

        /// <summary>
        /// 最近一次记录的下载字节数
        /// </summary>
        private long _lastestDownloadBytes = -1;

        /// <summary>
        /// 最近一次接收数据的时间
        /// </summary>
        private double _lastestDataReceivedTime;

        #region 接口实现
        /// <summary>
        /// 请求地址
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// 是否完成
        /// </summary>
        /// <remarks>
        /// 每次访问此属性都会自动调用内部方法 UpdateRequest() 进行状态更新。
        /// </remarks>
        public bool IsDone
        {
            get
            {
                PollRequest();
                return Status == EDownloadRequestStatus.Succeeded
                    || Status == EDownloadRequestStatus.Failed
                    || Status == EDownloadRequestStatus.Aborted;
            }
        }

        /// <summary>
        /// 请求状态
        /// </summary>
        public EDownloadRequestStatus Status { get; protected set; }

        /// <summary>
        /// 当前下载进度（0f - 1f）
        /// </summary>
        public float DownloadProgress { get; private set; }

        /// <summary>
        /// 当前请求已接收的字节数
        /// </summary>
        public long DownloadedBytes { get; private set; }

        /// <summary>
        /// HTTP 返回码
        /// </summary>
        public long HttpCode { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; protected set; }
        #endregion

        /// <summary>
        /// 构造下载器基类
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="webRequestCreator">UnityWebRequest 创建器（可选）</param>
        protected UnityWebRequestBase(string url, UnityWebRequestCreator webRequestCreator)
        {
            Url = url;
            _webRequestCreator = webRequestCreator;
            Status = EDownloadRequestStatus.None;
        }

        /// <summary>
        /// 发起请求
        /// </summary>
        /// <remarks>
        /// 仅在 Status 为 None 时生效，重复调用无效。
        /// 调用后 Status 变为 Running。
        /// </remarks>
        public void SendRequest()
        {
            if (Status == EDownloadRequestStatus.None)
            {
                Status = EDownloadRequestStatus.Running;

                try
                {
                    CreateWebRequest();

                    if (_webRequest == null)
                    {
                        Status = EDownloadRequestStatus.Failed;
                        Error = $"[{GetType().Name}] CreateWebRequest() returned null";
                    }
                    else
                    {
                        _webRequest.SendWebRequest();
                    }
                }
                catch (Exception ex)
                {
                    Status = EDownloadRequestStatus.Failed;
                    Error = $"[{GetType().Name}] Failed to create web request: {ex.Message}";
                }
            }
        }

        /// <summary>
        /// 中止请求
        /// </summary>
        /// <remarks>
        /// 可在任意状态调用，仅当 Status 为 None 或 Running 时生效。
        /// 调用后 Status 变为 Aborted。
        /// </remarks>
        public void AbortRequest()
        {
            if (Status == EDownloadRequestStatus.None || Status == EDownloadRequestStatus.Running)
            {
                Status = EDownloadRequestStatus.Aborted;
                if (_webRequest != null)
                    _webRequest.Abort();
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            CleanupWebRequest();
        }


        /// <summary>
        /// 创建 UnityWebRequest（子类实现）
        /// </summary>
        protected abstract void CreateWebRequest();

        /// <summary>
        /// 请求成功时的回调（子类可重写）
        /// </summary>
        protected virtual void OnRequestSucceeded()
        {
        }

        /// <summary>
        /// 请求失败时的回调（子类可重写）
        /// </summary>
        protected virtual void OnRequestFailed()
        {
        }


        /// <summary>
        /// 创建 UnityWebRequest GET 请求
        /// </summary>
        /// <param name="requestUrl">请求地址</param>
        /// <returns>UnityWebRequest 实例</returns>
        protected UnityWebRequest CreateGetWebRequest(string requestUrl)
        {
            if (_webRequestCreator != null)
                return _webRequestCreator.Invoke(requestUrl, UnityWebRequest.kHttpVerbGET);

            return new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbGET);
        }

        /// <summary>
        /// 创建 UnityWebRequest HEAD 请求
        /// </summary>
        /// <param name="requestUrl">请求地址</param>
        /// <returns>UnityWebRequest 实例</returns>
        protected UnityWebRequest CreateHeadWebRequest(string requestUrl)
        {
            if (_webRequestCreator != null)
                return _webRequestCreator.Invoke(requestUrl, UnityWebRequest.kHttpVerbHEAD);

            return new UnityWebRequest(requestUrl, UnityWebRequest.kHttpVerbHEAD);
        }

        /// <summary>
        /// 配置通用请求参数
        /// </summary>
        /// <param name="timeout">响应超时时间（秒），0 表示不应用超时</param>
        /// <param name="watchdogTimeout">看门狗超时时间（秒），0 表示禁用</param>
        /// <param name="headers">自定义请求头（可选）</param>
        protected void ConfigureRequest(int timeout, int watchdogTimeout, Dictionary<string, string> headers)
        {
            if (_webRequest == null)
                throw new YooInternalException("Cannot configure request: UnityWebRequest object is null. Ensure CreateWebRequest() is called first.");

            // 设置看门狗超时时间
            _watchdogTimeout = watchdogTimeout;

            // 设置响应的超时时间
            if (timeout > 0)
                _webRequest.timeout = timeout;

            // 设置请求头
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    _webRequest.SetRequestHeader(header.Key, header.Value);
                }
            }
        }

        /// <summary>
        /// 更新网络请求
        /// </summary>
        private void PollRequest()
        {
            if (Status != EDownloadRequestStatus.Running)
                return;

            DownloadProgress = _webRequest.downloadProgress;
            DownloadedBytes = (long)_webRequest.downloadedBytes;

            UpdateWatchdog();
            if (_webRequest.isDone == false)
                return;

            HttpCode = _webRequest.responseCode;
#if UNITY_2020_3_OR_NEWER
            bool isSuccess = _webRequest.result == UnityWebRequest.Result.Success;
#else
            bool isSuccess = !_webRequest.isNetworkError && !_webRequest.isHttpError;
#endif

            if (isSuccess)
            {
                Status = EDownloadRequestStatus.Succeeded;
                OnRequestSucceeded();
            }
            else
            {
                Status = EDownloadRequestStatus.Failed;
                Error = $"[{GetType().Name}] Request failed. URL: {Url}, Error: {_webRequest.error}";
                OnRequestFailed();
            }

            // 完成后释放
            CleanupWebRequest();
        }

        /// <summary>
        /// 更新看门狗机制
        /// </summary>
        private void UpdateWatchdog()
        {
            if (_watchdogTimeout == 0)
                return;
            if (_watchdogAborted)
                return;

            double realtimeSinceStartup = TimeUtility.RealtimeSinceStartup;
            if (DownloadedBytes != _lastestDownloadBytes)
            {
                _lastestDownloadBytes = DownloadedBytes;
                _lastestDataReceivedTime = realtimeSinceStartup;
            }
            else
            {
                double deltaTime = realtimeSinceStartup - _lastestDataReceivedTime;
                if (deltaTime > _watchdogTimeout)
                {
                    _watchdogAborted = true;
                    AbortRequest(); //看门狗终止网络请求
                }
            }
        }

        /// <summary>
        /// 清理 WebRequest 资源
        /// </summary>
        private void CleanupWebRequest()
        {
            if (_webRequest != null)
            {
                //注意：引擎底层会自动调用Abort方法
                _webRequest.Dispose();
                _webRequest = null;
            }
        }
    }
}
