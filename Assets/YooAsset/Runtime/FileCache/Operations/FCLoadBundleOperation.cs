
namespace YooAsset
{
    /// <summary>
    /// 加载资源包操作基类
    /// </summary>
    internal abstract class FCLoadBundleOperation : AsyncOperationBase
    {
        protected readonly struct LoadResult
        {
            /// <summary>
            /// 错误信息
            /// </summary>
            public readonly string Error;

            /// <summary>
            /// 是否成功
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
        /// 资源包句柄
        /// </summary>
        public IBundleHandle BundleHandle { get; protected set; }
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