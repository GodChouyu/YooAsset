
namespace YooAsset
{
    /// <summary>
    /// 写入缓存操作基类
    /// </summary>
    internal abstract class FCWriteCacheOperation : AsyncOperationBase
    {
    }

    /// <summary>
    /// 写入缓存完成操作
    /// </summary>
    internal class FCWriteCacheCompleteOperation : FCWriteCacheOperation
    {
        private readonly string _error;

        public FCWriteCacheCompleteOperation()
        {
            _error = null;
        }
        public FCWriteCacheCompleteOperation(string error)
        {
            _error = error;
        }
        internal override void InternalStart()
        {
            if (string.IsNullOrEmpty(_error))
            {
                Status = EOperationStatus.Succeeded;
            }
            else
            {
                Status = EOperationStatus.Failed;
                Error = _error;
            }
        }
        internal override void InternalUpdate()
        {
        }
    }
}
