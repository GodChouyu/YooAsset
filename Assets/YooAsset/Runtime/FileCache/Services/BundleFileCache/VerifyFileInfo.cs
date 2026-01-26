using System.IO;

namespace YooAsset
{
    internal class VerifyFileInfo
    {
        public string PackageName { get; private set; }
        public string BundleGUID { get; private set; }
        public string FolderPath { get; private set; }
        public string DataFilePath { get; private set; }
        public string InfoFilePath { get; private set; }

        public uint DataFileCRC { get; private set; }
        public long DataFileSize { get; private set; }

        /// <summary>
        /// 注意：原子操作对象
        /// </summary>
        public volatile int Result = 0;

        public VerifyFileInfo(string packageName, string bundleGUID, string folderPath, string dataFilePath, string infoFilePath)
        {
            PackageName = packageName;
            BundleGUID = bundleGUID;
            FolderPath = folderPath;
            DataFilePath = dataFilePath;
            InfoFilePath = infoFilePath;
        }

        public void SetDataFileInfo(uint dataFileCRC, long dataFileSize)
        {
            DataFileCRC = dataFileCRC;
            DataFileSize = dataFileSize;
        }

        public void DeleteFiles()
        {
            try
            {
                Directory.Delete(FolderPath, true);
            }
            catch (System.Exception ex)
            {
                YooLogger.Warning($"Failed to delete cache bundle folder : {ex}");
            }
        }
    }
}