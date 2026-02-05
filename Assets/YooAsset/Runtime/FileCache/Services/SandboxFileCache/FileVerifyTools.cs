using System;
using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 文件校验工具类
    /// </summary>
    internal class FileVerifyTools
    {
        /// <summary>
        /// 校验文件完整性
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="fileSize">期望的文件大小</param>
        /// <param name="fileCRC">期望的文件CRC值</param>
        /// <returns>校验结果</returns>
        public static EFileVerifyResult FileVerify(string filePath, long fileSize, uint fileCRC)
        {
            try
            {
                if (File.Exists(filePath) == false)
                    return EFileVerifyResult.DataFileNotExisted;

                // 可选条件：验证文件大小
                if (fileSize > 0)
                {
                    long size = FileUtility.GetFileSize(filePath);
                    if (size < fileSize)
                        return EFileVerifyResult.FileNotComplete;
                    else if (size > fileSize)
                        return EFileVerifyResult.FileOverflow;
                }

                // 可选条件：验证文件CRC
                if (fileCRC > 0)
                {
                    uint crc = HashUtility.ComputeFileCRC32AsUInt(filePath);
                    if (crc == fileCRC)
                        return EFileVerifyResult.Succeed;
                    else
                        return EFileVerifyResult.FileCrcError;
                }
                else
                {
                    return EFileVerifyResult.Succeed;
                }
            }
            catch (Exception ex)
            {
                YooLogger.Error($"File verification exception: {ex.Message}");
                return EFileVerifyResult.Exception;
            }
        }

        /// <summary>
        /// 校验文件完整性
        /// </summary>
        /// <param name="fileData">文件数据</param>
        /// <param name="fileSize">期望的文件大小</param>
        /// <param name="fileCRC">期望的文件CRC值</param>
        /// <returns>校验结果</returns>
        public static EFileVerifyResult FileVerify(byte[] fileData, long fileSize, uint fileCRC)
        {
            try
            {
                if (fileData == null || fileData.Length == 0)
                    return EFileVerifyResult.BytesDataInvalid;

                // 可选条件：验证文件大小
                if (fileSize > 0)
                {
                    long size = fileData.Length;
                    if (size < fileSize)
                        return EFileVerifyResult.FileNotComplete;
                    else if (size > fileSize)
                        return EFileVerifyResult.FileOverflow;
                }

                // 可选条件：验证文件CRC
                if (fileCRC > 0)
                {
                    uint crc = HashUtility.ComputeBytesCRC32AsUInt(fileData);
                    if (crc == fileCRC)
                        return EFileVerifyResult.Succeed;
                    else
                        return EFileVerifyResult.FileCrcError;
                }
                else
                {
                    return EFileVerifyResult.Succeed;
                }
            }
            catch (Exception ex)
            {
                YooLogger.Error($"File verification exception: {ex.Message}");
                return EFileVerifyResult.Exception;
            }
        }
    }
}