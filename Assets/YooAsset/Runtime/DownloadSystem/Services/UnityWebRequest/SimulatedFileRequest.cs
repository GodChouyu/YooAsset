using System;

namespace YooAsset
{
    /// <summary>
    /// 模拟下载器
    /// </summary>
    /// <remarks>
    /// 用于编辑器模式下模拟下载进度，不进行实际网络请求。
    /// 根据配置的下载速度模拟进度变化。
    /// </remarks>
    internal sealed class SimulatedFileRequest : IDownloadFileRequest
    {
        /// <summary>
        /// 模拟下载参数
        /// </summary>
        private readonly SimulateDownloadRequestArgs _args;

        /// <summary>
        /// 最近一次更新的时间戳（用于计算时间增量）
        /// </summary>
        private double _lastestUpdateTime;

        /// <summary>
        /// 文件保存路径（模拟下载不需要）
        /// </summary>
        public string SavePath
        {
            get { return null; }
        }

        #region 接口实现
        /// <summary>
        /// 请求地址
        /// </summary>
        public string Url { get; }

        /// <summary>
        /// 是否完成
        /// </summary>
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
        public EDownloadRequestStatus Status { get; private set; }

        /// <summary>
        /// 当前下载进度（0f - 1f）
        /// </summary>
        public float DownloadProgress { get; private set; }

        /// <summary>
        /// 当前请求已接收的字节数
        /// </summary>
        public long DownloadedBytes { get; private set; }

        /// <summary>
        /// HTTP 返回码（模拟固定返回 200）
        /// </summary>
        public long HttpCode { get; private set; }

        /// <summary>
        /// HTTP 错误信息
        /// </summary>
        public string HttpError { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; private set; }
        #endregion

        /// <summary>
        /// 构造模拟下载器
        /// </summary>
        /// <param name="args">模拟下载参数</param>
        public SimulatedFileRequest(SimulateDownloadRequestArgs args)
        {
            _args = args;
            Url = args.Url;
            Status = EDownloadRequestStatus.None;
        }

        /// <summary>
        /// 发起请求
        /// </summary>
        public void SendRequest()
        {
            if (Status == EDownloadRequestStatus.None)
            {
                Status = EDownloadRequestStatus.Running;
                _lastestUpdateTime = TimeUtility.RealtimeSinceStartup;
            }
        }

        /// <summary>
        /// 中止请求
        /// </summary>
        public void AbortRequest()
        {
            if (Status == EDownloadRequestStatus.None || Status == EDownloadRequestStatus.Running)
            {
                Status = EDownloadRequestStatus.Aborted;
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// 更新网络请求
        /// </summary>
        private void PollRequest()
        {
            if (Status != EDownloadRequestStatus.Running)
                return;

            double currentTime = TimeUtility.RealtimeSinceStartup;
            double deltaTime = currentTime - _lastestUpdateTime;
            _lastestUpdateTime = currentTime;

            // 计算本帧下载的字节数
            long downloadBytes = (long)(_args.DownloadSpeed * deltaTime);
            DownloadedBytes += downloadBytes;

            if (_args.FileSize > 0)
                DownloadProgress = (float)DownloadedBytes / _args.FileSize;

            // 检查是否完成
            if (DownloadedBytes >= _args.FileSize)
            {
                HttpCode = 200;
                DownloadProgress = 1f;
                DownloadedBytes = _args.FileSize;
                Status = EDownloadRequestStatus.Succeeded;
            }
        }
    }
}
