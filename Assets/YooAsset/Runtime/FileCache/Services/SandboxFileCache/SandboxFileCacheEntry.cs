using System;
using System.IO;

namespace YooAsset
{
    internal class SandboxFileCacheEntry : ICacheEntry
    {
        public string BundleGUID { get; private set; }
        public string InfoFilePath { get; private set; }
        public string DataFilePath { get; private set; }
        private long _fileSize = 0;

        public SandboxFileCacheEntry(string bundleGUID, string infoFilePath, string dataFilePath)
        {
            BundleGUID = bundleGUID;
            InfoFilePath = infoFilePath;
            DataFilePath = dataFilePath;
        }

        /// <summary>
        /// 删除记录文件
        /// </summary>
        public bool Delete()
        {
            try
            {
                string directory = Path.GetDirectoryName(InfoFilePath);
                var directoryInfo = new DirectoryInfo(directory);
                if (directoryInfo.Exists)
                {
                    directoryInfo.Delete(true);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                YooLogger.Error($"Failed to delete sandbox file. Error: {ex.Message}");
                return false;
            }
        }

        public long GetFileSize()
        {
            if (_fileSize == 0)
            {
                _fileSize = FileUtility.GetFileSize(InfoFilePath);
                _fileSize += FileUtility.GetFileSize(DataFilePath);
            }
            return _fileSize;
        }
    }
}