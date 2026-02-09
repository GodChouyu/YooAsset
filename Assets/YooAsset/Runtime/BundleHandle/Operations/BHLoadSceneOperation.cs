
namespace YooAsset
{
    /// <summary>
    /// 加载场景操作的抽象基类
    /// </summary>
    internal abstract class BHLoadSceneOperation : AsyncOperationBase
    {
        /// <summary>
        /// 加载的场景对象
        /// </summary>
        public UnityEngine.SceneManagement.Scene Result;

        /// <summary>
        /// 恢复挂起的场景加载
        /// </summary>
        public abstract void ResumeLoad();
    }
}