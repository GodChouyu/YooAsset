
namespace YooAsset
{
    internal struct DownloadFileOptions
    {
        /// <summary>
        /// 资源包
        /// </summary>
        public readonly PackageBundle Bundle;

        /// <summary>
        /// 失败后重试次数
        /// </summary>
        public readonly int FailedTryAgain;

        /// <summary>
        /// 主资源地址
        /// </summary>
        public string MainURL { private set; get; }

        /// <summary>
        /// 备用资源地址
        /// </summary>
        public string FallbackURL { private set; get; }

        /// <summary>
        /// 拷贝的本地文件路径
        /// </summary>
        public string ImportFilePath { set; get; }

        public DownloadFileOptions(PackageBundle bundle, int failedTryAgain)
        {
            Bundle = bundle;
            FailedTryAgain = failedTryAgain;
            MainURL = null;
            FallbackURL = null;
            ImportFilePath = null;
        }

        /// <summary>
        /// 设置下载地址
        /// </summary>
        public void SetURL(string mainURL, string fallbackURL)
        {
            MainURL = mainURL;
            FallbackURL = fallbackURL;
        }

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(MainURL) || string.IsNullOrEmpty(FallbackURL))
                return false;

            return true;
        }
    }
}