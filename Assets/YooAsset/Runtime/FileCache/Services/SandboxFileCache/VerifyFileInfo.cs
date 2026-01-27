using System.IO;

namespace YooAsset
{
    internal class VerifyFileInfo
    {
        public string BundleGUID { get; private set; }
        public string FolderPath { get; private set; }
        public string DataFilePath { get; private set; }
        public string InfoFilePath { get; private set; }

        /// <summary>
        /// 注意：原子操作对象
        /// </summary>
        public volatile int Result = 0;

        public VerifyFileInfo(string bundleGUID, string folderPath, string dataFilePath, string infoFilePath)
        {
            BundleGUID = bundleGUID;
            FolderPath = folderPath;
            DataFilePath = dataFilePath;
            InfoFilePath = infoFilePath;
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