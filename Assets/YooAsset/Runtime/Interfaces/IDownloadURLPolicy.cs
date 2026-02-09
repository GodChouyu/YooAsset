using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// URL 选择策略接口
    /// </summary>
    public interface IDownloadURLPolicy
    {
        /// <summary>
        /// 基于内部状态从候选列表中选择本次请求应使用的 URL。
        /// </summary>
        /// <param name="candidateURLs">候选 URL 列表（至少包含一个）</param>
        /// <returns>选中的 URL</returns>
        string SelectURL(IReadOnlyList<string> candidateURLs);

        /// <summary>
        /// 反馈请求成功，策略可据此更新内部状态。
        /// </summary>
        /// <param name="url">实际使用的 URL</param>
        void OnSuccess(string url);

        /// <summary>
        /// 反馈请求失败，策略可据此更新内部状态。
        /// </summary>
        /// <param name="url">实际使用的 URL</param>
        /// <param name="httpCode">HTTP 状态码（0 表示网络中断或非 HTTP 错误）</param>
        /// <param name="httpError">HTTP 错误信息</param>
        void OnFailure(string url, long httpCode, string httpError);
    }
}
