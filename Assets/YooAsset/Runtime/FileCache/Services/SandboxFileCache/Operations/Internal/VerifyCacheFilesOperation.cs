using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace YooAsset
{
    /// <summary>
    /// 缓存文件验证（线程版）
    /// </summary>
    internal sealed class VerifyCacheFilesOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            InitVerify,
            UpdateVerify,
            Done,
        }

        private readonly SandboxFileCache _cache;
        private readonly EFileVerifyLevel _verifyLevel;
        private readonly int _fileVerifyMaxConcurrency;
        private readonly List<VerifyFileInfo> _waitingList;
        private List<VerifyFileInfo> _verifyingList;
        private int _verifyMaxNum;
        private int _verifyTotalCount;
        private double _verifyStartTime;
        private int _succeedCount;
        private int _failedCount;
        private ESteps _steps = ESteps.None;


        internal VerifyCacheFilesOperation(SandboxFileCache cache, EFileVerifyLevel verifyLevel, int fileVerifyMaxConcurrency, List<VerifyFileInfo> elements)
        {
            _cache = cache;
            _verifyLevel = verifyLevel;
            _fileVerifyMaxConcurrency = fileVerifyMaxConcurrency;
            _waitingList = elements;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.InitVerify;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.InitVerify)
            {
                // 设置同时验证的最大数
                int processorCount = Environment.ProcessorCount * 2 + 1;
                _verifyMaxNum = Math.Min(processorCount, _fileVerifyMaxConcurrency);
                if (_verifyMaxNum < 1)
                    _verifyMaxNum = 1;

                YooLogger.Log($"Verify max concurrency : {_verifyMaxNum}");
                _verifyingList = new List<VerifyFileInfo>(_verifyMaxNum);
                _verifyStartTime = TimeUtility.RealtimeSinceStartup;
                _verifyTotalCount = _waitingList.Count;
                _steps = ESteps.UpdateVerify;
            }

            if (_steps == ESteps.UpdateVerify)
            {
                // 检测校验结果
                for (int i = _verifyingList.Count - 1; i >= 0; i--)
                {
                    var verifyElement = _verifyingList[i];
                    int result = verifyElement.Result;
                    if (result != 0)
                    {
                        _verifyingList.RemoveAt(i);
                        if (verifyElement.Result == (int)EFileVerifyResult.Succeed)
                        {
                            _succeedCount++;
                            var cacheEntry = new SandboxFileCacheEntry(verifyElement.BundleGUID, verifyElement.InfoFilePath, verifyElement.DataFilePath);
                            _cache.AddEntry(verifyElement.BundleGUID, cacheEntry);
                        }
                        else
                        {
                            _failedCount++;
                            YooLogger.Warning($"Failed to verify file {verifyElement.Result} and delete files : {verifyElement.FolderPath}");
                            verifyElement.DeleteFiles();
                        }
                    }
                }

                Progress = GetProgress();
                if (_waitingList.Count == 0 && _verifyingList.Count == 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    double costTime = TimeUtility.RealtimeSinceStartup - _verifyStartTime;
                    YooLogger.Log($"Verify cache files elapsed time {costTime:f1} seconds");
                }

                for (int i = _waitingList.Count - 1; i >= 0; i--)
                {
                    if (IsBusy)
                        break;

                    if (_verifyingList.Count >= _verifyMaxNum)
                        break;

                    var element = _waitingList[i];
                    bool succeed = ThreadPool.QueueUserWorkItem(new WaitCallback(VerifyFileInThread), element);
                    if (succeed == false)
                        VerifyFileInThread(element);

                    _waitingList.RemoveAt(i);
                    _verifyingList.Add(element);
                }
            }
        }
        private float GetProgress()
        {
            if (_verifyTotalCount == 0)
                return 1f;
            return (float)(_succeedCount + _failedCount) / _verifyTotalCount;
        }

        // 验证缓存文件（子线程内操作）
        private void VerifyFileInThread(object obj)
        {
            VerifyFileInfo element = (VerifyFileInfo)obj;
            int verifyResult = (int)VerifyFile(element, _verifyLevel);
            element.Result = verifyResult;
        }
        private EFileVerifyResult VerifyFile(VerifyFileInfo element, EFileVerifyLevel verifyLevel)
        {
            try
            {
                if (File.Exists(element.InfoFilePath) == false)
                    return EFileVerifyResult.InfoFileNotExisted;
                if (File.Exists(element.DataFilePath) == false)
                    return EFileVerifyResult.DataFileNotExisted;

                if (verifyLevel == EFileVerifyLevel.Low)
                {
                    return EFileVerifyResult.Succeed;
                }
                else
                {
                    // 解析信息文件填充验证数据
                    // 注意：验证数据在后续流程会被使用。
                    byte[] binaryData = FileUtility.ReadAllBytes(element.InfoFilePath);
                    BufferReader buffer = new BufferReader(binaryData);
                    uint dataFileCRC = buffer.ReadUInt32();
                    long dataFileSize = buffer.ReadInt64();

                    if (verifyLevel == EFileVerifyLevel.Middle)
                        return FileVerifyTools.FileVerify(element.DataFilePath, dataFileSize, 0);
                    else if (verifyLevel == EFileVerifyLevel.High)
                        return FileVerifyTools.FileVerify(element.DataFilePath, dataFileSize, dataFileCRC);
                    else
                        throw new System.NotImplementedException(verifyLevel.ToString());
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