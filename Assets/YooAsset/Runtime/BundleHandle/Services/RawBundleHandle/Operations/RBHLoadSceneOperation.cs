
namespace YooAsset
{
    /// <summary>
    /// 原生资源包的场景加载操作（不支持）
    /// </summary>
    internal class RBHLoadSceneOperation : BHLoadSceneOperation
    {
        internal override void InternalStart()
        {
            Status = EOperationStatus.Failed;
            Error = $"{nameof(RBHLoadSceneOperation)} does not support loading scene.";
        }
        internal override void InternalUpdate()
        {
        }
        public override void ResumeLoad()
        {
        }
    }
}