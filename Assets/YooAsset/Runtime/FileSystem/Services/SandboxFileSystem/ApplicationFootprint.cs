using System.IO;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 应用程序水印
    /// </summary>
    internal class ApplicationFootprint
    {
        private readonly string _filePath;
        private string _footprint;

        public ApplicationFootprint(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// 读取应用程序水印
        /// </summary>
        public void Load(string packageName)
        {
            if (File.Exists(_filePath))
            {
                _footprint = FileUtility.ReadAllText(_filePath);
            }
            else
            {
                Overwrite(packageName);
            }
        }

        /// <summary>
        /// 检测水印是否发生变化
        /// </summary>
        public bool IsDirty()
        {
#if UNITY_EDITOR
            return _footprint != Application.version;
#else
		    return _footprint != Application.buildGUID;
#endif
        }

        /// <summary>
        /// 覆盖掉水印
        /// </summary>
        public void Overwrite(string packageName)
        {
#if UNITY_EDITOR
            _footprint = Application.version;
#else
			_footprint = Application.buildGUID;
#endif
            FileUtility.WriteAllText(_filePath, _footprint);
            YooLogger.Log($"Save application footprint: {_footprint}");
        }
    }
}