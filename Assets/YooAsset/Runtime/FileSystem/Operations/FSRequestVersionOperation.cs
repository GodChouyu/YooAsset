
namespace YooAsset
{
    internal abstract class FSRequestVersionOperation : AsyncOperationBase
    {
        /// <summary>
        /// 资源版本
        /// </summary>
        internal string PackageVersion { set; get; }
    }
}