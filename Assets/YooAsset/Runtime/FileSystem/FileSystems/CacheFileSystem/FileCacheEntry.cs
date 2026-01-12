using System;
using System.IO;

namespace YooAsset
{
    internal class FileCacheEntry
    {
        public string InfoFilePath { private set; get; }
        public string DataFilePath { private set; get; }
        public uint DataFileCRC { private set; get; }
        public long DataFileSize { private set; get; }

        public FileCacheEntry(string infoFilePath, string dataFilePath, uint dataFileCRC, long dataFileSize)
        {
            InfoFilePath = infoFilePath;
            DataFilePath = dataFilePath;
            DataFileCRC = dataFileCRC;
            DataFileSize = dataFileSize;
        }

        /// <summary>
        /// 修正内容
        /// </summary>
        public void Modify(string dataFilePath)
        {
            DataFilePath = dataFilePath;
        }

        /// <summary>
        /// 删除记录文件
        /// </summary>
        public bool DeleteFolder()
        {
            try
            {
                string directory = Path.GetDirectoryName(InfoFilePath);
                DirectoryInfo directoryInfo = new DirectoryInfo(directory);
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