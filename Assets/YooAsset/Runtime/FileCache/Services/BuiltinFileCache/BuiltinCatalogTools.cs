using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 内置资源目录工具类
    /// </summary>
    internal static class BuiltinCatalogTools
    {
#if UNITY_EDITOR
        /// <summary>
        /// 生成包裹的内置资源目录文件
        /// 说明：根据指定目录下的文件生成清单文件。
        /// </summary>
        public static bool CreateFile(IManifestDecryptor decryptor, string packageName, string packageDirectory)
        {
            // 获取资源清单版本
            string packageVersion;
            {
                string versionFileName = YooAssetSettingsData.GetPackageVersionFileName(packageName);
                string versionFilePath = $"{packageDirectory}/{versionFileName}";
                if (File.Exists(versionFilePath) == false)
                {
                    Debug.LogError($"Package version file not found: {versionFilePath}");
                    return false;
                }

                packageVersion = FileUtility.ReadAllText(versionFilePath);
            }

            // 加载资源清单文件
            PackageManifest packageManifest;
            {
                string manifestFileName = YooAssetSettingsData.GetManifestBinaryFileName(packageName, packageVersion);
                string manifestFilePath = $"{packageDirectory}/{manifestFileName}";
                if (File.Exists(manifestFilePath) == false)
                {
                    Debug.LogError($"Package manifest file not found: {manifestFilePath}");
                    return false;
                }

                var binaryData = FileUtility.ReadAllBytes(manifestFilePath);
                packageManifest = PackageManifestTools.DeserializeManifestFromBinary(binaryData, decryptor);
            }

            // 获取文件名映射关系
            Dictionary<string, string> fileMapping = new Dictionary<string, string>();
            {
                foreach (var packageBundle in packageManifest.BundleList)
                {
                    fileMapping.Add(packageBundle.FileName, packageBundle.BundleGUID);
                }
            }

            // 创建内置清单实例
            var buildinCatalog = new BuiltinCatalog();
            buildinCatalog.FileVersion = BuiltinCatalogDefine.FileVersion;
            buildinCatalog.PackageName = packageName;
            buildinCatalog.PackageVersion = packageVersion;

            // 创建白名单查询集合
            HashSet<string> whiteFileNameList = new HashSet<string>
            {
                "link.xml",
                "buildlogtep.json",
                BuiltinCatalogDefine.JsonFileName,
                BuiltinCatalogDefine.BinaryFileName
            };
            string packageVersionFileName = YooAssetSettingsData.GetPackageVersionFileName(packageName);
            string packageHashFileName = YooAssetSettingsData.GetPackageHashFileName(packageName, packageVersion);
            string manifestBinaryFileName = YooAssetSettingsData.GetManifestBinaryFileName(packageName, packageVersion);
            string manifestJsonFileName = YooAssetSettingsData.GetManifestJsonFileName(packageName, packageVersion);
            string reportFileName = YooAssetSettingsData.GetBuildReportFileName(packageName, packageVersion);
            whiteFileNameList.Add(packageVersionFileName);
            whiteFileNameList.Add(packageHashFileName);
            whiteFileNameList.Add(manifestBinaryFileName);
            whiteFileNameList.Add(manifestJsonFileName);
            whiteFileNameList.Add(reportFileName);

            // 记录所有内置资源文件
            DirectoryInfo rootDirectory = new DirectoryInfo(packageDirectory);
            FileInfo[] fileInfos = rootDirectory.GetFiles();
            foreach (var fileInfo in fileInfos)
            {
                if (fileInfo.Extension == ".meta")
                    continue;

                if (whiteFileNameList.Contains(fileInfo.Name))
                    continue;

                string fileName = fileInfo.Name;
                if (fileMapping.TryGetValue(fileName, out string bundleGUID))
                {
                    var fileEntry = new BuiltinCatalog.FileEntry();
                    fileEntry.BundleGUID = bundleGUID;
                    fileEntry.FileName = fileName;
                    buildinCatalog.FileEntries.Add(fileEntry);
                }
                else
                {
                    Debug.LogWarning($"Failed to map file: {fileName}");
                }
            }

            // 创建输出文件
            string jsonFilePath = $"{packageDirectory}/{BuiltinCatalogDefine.JsonFileName}";
            if (File.Exists(jsonFilePath))
                File.Delete(jsonFilePath);
            SerializeToJson(jsonFilePath, buildinCatalog);

            // 创建输出文件
            string binaryFilePath = $"{packageDirectory}/{BuiltinCatalogDefine.BinaryFileName}";
            if (File.Exists(binaryFilePath))
                File.Delete(binaryFilePath);
            SerializeToBinary(binaryFilePath, buildinCatalog);

            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"Successfully saved catalog file: {binaryFilePath}");
            return true;
        }

        /// <summary>
        /// 生成空的包裹内置资源目录文件
        /// </summary>
        public static bool CreateEmptyFile(string packageName, string packageVersion, string outputPath)
        {
            // 创建内置清单实例
            var buildinFileCatalog = new BuiltinCatalog();
            buildinFileCatalog.FileVersion = BuiltinCatalogDefine.FileVersion;
            buildinFileCatalog.PackageName = packageName;
            buildinFileCatalog.PackageVersion = packageVersion;

            // 创建输出文件
            string jsonFilePath = $"{outputPath}/{BuiltinCatalogDefine.JsonFileName}";
            if (File.Exists(jsonFilePath))
                File.Delete(jsonFilePath);
            SerializeToJson(jsonFilePath, buildinFileCatalog);

            // 创建输出文件
            string binaryFilePath = $"{outputPath}/{BuiltinCatalogDefine.BinaryFileName}";
            if (File.Exists(binaryFilePath))
                File.Delete(binaryFilePath);
            SerializeToBinary(binaryFilePath, buildinFileCatalog);

            UnityEditor.AssetDatabase.Refresh();
            Debug.Log($"Successfully saved catalog file: {binaryFilePath}");
            return true;
        }

        /// <summary>
        /// 序列化（JSON文件）
        /// </summary>
        public static void SerializeToJson(string savePath, BuiltinCatalog catalog)
        {
            string json = JsonUtility.ToJson(catalog, true);
            FileUtility.WriteAllText(savePath, json);
        }

        /// <summary>
        /// 序列化（二进制文件）
        /// </summary>
        public static void SerializeToBinary(string savePath, BuiltinCatalog catalog)
        {
            using (FileStream fs = new FileStream(savePath, FileMode.Create))
            {
                // 创建缓存器
                BufferWriter buffer = new BufferWriter(BuiltinCatalogDefine.MaxFileSize);

                // 写入文件标记
                buffer.WriteUInt32(BuiltinCatalogDefine.FileHeader);

                // 写入文件版本
                buffer.WriteUTF8(BuiltinCatalogDefine.FileVersion);

                // 写入文件头信息
                buffer.WriteUTF8(catalog.PackageName);
                buffer.WriteUTF8(catalog.PackageVersion);

                // 写入资源包列表
                buffer.WriteInt32(catalog.FileEntries.Count);
                for (int i = 0; i < catalog.FileEntries.Count; i++)
                {
                    var fileWrapper = catalog.FileEntries[i];
                    buffer.WriteUTF8(fileWrapper.BundleGUID);
                    buffer.WriteUTF8(fileWrapper.FileName);
                }

                // 写入文件流
                buffer.WriteToStream(fs);
                fs.Flush();
            }
        }
