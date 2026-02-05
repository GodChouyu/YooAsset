
namespace YooAsset
{
    /// <summary>
    /// 清理缓存操作基类
    /// </summary>
    internal abstract class FCClearCacheOperation : AsyncOperationBase
    {
    }

    /// <summary>
    /// 清理缓存完成操作
    /// </summary>
    internal class FCClearCacheCompleteOperation : FCClearCacheOperation
    {
        private readonly string _error;

        public FCClearCacheCompleteOperation()
        {
            _error = null;
        }
        public FCClearCacheCompleteOperation(string error)
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
