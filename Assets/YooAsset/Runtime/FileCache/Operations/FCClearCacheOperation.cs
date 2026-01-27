
namespace YooAsset
{
    internal abstract class FCClearCacheOperation : AsyncOperationBase
    {
    }

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
