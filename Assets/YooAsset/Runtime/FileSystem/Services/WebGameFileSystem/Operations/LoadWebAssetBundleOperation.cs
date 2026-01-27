using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 加载 AssetBundle 的抽象基类
    /// 用户可继承此类实现自定义加载逻辑（如加密解密）
    /// </summary>
    public abstract class LoadWebAssetBundleOperation : AsyncOperationBase
    {
        protected readonly LoadWebAssetBundleOptions _options;

        /// <summary>
        /// 加载结果：AssetBundle 对象
        /// </summary>
        public AssetBundle Result { get; protected set; }

        /// <summary>
        /// 下载进度
        /// </summary>
        public float DownloadProgress { protected set; get; }

        /// <summary>
        /// 下载大小
        /// </summary>
        public long DownloadedBytes { protected set; get; }

        public LoadWebAssetBundleOperation(LoadWebAssetBundleOptions options)
        {
            _options = options;
        }
    }

    /// <summary>
    /// 立即完成（失败）的 AssetBundle 加载操作
    /// 用途：当 Factory 判定某种场景不支持（例如默认实现不支持加密包）时，返回该 Operation
    /// </summary>
    public sealed class LoadWebAssetBundleCompleteOperation : LoadWebAssetBundleOperation
    {
        private readonly string _error;

        public LoadWebAssetBundleCompleteOperation(string error, LoadWebAssetBundleOptions options) : base(options)
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
