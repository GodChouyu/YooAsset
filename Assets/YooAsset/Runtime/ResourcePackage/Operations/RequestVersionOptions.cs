
namespace YooAsset
{
    public struct RequestVersionOptions
    {
        /// <summary>
        /// 在URL末尾添加时间戳
        /// </summary>
        public bool AppendTimeTicks { get; set; }

        /// <summary>
        /// 超时时间
        /// </summary>
        public int Timeout { get; set; }

        public RequestVersionOptions(bool appendTimeTicks, int timeout)
        {
            AppendTimeTicks = appendTimeTicks;
            Timeout = timeout;
        }
    }
}