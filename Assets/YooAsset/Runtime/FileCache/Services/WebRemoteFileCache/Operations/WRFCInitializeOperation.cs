
namespace YooAsset
{
    internal class WRFCInitializeOperation : FCInitializeOperation
    {
        private readonly WebRemoteFileCache _fileCache;

        public WRFCInitializeOperation(WebRemoteFileCache cache)
        {
            _fileCache = cache;
        }
        internal override void InternalStart()
        {
            Status = EOperationStatus.Succeeded;
        }
        internal override void InternalUpdate()
        {
        }
    }
}
