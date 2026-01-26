
namespace YooAsset
{
    internal struct FCInitializeOptions
    {
        /// <summary>
        /// 文件校验最大并发数
        /// </summary>
        public int FileVerifyMaxConcurrency { get; set; }

        /// <summary>
        /// 文件校验级别
        /// </summary>
        public EFileVerifyLevel FileVerifyLevel { get; set; }
    }
}
