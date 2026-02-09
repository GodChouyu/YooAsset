using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 下载重试控制器
    /// </summary>
    internal sealed class DownloadRetry
    {
        private readonly int _maxRetryCount;
        private readonly IDownloadRetryPolicy _retryPolicy;
        private int _retryCount;
        private float _waitTimer;
        private float _waitDuration;

        /// <summary>
        /// 已重试次数
        /// </summary>
        public int RetryCount => _retryCount;

        /// <summary>
        /// 当前等待目标时长（秒）
        /// </summary>
        public float WaitDuration => _waitDuration;

        /// <summary>
        /// 创建下载重试控制器
        /// </summary>
        /// <param name="maxRetryCount">最大重试次数</param>
        /// <param name="retryPolicy">重试策略</param>
        public DownloadRetry(int maxRetryCount, IDownloadRetryPolicy retryPolicy)
        {
            _maxRetryCount = maxRetryCount;
            _retryPolicy = retryPolicy;
            _retryCount = 0;
            _waitTimer = 0f;
            _waitDuration = 0f;
        }

        /// <summary>
        /// 判断本次失败是否允许重试
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="httpCode">HTTP 状态码</param>
        /// <param name="httpError">HTTP 错误信息</param>
        /// <returns>
        /// 返回 true 表示允许重试；调用方应紧接着调用 BeginWait() 启动等待。
        /// 返回 false 表示不允许重试（达到次数上限或错误不可重试）。
        /// </returns>
        public bool CanRetry(string url, long httpCode, string httpError)
        {
            if (_retryCount >= _maxRetryCount)
                return false;

            if (_retryPolicy.IsRetryableError(url, httpCode, httpError) == false)
                return false;

            YooLogger.Warning($"Download failed: {url}. HttpCode={httpCode}");
            return true;
        }

        /// <summary>
        /// 判断是否可以进入网络重试
        /// </summary>
        public bool CanRetry()
        {
            if (_retryCount >= _maxRetryCount)
                return false;

            return true;
        }

        /// <summary>
        /// 开始本次重试等待
        /// </summary>
        public void BeginWait()
        {
            _waitTimer = 0f;
            _retryCount++;
            _waitDuration = _retryPolicy.ComputeDelay(_retryCount, _waitDuration);
            YooLogger.Warning($"Download retrying in {WaitDuration:F1}s.");
        }

        /// <summary>
        /// 推进等待计时
        /// </summary>
        public bool Tick()
        {
            _waitTimer += Time.unscaledDeltaTime;
            return _waitTimer >= _waitDuration;
        }
    }
}
