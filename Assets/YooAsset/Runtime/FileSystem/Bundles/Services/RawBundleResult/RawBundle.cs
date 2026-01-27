
namespace YooAsset
{
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

        public void Unload()
        {
            _data = null;
        }
    }
}