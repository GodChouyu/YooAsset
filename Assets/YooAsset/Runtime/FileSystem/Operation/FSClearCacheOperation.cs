
namespace YooAsset
{
    internal abstract class FSClearCacheOperation : AsyncOperationBase
    {
    }

    internal sealed class FSClearCacheCompleteOperation : FSClearCacheOperation
    {
        private readonly string _error;

        internal FSClearCacheCompleteOperation()
        {
            _error = null;
        }
        internal FSClearCacheCompleteOperation(string error)
        {
            _error = error;
        }
        internal override void InternalStart()
        {
            if (string.IsNullOrEmpty(_error))
            {
                Status = EOperationStatus.Succeed;
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