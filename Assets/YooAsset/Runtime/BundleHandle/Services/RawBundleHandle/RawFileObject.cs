using System.Text;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 原生文件对象
    /// </summary>
    public class RawFileObject : ScriptableObject
    {
        private byte[] _fileData;
        private string _fileText;

        /// <summary>
        /// 原生文件的二进制数据
        /// </summary>
        public byte[] Data => _fileData;

        /// <summary>
        /// 原生文件的UTF-8文本内容
        /// </summary>
        public string Text
        {
            get
            {
                if (_fileData == null || _fileData.Length == 0)
                    return null;

                if (string.IsNullOrEmpty(_fileText))
                    _fileText = Encoding.UTF8.GetString(_fileData);
                return _fileText;
            }
        }

        /// <summary>
        /// 创建原生文件对象实例
        /// </summary>
        public static RawFileObject Create(byte[] data)
        {
            var obj = CreateInstance<RawFileObject>();
            obj._fileData = data;
            return obj;
        }
    }
}