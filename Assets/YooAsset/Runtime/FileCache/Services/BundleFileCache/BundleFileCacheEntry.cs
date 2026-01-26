using System;
using System.IO;

namespace YooAsset
{
    internal class BundleCacheEntry : ICacheEntry
    {
        public string BundleGUID { get; private set; }
        public string InfoFilePath { get; private set; }
        public string DataFilePath { get; private set; }
        public uint DataFileCRC { get; private set; }
        public long DataFileSize { get; private set; }

        public BundleCacheEntry(string bundleGUID, string infoFilePath, string dataFilePath, uint dataFileCRC, long dataFileSize)
        {
            BundleGUID = bundleGUID;
            InfoFilePath = infoFilePath;
            DataFilePath = dataFilePath;
            DataFileCRC = dataFileCRC;
            DataFileSize = dataFileSize;
        }

        /// <summary>
        /// 移动文件
        /// </summary>
        public void MoveFile(string destFilePath)
        {
            File.Move(DataFilePath, destFilePath);
            DataFilePath = destFilePath;
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
                YooLogger.Error($"Failed to delete cache file. Error: {ex.Message}");
                return false;
            }
        }
    }
}