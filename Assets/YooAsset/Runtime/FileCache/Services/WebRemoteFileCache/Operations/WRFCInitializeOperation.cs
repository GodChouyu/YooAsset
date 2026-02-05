
namespace YooAsset
{
    /// <summary>
    /// Web远端文件缓存初始化操作
    /// </summary>
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
