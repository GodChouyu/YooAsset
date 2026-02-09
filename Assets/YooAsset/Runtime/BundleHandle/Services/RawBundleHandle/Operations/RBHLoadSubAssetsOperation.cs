
namespace YooAsset
{
    /// <summary>
    /// 原生资源包的加载子资源操作（不支持）
    /// </summary>
    internal class RBHLoadSubAssetsOperation : BHLoadSubAssetsOperation
    {
        internal override void InternalStart()
        {
            Status = EOperationStatus.Failed;
            Error = $"{nameof(RBHLoadSubAssetsOperation)} does not support loading sub-assets.";
        }
        internal override void InternalUpdate()
        {
        }
    }
}