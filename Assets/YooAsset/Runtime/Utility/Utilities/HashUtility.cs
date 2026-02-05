using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace YooAsset
{
    /// <summary>
    /// 哈希工具类
    /// </summary>
    public static class HashUtility
    {
        private static string ToHexString(byte[] hashBytes)
        {
            string result = BitConverter.ToString(hashBytes);
            result = result.Replace("-", "");
            return result.ToLower();
        }

        #region SHA1
        /// <summary>
        /// 计算字符串的SHA1哈希值
        /// </summary>
        public static string ComputeSHA1(string str)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            return ComputeBytesSHA1(buffer);
        }

        /// <summary>
        /// 计算文件的SHA1哈希值
        /// </summary>
        public static string ComputeFileSHA1(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ComputeStreamSHA1(fs);
            }
        }

        /// <summary>
        /// 计算数据流的SHA1哈希值
        /// </summary>
        public static string ComputeStreamSHA1(Stream stream)
        {
            // 说明：创建的是SHA1类的实例，生成的是160位的散列码
            HashAlgorithm hash = HashAlgorithm.Create();
            byte[] hashBytes = hash.ComputeHash(stream);
            return ToHexString(hashBytes);
        }

        /// <summary>
        /// 计算字节数组的SHA1哈希值
        /// </summary>
        public static string ComputeBytesSHA1(byte[] buffer)
        {
            // 说明：创建的是SHA1类的实例，生成的是160位的散列码
            HashAlgorithm hash = HashAlgorithm.Create();
            byte[] hashBytes = hash.ComputeHash(buffer);
            return ToHexString(hashBytes);
        }
        #endregion

        #region MD5
        /// <summary>
        /// 获取字符串的MD5
        /// </summary>
        public static string ComputeMD5(string str)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            return ComputeBytesMD5(buffer);
        }

        /// <summary>
        /// 获取文件的MD5
        /// </summary>
        public static string ComputeFileMD5(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ComputeStreamMD5(fs);
            }
        }

        /// <summary>
        /// 获取数据流的MD5
        /// </summary>
        public static string ComputeStreamMD5(Stream stream)
        {
            MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
            byte[] hashBytes = provider.ComputeHash(stream);
            return ToHexString(hashBytes);
        }

        /// <summary>
        /// 获取字节数组的MD5
        /// </summary>
        public static string ComputeBytesMD5(byte[] buffer)
        {
            MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider();
            byte[] hashBytes = provider.ComputeHash(buffer);
            return ToHexString(hashBytes);
        }
        #endregion

        #region CRC32
        /// <summary>
        /// 获取字符串的CRC32
        /// </summary>
        public static string ComputeCRC32(string str)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            return ComputeBytesCRC32(buffer);
        }

        /// <summary>
        /// 计算字符串的CRC32值（返回无符号整数）
        /// </summary>
        public static uint ComputeCRC32AsUInt(string str)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(str);
            return ComputeBytesCRC32AsUInt(buffer);
        }

        /// <summary>
        /// 获取文件的CRC32
        /// </summary>
        public static string ComputeFileCRC32(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ComputeStreamCRC32(fs);
            }
        }

        /// <summary>
        /// 计算文件的CRC32值（返回无符号整数）
        /// </summary>
        public static uint ComputeFileCRC32AsUInt(string filePath)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ComputeStreamCRC32AsUInt(fs);
            }
        }

        /// <summary>
        /// 获取数据流的CRC32
        /// </summary>
        public static string ComputeStreamCRC32(Stream stream)
        {
            CRC32Algorithm hash = new CRC32Algorithm();
            byte[] hashBytes = hash.ComputeHash(stream);
            return ToHexString(hashBytes);
        }

        /// <summary>
        /// 计算数据流的CRC32值（返回无符号整数）
        /// </summary>
        public static uint ComputeStreamCRC32AsUInt(Stream stream)
        {
            CRC32Algorithm hash = new CRC32Algorithm();
            hash.ComputeHash(stream);
            return hash.Crc32Value;
        }

        /// <summary>
        /// 获取字节数组的CRC32
        /// </summary>
        public static string ComputeBytesCRC32(byte[] buffer)
        {
            CRC32Algorithm hash = new CRC32Algorithm();
            byte[] hashBytes = hash.ComputeHash(buffer);
            return ToHexString(hashBytes);
        }

        /// <summary>
        /// 计算字节数组的CRC32值（返回无符号整数）
        /// </summary>
        public static uint ComputeBytesCRC32AsUInt(byte[] buffer)
        {
            CRC32Algorithm hash = new CRC32Algorithm();
            hash.ComputeHash(buffer);
            return hash.Crc32Value;
        }
        #endregion
    }
}