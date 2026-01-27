using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 默认的 AssetBundle 加载操作（非加密）
    /// 通用实现，适用于 WebRemoteFileSystem 和 WebServerFileSystem
    /// </summary>
    public class DefaultLoadWebAssetBundleOperation : LoadWebAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            CreateRequest,
            CheckRequest,
            TryAgain,
            Done,
        }

        private IDownloadAssetBundleRequest _downloadAssetBundleRequest;
        private ESteps _steps = ESteps.None;

        private int _requestCount = 0;
        private float _tryAgainTimer = 0;
        private int _failedTryAgain;

        public DefaultLoadWebAssetBundleOperation(LoadWebAssetBundleOptions opionts) : base(opionts)
        {
            _failedTryAgain = opionts.RetryCount;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CreateRequest;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            // 创建下载器
            if (_steps == ESteps.CreateRequest)
            {
                string url = GetRequestURL();
                var args = new DownloadAssetBundleRequestArgs(url, 0, _options.WatchdogTimeout, _options.DisableUnityWebCache, _options.Bundle.FileHash, _options.Bundle.UnityCRC);
                _downloadAssetBundleRequest = _options.DownloadBackend.CreateAssetBundleRequest(args);
                _downloadAssetBundleRequest.SendRequest();
                _steps = ESteps.CheckRequest;
            }

            // 检测下载结果
            if (_steps == ESteps.CheckRequest)
            {
                Progress = _downloadAssetBundleRequest.DownloadProgress;
                DownloadedBytes = _downloadAssetBundleRequest.DownloadedBytes;
                DownloadProgress = _downloadAssetBundleRequest.DownloadProgress;
                if (_downloadAssetBundleRequest.IsDone == false)
                    return;

                if (_downloadAssetBundleRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                    Result = _downloadAssetBundleRequest.Result;
                }
                else
                {
                    if (_failedTryAgain > 0)
                    {
                        _steps = ESteps.TryAgain;
                        YooLogger.Warning($"Failed download : {_downloadAssetBundleRequest.Url} Try again.");
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _downloadAssetBundleRequest.Error;
                        YooLogger.Error(Error);
                    }
                }

                // 最终释放请求器
                _downloadAssetBundleRequest.Dispose();
            }

            // 重新尝试下载
            if (_steps == ESteps.TryAgain)
            {
                _tryAgainTimer += Time.unscaledDeltaTime;
                if (_tryAgainTimer > 1f)
                {
                    _tryAgainTimer = 0f;
                    _failedTryAgain--;
                    Progress = 0f;
                    DownloadProgress = 0f;
                    DownloadedBytes = 0;
                    _steps = ESteps.CreateRequest;
                }
            }
        }

        /// <summary>
        /// 获取网络请求地址
        /// </summary>
        protected string GetRequestURL()
        {
            // 轮流返回请求地址
            _requestCount++;
            if (_requestCount % 2 == 0)
                return _options.FallbackURL;
            else
                return _options.MainURL;
        }
    }

    /// <summary>
    /// 默认的 AssetBundle 加载操作（加密）
    /// 通用实现，适用于 WebRemoteFileSystem 和 WebServerFileSystem
    /// </summary>
    public abstract class DefaultLoadWebAssetBundleFromMemoryOperation : LoadWebAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            CreateRequest,
            CheckRequest,
            TryAgain,
            Done,
        }

        private IDownloadBytesRequest _downloadBytesRequest;
        private ESteps _steps = ESteps.None;

        private int _requestCount = 0;
        private float _tryAgainTimer = 0;
        private int _failedTryAgain;

        public DefaultLoadWebAssetBundleFromMemoryOperation(LoadWebAssetBundleOptions opionts) : base(opionts)
        {
            _failedTryAgain = opionts.RetryCount;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CreateRequest;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            // 创建下载器
            if (_steps == ESteps.CreateRequest)
            {
                string url = GetRequestURL();
                var args = new DownloadDataRequestArgs(url, 0, _options.WatchdogTimeout);
                _downloadBytesRequest = _options.DownloadBackend.CreateBytesRequest(args);
                _downloadBytesRequest.SendRequest();
                _steps = ESteps.CheckRequest;
            }

            // 检测下载结果
            if (_steps == ESteps.CheckRequest)
            {
                Progress = _downloadBytesRequest.DownloadProgress;
                DownloadProgress = _downloadBytesRequest.DownloadProgress;
                DownloadedBytes = _downloadBytesRequest.DownloadedBytes;
                if (_downloadBytesRequest.IsDone == false)
                    return;

                // 检查网络错误
                if (_downloadBytesRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    var rawData = Decryption(_downloadBytesRequest.Result);
                    if (rawData == null || rawData.Length == 0)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "AssetBundle raw data is null or empty.";
                    }
                    else
                    {
                        AssetBundle assetBundle = AssetBundle.LoadFromMemory(rawData);
                        if (assetBundle == null)
                        {
                            _steps = ESteps.Done;
                            Status = EOperationStatus.Failed;
                            Error = $"Failed load encrypted AssetBundle: {_options.Bundle.BundleName}";
                        }
                        else
                        {
                            _steps = ESteps.Done;
                            Status = EOperationStatus.Succeeded;
                            Result = assetBundle;
                        }
                    }
                }
                else
                {
                    if (_failedTryAgain > 0)
                    {
                        _steps = ESteps.TryAgain;
                        YooLogger.Warning($"Failed download : {_downloadBytesRequest.Url} Try again.");
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _downloadBytesRequest.Error;
                        YooLogger.Error(Error);
                    }
                }

                // 最终释放请求器
                _downloadBytesRequest.Dispose();
            }

            // 重新尝试下载
            if (_steps == ESteps.TryAgain)
            {
                _tryAgainTimer += Time.unscaledDeltaTime;
                if (_tryAgainTimer > 1f)
                {
                    _tryAgainTimer = 0f;
                    _failedTryAgain--;
                    Progress = 0f;
                    DownloadProgress = 0f;
                    DownloadedBytes = 0;
                    _steps = ESteps.CreateRequest;
                }
            }
        }

        /// <summary>
        /// 文件数据解密
        /// </summary>
        protected abstract byte[] Decryption(byte[] data);

        /// 获取网络请求地址
        /// </summary>
        protected string GetRequestURL()
        {
            // 轮流返回请求地址
            _requestCount++;
            if (_requestCount % 2 == 0)
                return _options.FallbackURL;
            else
                return _options.MainURL;
        }
    }
}
