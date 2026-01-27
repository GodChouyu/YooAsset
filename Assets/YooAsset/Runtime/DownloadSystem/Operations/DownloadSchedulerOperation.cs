using System;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 下载调度器
    /// </summary>
    /// <remarks>
    /// 管理所有活跃的下载任务，控制并发数量。
    /// </remarks>
    internal class DownloadSchedulerOperation : AsyncOperationBase, IDisposable
    {
        public struct SchedulerConfig
        {
            public string SchedulerName { get; set; }
            public IDownloadBackend DownloadBackend { get; set; }
            public int MaxConcurrency { get; set; }
            public int MaxRequestPerFrame { get; set; }
        }

        private readonly Dictionary<string, DownloadFileBaseOperation> _downloaders = new Dictionary<string, DownloadFileBaseOperation>(1000);
        private readonly List<string> _removeList = new List<string>(1000);
        private readonly SchedulerConfig _config;

        /// <summary>
        /// 是否已暂停
        /// </summary>
        public bool Paused { get; private set; } = false;

        /// <summary>
        /// 当前活跃的下载任务数
        /// </summary>
        public int ActiveDownloadCount { get; private set; }

        /// <summary>
        /// 当前等待中的下载任务数
        /// </summary>
        public int PendingDownloadCount
        {
            get
            {
                return _downloaders.Count - ActiveDownloadCount;
            }
        }

        /// <summary>
        /// 构造下载中心
        /// </summary>
        public DownloadSchedulerOperation(SchedulerConfig config)
        {
            _config = config;
        }
        internal override void InternalStart()
        {
        }
        internal override void InternalUpdate()
        {
            // 驱动下载后台
            _config.DownloadBackend.Update();

            // 获取可移除的下载器集合
            _removeList.Clear();
            foreach (var valuePair in _downloaders)
            {
                var downloader = valuePair.Value;
                downloader.UpdateOperation();
                if (downloader.IsDone)
                {
                    _removeList.Add(valuePair.Key);
                    continue;
                }

                // 注意：主动终止引用计数为零的下载任务
                if (downloader.RefCount <= 0)
                {
                    _removeList.Add(valuePair.Key);
                    downloader.AbortOperation();
                    continue;
                }
            }

            // 移除下载器
            foreach (var key in _removeList)
            {
                if (_downloaders.TryGetValue(key, out var downloader))
                {
                    RemoveChildOperation(downloader);
                    _downloaders.Remove(key);
                }
            }

            // 暂停时不启动新任务
            if (Paused)
                return;

            // 最大并发数检测
            ActiveDownloadCount = GetProcessingOperationCount();
            if (ActiveDownloadCount != _downloaders.Count)
            {
                int maxConcurrency = _config.MaxConcurrency;
                int maxRequestPerFrame = _config.MaxRequestPerFrame;
                if (ActiveDownloadCount < maxConcurrency)
                {
                    int startCount = maxConcurrency - ActiveDownloadCount;
                    if (startCount > maxRequestPerFrame)
                        startCount = maxRequestPerFrame;

                    foreach (var operationPair in _downloaders)
                    {
                        var operation = operationPair.Value;
                        if (operation.Status == EOperationStatus.None)
                        {
                            operation.StartOperation();
                            startCount--;
                            if (startCount <= 0)
                                break;
                        }
                    }
                }
            }
        }
        internal override string InternalGetDescription()
        {
            return _config.SchedulerName;
        }

        /// <summary>
        /// 释放下载资源
        /// </summary>
        public void Dispose()
        {
            foreach (var valuePair in _downloaders)
            {
                var operation = valuePair.Value;
                operation.AbortOperation();
            }
            _downloaders.Clear();
        }

        /// <summary>
        /// 尝试获取已经存在的下载器
        /// </summary>
        public DownloadFileBaseOperation TryGetDownloadFile(PackageBundle bundle)
        {
            if (_downloaders.TryGetValue(bundle.BundleGUID, out var oldDownloader))
            {
                oldDownloader.Reference();
                return oldDownloader;
            }
            return null;
        }

        /// <summary>
        /// 添加新的下载器到调度中心
        /// </summary>
        public void AddDownloadFile(DownloadFileBaseOperation downloadFileOp)
        {
            string bundleGUID = downloadFileOp.Bundle.BundleGUID;
            if (_downloaders.ContainsKey(bundleGUID))
                throw new YooInternalException();

            AddChildOperation(downloadFileOp);
            _downloaders.Add(bundleGUID, downloadFileOp);
            downloadFileOp.Reference();
        }

        /// <summary>
        /// 获取正在进行中的下载器总数
        /// </summary>
        private int GetProcessingOperationCount()
        {
            int count = 0;
            foreach (var operationPair in _downloaders)
            {
                var operation = operationPair.Value;
                if (operation.Status != EOperationStatus.None)
                    count++;
            }
            return count;
        }
    }
}
