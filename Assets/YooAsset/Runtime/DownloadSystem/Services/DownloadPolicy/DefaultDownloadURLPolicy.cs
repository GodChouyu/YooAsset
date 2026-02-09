using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 默认的 URL 选择策略
    /// </summary>
    public class DefaultDownloadURLPolicy : IDownloadURLPolicy
    {
        private int _failureCount = 0;

        /// <summary>
        /// 基于内部失败计数轮转选择 URL
        /// </summary>
        public string SelectURL(IReadOnlyList<string> candidateURLs)
        {
            int index = _failureCount % candidateURLs.Count;
            return candidateURLs[index];
        }

        /// <summary>
        /// 请求成功反馈（默认策略不做处理）
        /// </summary>
        public void OnSuccess(string url)
        {
        }

        /// <summary>
        /// 请求失败反馈，递增失败计数以切换 URL
        /// </summary>
        public void OnFailure(string url, long httpCode, string httpError)
        {
            _failureCount++;
        }
    }
}
