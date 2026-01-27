using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 清单文件工具类
    /// </summary>
    internal static class PackageManifestTools
    {
        /// <summary>
        /// MD5哈希值长度（32个字符）
        /// </summary>
        private const int MD5HashLength = 32;

        /// <summary>
        /// 验证清单文件的二进制数据
        /// </summary>
        /// <param name="fileData">文件二进制数据</param>
        /// <param name="hashValue">期望的哈希值</param>
        /// <returns>如果验证通过返回true，否则返回false</returns>
        public static bool VerifyManifestData(byte[] fileData, string hashValue)
        {
            if (fileData == null || fileData.Length == 0)
                return false;
            if (string.IsNullOrEmpty(hashValue))
                return false;

            // 注意:兼容两种验证方式
            string fileHash;
            if (hashValue.Length == MD5HashLength)
                fileHash = HashUtility.ComputeBytesMD5(fileData);
            else
                fileHash = HashUtility.ComputeBytesCRC32(fileData);

            return fileHash == hashValue;
        }

        /// <summary>
        /// 将清单文件序列化为JSON格式
        /// </summary>
        /// <param name="savePath">保存路径</param>
        /// <param name="manifest">清单对象</param>
        public static void SerializeManifestToJson(string savePath, PackageManifest manifest)
        {
            string json = JsonUtility.ToJson(manifest, true);
            FileUtility.WriteAllText(savePath, json);
        }

        /// <summary>
        /// 将清单文件序列化为二进制格式
        /// </summary>
        /// <param name="savePath">保存路径</param>
        /// <param name="manifest">清单对象</param>
        /// <param name="services">清单处理服务（可为null）</param>
        public static void SerializeManifestToBinary(string savePath, PackageManifest manifest, IManifestProcessServices services)
        {
            using (FileStream fs = new FileStream(savePath, FileMode.Create))
            {
                // 创建缓存器
                BufferWriter buffer = new BufferWriter(PackageManifestDefine.FileMaxSize);

                // 写入文件标记
                buffer.WriteUInt32(PackageManifestDefine.FileSign);

                // 写入文件版本
                buffer.WriteUTF8(manifest.FileVersion);

                // 写入文件头信息
                buffer.WriteBool(manifest.EnableAddressable);
                buffer.WriteBool(manifest.SupportExtensionless);
                buffer.WriteBool(manifest.LocationToLower);
                buffer.WriteBool(manifest.IncludeAssetGUID);
                buffer.WriteBool(manifest.ReplaceAssetPathWithAddress);
                buffer.WriteInt32(manifest.OutputNameStyle);
                buffer.WriteInt32(manifest.BuildBundleType);
                buffer.WriteUTF8(manifest.BuildPipeline);
                buffer.WriteUTF8(manifest.PackageName);
                buffer.WriteUTF8(manifest.PackageVersion);
                buffer.WriteUTF8(manifest.PackageNote);

                // 写入资源列表
                buffer.WriteInt32(manifest.AssetList.Count);
                for (int i = 0; i < manifest.AssetList.Count; i++)
                {
                    var packageAsset = manifest.AssetList[i];
                    buffer.WriteUTF8(packageAsset.Address);
                    buffer.WriteUTF8(packageAsset.AssetPath);
                    buffer.WriteUTF8(packageAsset.AssetGUID);
                    buffer.WriteUTF8Array(packageAsset.AssetTags);
                    buffer.WriteInt32(packageAsset.BundleID);
                    buffer.WriteInt32Array(packageAsset.DependentBundleIDs);
                }

                // 写入资源包列表
                buffer.WriteInt32(manifest.BundleList.Count);
                for (int i = 0; i < manifest.BundleList.Count; i++)
                {
                    var packageBundle = manifest.BundleList[i];
                    buffer.WriteUTF8(packageBundle.BundleName);
                    buffer.WriteUInt32(packageBundle.UnityCRC);
                    buffer.WriteUTF8(packageBundle.FileHash);
                    buffer.WriteUInt32(packageBundle.FileCRC);
                    buffer.WriteInt64(packageBundle.FileSize);
                    buffer.WriteBool(packageBundle.IsEncrypted);
                    buffer.WriteUTF8Array(packageBundle.Tags);
                    buffer.WriteInt32Array(packageBundle.DependentBundleIDs);
                }

                // 清单处理操作
                if (services != null)
                {
                    var tempBytes = buffer.GetBytes();
                    var resultBytes = services.ProcessManifest(tempBytes);
                    fs.Write(resultBytes, 0, resultBytes.Length);
                    fs.Flush();
                }
                else
                {
                    // 写入文件流
                    buffer.WriteToStream(fs);
                    fs.Flush();
                }
            }
        }

        /// <summary>
        /// 从JSON字符串反序列化清单文件
        /// </summary>
        /// <param name="jsonContent">JSON内容字符串</param>
        /// <returns>返回反序列化后的清单对象</returns>
        public static PackageManifest DeserializeManifestFromJson(string jsonContent)
        {
            var manifest = JsonUtility.FromJson<PackageManifest>(jsonContent);

            // 初始化资源包
            for (int i = 0; i < manifest.BundleList.Count; i++)
            {
                var packageBundle = manifest.BundleList[i];
                packageBundle.Initialize(manifest);
            }

            // 初始化资源清单
            manifest.Initialize();
            return manifest;
        }

        /// <summary>
        /// 从二进制数据反序列化清单文件
        /// </summary>
        /// <param name="binaryData">二进制数据</param>
        /// <param name="services">清单恢复服务（可为null）</param>
        /// <returns>返回反序列化后的清单对象</returns>
        public static PackageManifest DeserializeManifestFromBinary(byte[] binaryData, IManifestRestoreServices services)
        {
            DeserializeManifestOperation operation = new DeserializeManifestOperation(services, binaryData);
            operation.StartOperation();
            operation.WaitForCompletion();
            return operation.Manifest;
        }

        /// <summary>
        /// 获取资源文件的后缀名
        /// </summary>
        /// <param name="bundleName">资源包名称</param>
        /// <returns>返回文件后缀名（包含点号）</returns>
        public static string GetRemoteBundleFileExtension(string bundleName)
        {
            string fileExtension = Path.GetExtension(bundleName);
            return fileExtension;
        }

        /// <summary>
        /// 获取远端的资源文件名
        /// </summary>
        /// <param name="nameStyle">文件名称样式</param>
        /// <param name="bundleName">资源包名称</param>
        /// <param name="fileExtension">文件后缀名</param>
        /// <param name="fileHash">文件哈希值</param>
        /// <returns>返回根据命名样式生成的远端文件名</returns>
        public static string GetRemoteBundleFileName(int nameStyle, string bundleName, string fileExtension, string fileHash)
        {
            EFileNameStyle style = (EFileNameStyle)nameStyle;
            switch (style)
            {
                case EFileNameStyle.HashName:
                    return StringUtility.Format("{0}{1}", fileHash, fileExtension);

                case EFileNameStyle.BundleName:
                    return bundleName;

                case EFileNameStyle.BundleName_HashName:
                    if (string.IsNullOrEmpty(fileExtension))
                    {
                        return StringUtility.Format("{0}_{1}", bundleName, fileHash);
                    }
                    else
                    {
                        string fileName = bundleName.Remove(bundleName.LastIndexOf('.'));
                        return StringUtility.Format("{0}_{1}{2}", fileName, fileHash, fileExtension);
                    }

                default:
                    throw new NotImplementedException($"Invalid name style : {nameStyle}");
            }
        }
    }
}