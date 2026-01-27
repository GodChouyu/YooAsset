using System;
using System.IO;

namespace YooAsset
{
    internal class FileVerifyTools
    {
        /// <summary>
        /// 文件校验
        /// </summary>
        public static EFileVerifyResult FileVerify(string filePath, long fileSize, uint fileCRC)
        {
            try
            {
                if (File.Exists(filePath) == false)
                    return EFileVerifyResult.DataFileNotExisted;

                // 验证文件大小
                if (fileSize > 0)
                {
                    long size = FileUtility.GetFileSize(filePath);
                    if (size < fileSize)
                        return EFileVerifyResult.FileNotComplete;
                    else if (size > fileSize)
                        return EFileVerifyResult.FileOverflow;
                }

                // 验证文件CRC
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
                YooLogger.Error($"File verify exception : {ex.Message}");
                return EFileVerifyResult.Exception;
            }
        }
    }
}