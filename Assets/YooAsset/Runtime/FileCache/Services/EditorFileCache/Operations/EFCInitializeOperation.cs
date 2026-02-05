
namespace YooAsset
{
    /// <summary>
    /// 编辑器文件缓存初始化操作
    /// </summary>
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
