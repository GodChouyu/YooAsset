
namespace YooAsset
{
    /// <summary>
    /// 原生资源包
    /// </summary>
    public class RawBundle
    {
        private byte[] _data;

        public RawBundle(byte[] data)
        {
            _data = data;
        }

        /// <summary>
        /// 加载原生文件对象
        /// </summary>
        public RawFileObject LoadRawFileObject()
        {
            return RawFileObject.Create(_data);
        }

        /// <summary>
        /// 卸载原生资源包数据
        /// </summary>
        public void Unload()
        {
            _data = null;
        }
    }
}