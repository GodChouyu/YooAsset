using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace YooAsset
{
    /// <summary>
    /// 文件工具类
    /// </summary>
    internal static class FileUtility
    {
        /// <summary>
        /// 读取文件的文本数据
        /// </summary>
        public static string ReadAllText(string filePath)
        {
            if (File.Exists(filePath) == false)
                return null;
            return File.ReadAllText(filePath, Encoding.UTF8);
        }

        /// <summary>
        /// 读取文件的字节数据
        /// </summary>
        public static byte[] ReadAllBytes(string filePath)
        {
            if (File.Exists(filePath) == false)
                return null;
            return File.ReadAllBytes(filePath);
        }

        /// <summary>
        /// 写入文本数据（会覆盖指定路径的文件）
        /// </summary>
        public static void WriteAllText(string filePath, string content)
        {
            // 创建文件夹路径
            EnsureFileDirectory(filePath);

            byte[] bytes = Encoding.UTF8.GetBytes(content);
            File.WriteAllBytes(filePath, bytes); //避免写入BOM标记
        }

        /// <summary>
        /// 写入字节数据（会覆盖指定路径的文件）
        /// </summary>
        public static void WriteAllBytes(string filePath, byte[] data)
        {
            // 创建文件夹路径
            EnsureFileDirectory(filePath);

            File.WriteAllBytes(filePath, data);
        }

        /// <summary>
        /// 确保文件的父目录存在，如不存在则创建
        /// </summary>
        public static void EnsureFileDirectory(string filePath)
        {
            // 获取文件的文件夹路径
            string directory = Path.GetDirectoryName(filePath);
            EnsureDirectory(directory);
        }

        /// <summary>
        /// 确保目录存在，如不存在则创建
        /// </summary>
        public static void EnsureDirectory(string directory)
        {
            // If the directory doesn't exist, create it.
            if (Directory.Exists(directory) == false)
                Directory.CreateDirectory(directory);
        }

        /// <summary>
        /// 获取文件大小（字节数）
        /// </summary>
        public static long GetFileSize(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            return fileInfo.Length;
        }
    }
}