using System.IO;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 加载 AssetBundle 的抽象基类
    /// 用户可继承此类实现自定义加载逻辑（如加密解密）
    /// </summary>
    public abstract class LoadAssetBundleOperation : AsyncOperationBase
    {
        protected readonly LoadAssetBundleOptions _options;

        /// <summary>
        /// 加载结果：AssetBundle 对象
        /// </summary>
        public AssetBundle Result { get; protected set; }

        /// <summary>
        /// 托管流对象（如果使用流加载）
        /// 注意：流对象在资源包对象释放的时候会自动释放
        /// </summary>
        public Stream ManagedStream { get; protected set; }

        public LoadAssetBundleOperation(LoadAssetBundleOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// 后备加载方法：从内存加载 AssetBundle
        /// 当主加载方式失败时，FileSystem 会调用此方法作为后备机制
        /// </summary>
        /// <returns>加载成功返回 AssetBundle 对象，失败返回 null</returns>
        public abstract AssetBundle LoadFromMemory();

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
    /// 立即完成（失败）的 AssetBundle 加载操作
    /// 用途：当 Factory 判定某种场景不支持（例如默认实现不支持加密包）时，返回该 Operation
    /// </summary>
    public sealed class LoadAssetBundleCompleteOperation : LoadAssetBundleOperation
    {
        private readonly string _error;

        public LoadAssetBundleCompleteOperation(string error, LoadAssetBundleOptions options) : base(options)
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
        public override AssetBundle LoadFromMemory()
        {
            return null;
        }
    }
}
