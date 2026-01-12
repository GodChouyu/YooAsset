using System.Text;

namespace YooAsset
{
    /// <summary>
    /// 加载 RawBundle 的 Operation 工厂委托
    /// </summary>
    public delegate LoadRawBundleOperation LoadRawBundleOperationFactory(bool bundleEncrypted, LoadRawBundleOptions options);

    /// <summary>
    /// 加载 RawBundle 的抽象基类
    /// 用户可继承此类实现自定义加载逻辑（如加密解密）
    /// </summary>
    public abstract class LoadRawBundleOperation : AsyncOperationBase
    {
        protected readonly LoadRawBundleOptions _options;

        /// <summary>
        /// 加载结果：RawBundle 对象
        /// </summary>
        public RawBundle Result { get; protected set; }

        public LoadRawBundleOperation(LoadRawBundleOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// 检查文件路径是否支持 FileIO 读取
        /// </summary>
        protected static bool IsSupportFileIO(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return true;
            if (filePath.StartsWith("jar:") || filePath.StartsWith("content:"))
                return false;
            return true;
        }
    }

    /// <summary>
    /// 立即完成（失败）的 RawBundle 加载操作
    /// 用途：当 Factory 判定某种场景不支持（例如默认实现不支持加密包）时，返回该 Operation
    /// </summary>
    public sealed class LoadRawBundleCompleteOperation : LoadRawBundleOperation
    {
        private readonly string _error;

        public LoadRawBundleCompleteOperation(string error, LoadRawBundleOptions options) : base(options)
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
