
namespace YooAsset
{
    /// <summary>
    /// 加载所有资源操作的抽象基类
    /// </summary>
    internal abstract class BHLoadAllAssetsOperation : AsyncOperationBase
    {
        /// <summary>
        /// 加载的所有资源对象
        /// </summary>
        public UnityEngine.Object[] Result;
    }
}