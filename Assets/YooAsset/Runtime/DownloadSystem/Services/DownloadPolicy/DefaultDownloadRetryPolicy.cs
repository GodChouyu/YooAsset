using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 默认的下载重试策略
    /// </summary>
    public class DefaultDownloadRetryPolicy : IDownloadRetryPolicy
    {
        private const float BaseDelay = 1f;
        private const float MaxDelay = 10f;

        /// <summary>
        /// 判断本次下载失败是否属于可重试的错误
        /// </summary>
        public bool IsRetryableError(string url, long httpCode, string httpError)
        {
            // HTTP 状态码
            // 1xx 信息响应
            // 2xx 成功响应
            // 3xx 重定向消息
            // 4xx 客户端错误响应
            // 5xx 服务器错误响应

            // 本地协议/本地路径不可重试
            if (DownloadSystemTools.IsLocalFileUrl(url))
                return false;

            // 网络瞬断可重试
            if (httpCode == 0)
                return true;

            // 4xx 客户端错误不可重试
            // 例外：408 Request Timeout
            // 例外：416 Range Not Satisfiable
            // 例外：429 Too Many Requests
            if (httpCode >= 400 && httpCode < 500)
                return httpCode == 408 || httpCode == 416 || httpCode == 429;

            // 其它情况可重试
            return true;
        }

        /// <summary>
        /// 计算本次重试应等待的时长（秒）
        /// 线性退避：每次在上一次基础上 +1 秒，封顶到最大等待秒数。
        /// </summary>
        public float ComputeDelay(int retryCount, float previousDelay)
        {
            return Mathf.Clamp(previousDelay + 1f, BaseDelay, MaxDelay);
        }
    }
}
