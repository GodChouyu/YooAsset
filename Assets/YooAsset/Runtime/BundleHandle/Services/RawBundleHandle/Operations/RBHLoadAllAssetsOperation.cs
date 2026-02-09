
namespace YooAsset
{
    /// <summary>
    /// 原生资源包的加载所有资源操作（不支持）
    /// </summary>
    internal class RBHLoadAllAssetsOperation : BHLoadAllAssetsOperation
    {
        internal override void InternalStart()
        {
            Status = EOperationStatus.Failed;
            Error = $"{nameof(RBHLoadAllAssetsOperation)} does not support loading all assets.";
        }
        internal override void InternalUpdate()
        {
        }
    }
}