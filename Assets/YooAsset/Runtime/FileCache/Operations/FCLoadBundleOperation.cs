
namespace YooAsset
{
    internal abstract class FCLoadBundleOperation : AsyncOperationBase
    {
        public IBundleResult BundleResult { get; protected set; }

        /// <summary>
        /// 检查文件路径是否支持 FileIO 读取
        /// </summary>
        protected bool IsSupportFileIO(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return true;
            if (filePath.StartsWith("jar:") || filePath.StartsWith("content:"))
                return false;
            return true;
        }
    }

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