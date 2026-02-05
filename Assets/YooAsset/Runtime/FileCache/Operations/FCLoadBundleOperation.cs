
namespace YooAsset
{
    /// <summary>
    /// 加载资源包操作基类
    /// </summary>
    internal abstract class FCLoadBundleOperation : AsyncOperationBase
    {
        protected struct LoadResult
        {
            /// <summary>
            /// 错误信息
            /// </summary>
            public readonly string Error;

            /// <summary>
            /// 加载成功
            /// </summary>
            public bool Succeeded
            {
                get { return Error == null; }
            }

            public LoadResult(string error)
            {
                Error = error;
            }

            public static LoadResult Default()
            {
                return new LoadResult(null);
            }
            public static LoadResult Failure(string error)
            {
                return new LoadResult(error);
            }
        }

        /// <summary>
        /// 资源包加载结果
        /// </summary>
        public IBundleResult BundleResult { get; protected set; }

        /// <summary>
        /// 检查文件路径是否支持 FileIO 读取
        /// </summary>
        protected bool SupportsFileIO(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;
            if (filePath.StartsWith("jar:") || filePath.StartsWith("content:"))
                return false;
            return true;
        }

        /// <summary>
        /// 判断是否为可重试的错误
        /// </summary>
        protected bool IsRetryableError(long httpCode)
        {
            // HTTP 状态码
            // 1xx 信息响应
            // 2xx 成功响应
            // 3xx 重定向消息
            // 4xx 客户端错误响应
            // 5xx 服务器错误响应

            if (httpCode == 0)
                return true;

            // 4xx 客户端错误不可重试
            // 说明：408 Request Timeout
            // 说明：429 Too Many Requests
            if (httpCode >= 400 && httpCode < 500)
                return httpCode == 408 || httpCode == 429;

            // 其它情况可重试
            return true;
        }
    }

    /// <summary>
    /// 加载资源包失败操作
    /// </summary>
    internal sealed class FCLoadBundleErrorOperation : FCLoadBundleOperation
    {
        private readonly string _error;

        internal FCLoadBundleErrorOperation(string error)
        {
            _error = error;
        }
        internal override void InternalStart()
        {
            Status = EOperationStatus.Failed;
            Error = _error;
        }
        internal override void InternalUpdate()
        {
        }
    }
}