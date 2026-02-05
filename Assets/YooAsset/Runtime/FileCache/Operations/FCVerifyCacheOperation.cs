
namespace YooAsset
{
    /// <summary>
    /// 验证缓存操作基类
    /// </summary>
    internal abstract class FCVerifyCacheOperation : AsyncOperationBase
    {
    }

    /// <summary>
    /// 验证缓存完成操作
    /// </summary>
    internal class FCVerifyCacheCompleteOperation : FCVerifyCacheOperation
    {
        private readonly string _error;
        
        public FCVerifyCacheCompleteOperation()
        {
            _error = null;
        }
        public FCVerifyCacheCompleteOperation(string error)
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