#endif

        /// <summary>
        /// 反序列化（JSON文件）
        /// </summary>
        public static BuiltinCatalog DeserializeFromJson(string jsonContent)
        {
            return JsonUtility.FromJson<BuiltinCatalog>(jsonContent);
        }

        /// <summary>
        /// 反序列化（二进制文件）
        /// </summary>
        public static BuiltinCatalog DeserializeFromBinary(byte[] binaryData)
        {
            if (binaryData == null || binaryData.Length == 0)
                throw new Exception("Catalog file data is null or empty.");

            // 创建缓存器
            BufferReader buffer = new BufferReader(binaryData);

            // 读取文件标记
            uint fileHeader = buffer.ReadUInt32();
            if (fileHeader != BuiltinCatalogDefine.FileHeader)
                throw new Exception("Invalid catalog file.");

            // 读取文件版本
            string fileVersion = buffer.ReadUTF8();
            if (fileVersion != BuiltinCatalogDefine.FileVersion)
                throw new Exception($"The catalog file version is not compatible: {fileVersion} != {BuiltinCatalogDefine.FileVersion}");

            BuiltinCatalog catalog = new BuiltinCatalog();
            {
                // 读取文件头信息
                catalog.FileVersion = fileVersion;
                catalog.PackageName = buffer.ReadUTF8();
                catalog.PackageVersion = buffer.ReadUTF8();

                // 读取文件条目列表
                int fileCount = buffer.ReadInt32();
                catalog.FileEntries = new List<BuiltinCatalog.FileEntry>(fileCount);
                for (int i = 0; i < fileCount; i++)
                {
                    var fileEntry = new BuiltinCatalog.FileEntry();
                    fileEntry.BundleGUID = buffer.ReadUTF8();
                    fileEntry.FileName = buffer.ReadUTF8();
                    catalog.FileEntries.Add(fileEntry);
                }
            }

            return catalog;
        }
    }
}