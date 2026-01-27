
namespace YooAsset
{
    /// <summary>
    /// 原生文件提供者，负责加载原始文件资源
    /// </summary>
    internal class RawFileProvider : ProviderBase
    {
        public RawFileProvider(ResourceManager manager, string providerGUID, AssetInfo assetInfo) : base(manager, providerGUID, assetInfo)
        {
        }
        protected override void ProcessBundleResult()
        {
            InvokeCompletion(string.Empty, EOperationStatus.Succeeded);
        }
    }
}