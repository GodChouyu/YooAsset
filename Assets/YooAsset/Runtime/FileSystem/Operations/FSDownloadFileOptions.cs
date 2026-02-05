
namespace YooAsset
{
    internal readonly struct FSDownloadFileOptions
    {
        /// <summary>
        /// 资源包对象
        /// </summary>
        public readonly PackageBundle Bundle;

        /// <summary>
        /// 失败后重试次数
        /// </summary>
        public readonly int RetryCount;

        /// <summary>
        /// 拷贝的本地文件路径
        /// </summary>
        public readonly string ImportFilePath;

        public FSDownloadFileOptions(PackageBundle bundle, int retryCount)
        {
            Bundle = bundle;
            RetryCount = retryCount;
            ImportFilePath = null;
        }
        public FSDownloadFileOptions(PackageBundle bundle, int retryCount, string importFilePath)
        {
            Bundle = bundle;
            RetryCount = retryCount;
            ImportFilePath = importFilePath;
        }
    }
}