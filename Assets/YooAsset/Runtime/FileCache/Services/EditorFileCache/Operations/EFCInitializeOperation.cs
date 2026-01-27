
namespace YooAsset
{
    internal class EFCInitializeOperation : FCInitializeOperation
    {
        private readonly EditorFileCache _fileCache;

        public EFCInitializeOperation(EditorFileCache cache)
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
