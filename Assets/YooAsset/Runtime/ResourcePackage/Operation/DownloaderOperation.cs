using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    public abstract class DownloaderOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            Check,
            Downloading,
            Finish,
            Done,
        }

        private const int MAX_LOADER_COUNT = 64;
        private readonly string _packageName;
        private readonly int _maximumConcurrency;
        private readonly int _failedTryAgain;
        private readonly List<BundleInfo> _bundleInfoList;
        private readonly List<FSDownloadFileOperation> _downloaders = new List<FSDownloadFileOperation>(MAX_LOADER_COUNT);
        private readonly List<FSDownloadFileOperation> _removeList = new List<FSDownloadFileOperation>(MAX_LOADER_COUNT);
        private readonly List<FSDownloadFileOperation> _failedList = new List<FSDownloadFileOperation>(MAX_LOADER_COUNT);

        // 数据相关
        private bool _isPause = false;
        private long _lastDownloadBytes = 0;
        private int _lastDownloadCount = 0;
        private long _cachedDownloadBytes = 0;
        private int _cachedDownloadCount = 0;
        private ESteps _steps = ESteps.None;


        /// <summary>
        /// 统计的下载文件总数量
        /// </summary>
        public int TotalDownloadCount { get; private set; }

        /// <summary>
        /// 统计的下载文件的总大小
        /// </summary>
        public long TotalDownloadBytes { get; private set; }

        /// <summary>
        /// 当前已经完成的下载总数量
        /// </summary>
        public int CurrentDownloadCount
        {
            get { return _lastDownloadCount; }
        }

        /// <summary>
        /// 当前已经完成的下载总大小
        /// </summary>
        public long CurrentDownloadBytes
        {
            get { return _lastDownloadBytes; }
        }

        /// <summary>
        /// 下载完成事件委托（无论成功或失败）
        /// </summary>
        public DownloadFinishedEventHandler DownloadFinishedHandler { get; set; }

        /// <summary>
        /// 下载进度更新事件委托
        /// </summary>
        public DownloadProgressChangedEventHandler DownloadProgressChangedHandler { get; set; }

        /// <summary>
        /// 下载错误事件委托
        /// </summary>
        public DownloadErrorEventHandler DownloadErrorHandler { get; set; }

        /// <summary>
        /// 开始下载单个文件事件委托
        /// </summary>
        public DownloadFileStartedEventHandler DownloadFileStartedHandler { get; set; }


        internal DownloaderOperation(string packageName, List<BundleInfo> downloadList, int maximumConcurrency, int failedTryAgain)
        {
            _packageName = packageName;
            _bundleInfoList = downloadList;
            _maximumConcurrency = UnityEngine.Mathf.Clamp(maximumConcurrency, 1, MAX_LOADER_COUNT);
            _failedTryAgain = failedTryAgain;

            // 统计下载信息
            CalculateDownloaderInfo();
        }
        internal override void InternalStart()
        {
            YooLogger.Log($"Begin to download {TotalDownloadCount} files and {TotalDownloadBytes} bytes");
            _steps = ESteps.Check;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.Check)
            {
                if (_bundleInfoList == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Download bundle list is null.";

                    if (DownloadFinishedHandler != null)
                    {
                        var args = DownloadFinishedEventArgs.CreateFailure(_packageName, Error);
                        DownloadFinishedHandler.Invoke(args);
                    }
                }
                else if (_bundleInfoList.Count == 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                    Progress = 1f;

                    if (DownloadFinishedHandler != null)
                    {
                        var args = DownloadFinishedEventArgs.CreateSuccess(_packageName);
                        DownloadFinishedHandler.Invoke(args);
                    }
                }
                else
                {
                    _steps = ESteps.Downloading;
                }
            }

            if (_steps == ESteps.Downloading)
            {
                // 检测下载器结果
                _removeList.Clear();
                long downloadBytes = _cachedDownloadBytes;
                foreach (var downloader in _downloaders)
                {
                    downloader.UpdateOperation();
                    downloadBytes += downloader.DownloadedBytes;
                    if (downloader.IsDone == false)
                        continue;

                    // 检测是否下载失败
                    if (downloader.Status != EOperationStatus.Succeed)
                    {
                        _removeList.Add(downloader);
                        _failedList.Add(downloader);
                        continue;
                    }

                    // 下载成功
                    _removeList.Add(downloader);
                    _cachedDownloadCount++;
                    _cachedDownloadBytes += downloader.DownloadedBytes;
                }

                // 移除已经完成的下载器（无论成功或失败）
                foreach (var downloader in _removeList)
                {
                    _downloaders.Remove(downloader);
                }

                // 如果下载进度发生变化
                if (_lastDownloadBytes != downloadBytes || _lastDownloadCount != _cachedDownloadCount)
                {
                    _lastDownloadBytes = downloadBytes;
                    _lastDownloadCount = _cachedDownloadCount;
                    Progress = CalculateProgress();

                    if (DownloadProgressChangedHandler != null)
                    {
                        var data = new DownloadProgressChangedEventArgs();
                        data.PackageName = _packageName;
                        data.Progress = Progress;
                        data.TotalDownloadCount = TotalDownloadCount;
                        data.TotalDownloadBytes = TotalDownloadBytes;
                        data.CurrentDownloadCount = _lastDownloadCount;
                        data.CurrentDownloadBytes = _lastDownloadBytes;
                        DownloadProgressChangedHandler.Invoke(data);
                    }
                }

                // 动态创建新的下载器到最大数量限制
                // 注意：如果期间有下载失败的文件，暂停动态创建下载器
                if (_bundleInfoList.Count > 0 && _failedList.Count == 0)
                {
                    if (_isPause)
                        return;

                    if (_downloaders.Count < _maximumConcurrency)
                    {
                        int index = _bundleInfoList.Count - 1;
                        var bundleInfo = _bundleInfoList[index];
                        var downloader = bundleInfo.CreateBundleDownloader(_failedTryAgain);
                        downloader.StartOperation();
                        this.AddChildOperation(downloader);

                        _downloaders.Add(downloader);
                        _bundleInfoList.RemoveAt(index);

                        if (DownloadFileStartedHandler != null)
                        {
                            var data = new DownloadFileStartedEventArgs();
                            data.PackageName = _packageName;
                            data.BundleName = bundleInfo.Bundle.BundleName;
                            data.FileName = bundleInfo.Bundle.FileName;
                            data.FileSize = bundleInfo.Bundle.FileSize;
                            DownloadFileStartedHandler.Invoke(data);
                        }
                    }
                }

                // 下载结束
                if (_downloaders.Count == 0)
                {
                    _steps = ESteps.Finish;
                }
            }

            if (_steps == ESteps.Finish)
            {
                if (_failedList.Count > 0)
                {
                    var failedDownloader = _failedList[0];
                    string bundleName = failedDownloader.Bundle.BundleName;
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to download file : {bundleName}";

                    if (DownloadErrorHandler != null)
                    {
                        var data = new DownloadErrorEventArgs();
                        data.PackageName = _packageName;
                        data.FileName = bundleName;
                        data.ErrorInfo = failedDownloader.Error;
                        DownloadErrorHandler.Invoke(data);
                    }

                    if (DownloadFinishedHandler != null)
                    {
                        var args = DownloadFinishedEventArgs.CreateFailure(_packageName, failedDownloader.Error);
                        DownloadFinishedHandler.Invoke(args);
                    }
                }
                else
                {
                    // 结算成功
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;

                    if (DownloadFinishedHandler != null)
                    {
                        var args = DownloadFinishedEventArgs.CreateSuccess(_packageName);
                        DownloadFinishedHandler.Invoke(args);
                    }
                }
            }
        }
        private void CalculateDownloaderInfo()
        {
            if (_bundleInfoList != null)
            {
                TotalDownloadBytes = 0;
                TotalDownloadCount = _bundleInfoList.Count;
                foreach (var packageBundle in _bundleInfoList)
                {
                    TotalDownloadBytes += packageBundle.Bundle.FileSize;
                }
            }
            else
            {
                TotalDownloadBytes = 0;
                TotalDownloadCount = 0;
            }
        }

        private float CalculateProgress()
        {
            if (TotalDownloadBytes == 0)
                return (float)_lastDownloadCount / TotalDownloadCount;
            else
                return (float)_lastDownloadBytes / TotalDownloadBytes;
        }

        /// <summary>
        /// 合并其它下载器
        /// </summary>
        /// <param name="downloader">合并的下载器</param>
        public void Combine(DownloaderOperation downloader)
        {
            if (_packageName != downloader._packageName)
            {
                YooLogger.Error("The downloaders have different resource packages.");
                return;
            }

            if (Status != EOperationStatus.None)
            {
                YooLogger.Error("The downloader is running, can not combine with other downloader.");
                return;
            }

            HashSet<string> combineGuidSet = new HashSet<string>();
            foreach (var bundleInfo in _bundleInfoList)
            {
                string combineGUID = bundleInfo.GetDownloadCombineGUID();
                if (combineGuidSet.Contains(combineGUID) == false)
                {
                    combineGuidSet.Add(combineGUID);
                }
            }

            // 合并下载列表
            foreach (var bundleInfo in downloader._bundleInfoList)
            {
                string combineGUID = bundleInfo.GetDownloadCombineGUID();
                if (combineGuidSet.Contains(combineGUID) == false)
                {
                    _bundleInfoList.Add(bundleInfo);
                }
            }

            // 重新统计下载信息
            CalculateDownloaderInfo();
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        public void BeginDownload()
        {
            if (_steps == ESteps.None)
            {
                OperationSystem.StartOperation(_packageName, this);
            }
        }

        /// <summary>
        /// 暂停下载
        /// </summary>
        public void PauseDownload()
        {
            _isPause = true;
        }

        /// <summary>
        /// 恢复下载
        /// </summary>
        public void ResumeDownload()
        {
            _isPause = false;
        }

        /// <summary>
        /// 取消下载
        /// </summary>
        public void CancelDownload()
        {
            if (_steps != ESteps.Done)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = "User cancel.";

                foreach (var downloader in _downloaders)
                {
                    downloader.AbortOperation();
                }
            }
        }
    }

    public sealed class ResourceDownloaderOperation : DownloaderOperation
    {
        internal ResourceDownloaderOperation(string packageName, List<BundleInfo> downloadList, int maximumConcurrency, int failedTryAgain)
            : base(packageName, downloadList, maximumConcurrency, failedTryAgain)
        {
        }

        /// <summary>
        /// 创建空的下载器
        /// </summary>
        internal static ResourceDownloaderOperation CreateEmptyDownloader(string packageName)
        {
            List<BundleInfo> downloadList = new List<BundleInfo>();
            var operation = new ResourceDownloaderOperation(packageName, downloadList, 1, 1);
            return operation;
        }
    }
    public sealed class ResourceUnpackerOperation : DownloaderOperation
    {
        internal ResourceUnpackerOperation(string packageName, List<BundleInfo> downloadList, int maximumConcurrency, int failedTryAgain)
            : base(packageName, downloadList, maximumConcurrency, failedTryAgain)
        {
        }

        /// <summary>
        /// 创建空的解压器
        /// </summary>
        internal static ResourceUnpackerOperation CreateEmptyUnpacker(string packageName)
        {
            List<BundleInfo> downloadList = new List<BundleInfo>();
            var operation = new ResourceUnpackerOperation(packageName, downloadList, 1, 1);
            return operation;
        }
    }
    public sealed class ResourceImporterOperation : DownloaderOperation
    {
        internal ResourceImporterOperation(string packageName, List<BundleInfo> downloadList, int maximumConcurrency, int failedTryAgain)
            : base(packageName, downloadList, maximumConcurrency, failedTryAgain)
        {
        }

        /// <summary>
        /// 创建空的导入器
        /// </summary>
        internal static ResourceImporterOperation CreateEmptyImporter(string packageName)
        {
            List<BundleInfo> downloadList = new List<BundleInfo>();
            var operation = new ResourceImporterOperation(packageName, downloadList, 1, 1);
            return operation;
        }
    }
}