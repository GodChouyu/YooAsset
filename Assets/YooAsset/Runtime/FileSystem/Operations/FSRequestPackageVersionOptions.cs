
namespace YooAsset
{
    /// <summary>
    /// 请求包裹版本操作选项
    /// </summary>
    internal readonly struct FSRequestPackageVersionOptions
    {
        /// <summary>
        /// 在URL末尾添加时间戳
        /// </summary>
        public readonly bool AppendTimeTicks;

        /// <summary>
        /// 超时时间
        /// </summary>
        public readonly int Timeout;

        public FSRequestPackageVersionOptions(bool appendTimeTicks, int timeout)
        {
            AppendTimeTicks = appendTimeTicks;
            Timeout = timeout;
        }
    }
}
