
namespace YooAsset
{
    /// <summary>
    /// 请求版本选项
    /// </summary>
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

        /// <summary>
        /// 创建请求版本选项
        /// </summary>
        /// <param name="appendTimeTicks">是否在URL末尾添加时间戳</param>
        /// <param name="timeout">超时时间（秒）</param>
        public RequestVersionOptions(bool appendTimeTicks, int timeout)
        {
            AppendTimeTicks = appendTimeTicks;
            Timeout = timeout;
        }
    }
}