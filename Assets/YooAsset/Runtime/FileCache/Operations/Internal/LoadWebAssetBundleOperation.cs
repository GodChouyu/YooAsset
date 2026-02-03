using UnityEngine;

namespace YooAsset
{
    internal abstract class LoadWebAssetBundleOperation : FCLoadBundleOperation
    {
    }

    internal class LoadWebNormalAssetBundleOperation : LoadWebAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            DownloadBundle,
            CheckResult,
            TryAgain,
            Done,
        }

        protected readonly LoadWebAssetBundleOptions _options;
        private IDownloadAssetBundleRequest _downloadAssetBundleRequest;
        private ESteps _steps = ESteps.None;

        // 失败重试
        private int _requestCount = 0;
        private float _tryAgainTimer = 0;
        private int _failedTryAgain;

        public LoadWebNormalAssetBundleOperation(LoadWebAssetBundleOptions options)
        {
            _options = options;
            _failedTryAgain = int.MaxValue; //注意：网络原因失败后，重新尝试直到成功
        }
        internal override void InternalStart()
        {
            _steps = ESteps.DownloadBundle;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.DownloadBundle)
            {
                string url = GetRequestURL();
                var args = new DownloadAssetBundleRequestArgs(url, 0, _options.WatchdogTimeout, _options.DisableUnityWebCache, _options.Bundle.FileHash, _options.Bundle.UnityCRC);
                _downloadAssetBundleRequest = _options.DownloadBackend.CreateAssetBundleRequest(args);
                _downloadAssetBundleRequest.SendRequest();
                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                Progress = _downloadAssetBundleRequest.DownloadProgress;
                if (_downloadAssetBundleRequest.IsDone == false)
                    return;

                if (_downloadAssetBundleRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    var assetBundle = _downloadAssetBundleRequest.Result;
                    if (assetBundle == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Fatal error: dwonload asset bundle is null.";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        BundleResult = new AssetBundleResult(_downloadAssetBundleRequest.Url, _options.Bundle, assetBundle, null);
                    }
                }
                else
                {
                    if (_failedTryAgain > 0)
                    {
                        _steps = ESteps.TryAgain;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _downloadAssetBundleRequest.Error;
                    }
                }

                // 最终释放请求器
                _downloadAssetBundleRequest.Dispose();
            }

            if (_steps == ESteps.TryAgain)
            {
                _tryAgainTimer += UnityEngine.Time.unscaledDeltaTime;
                if (_tryAgainTimer > 1f)
                {
                    _tryAgainTimer = 0f;
                    _failedTryAgain--;
                    Progress = 0f;
                    _steps = ESteps.DownloadBundle;
                }
            }
        }

        private string GetRequestURL()
        {
            // 轮流返回请求地址
            _requestCount++;
            if (_requestCount % 2 == 0)
                return _options.FallbackURL;
            else
                return _options.MainURL;
        }
    }

    internal class LoadWebEncryptedAssetBundleOperation : LoadWebAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            DownloadData,
            CheckResult,
            TryAgain,
            Done,
        }

        protected readonly LoadWebAssetBundleOptions _options;
        private IDownloadBytesRequest _downloadBytesRequest;
        private IBundleMemoryDecryptor _decryptor;
        private ESteps _steps = ESteps.None;

        // 失败重试
        private int _requestCount = 0;
        private float _tryAgainTimer = 0;
        private int _failedTryAgain;

        public LoadWebEncryptedAssetBundleOperation(LoadWebAssetBundleOptions options)
        {
            _options = options;
            _failedTryAgain = int.MaxValue; //注意：网络原因失败后，重新尝试直到成功
        }
        internal override void InternalStart()
        {
            _steps = ESteps.DownloadData;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.DownloadData)
            {
                var decryptor = _options.Decryptor;
                if (decryptor == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"{_options.CacheName} decryptor is null.";
                    return;
                }

                if (decryptor is IBundleMemoryDecryptor)
                {
                    string url = GetRequestURL();
                    _decryptor = decryptor as IBundleMemoryDecryptor;
                    var args = new DownloadDataRequestArgs(url, 0, _options.WatchdogTimeout);
                    _downloadBytesRequest = _options.DownloadBackend.CreateBytesRequest(args);
                    _downloadBytesRequest.SendRequest();
                    _steps = ESteps.CheckResult;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"{_options.CacheName} not support {decryptor.GetType().Name}";
                    return;
                }
            }

            if (_steps == ESteps.CheckResult)
            {
                Progress = _downloadBytesRequest.DownloadProgress;
                if (_downloadBytesRequest.IsDone == false)
                    return;

                // 检查网络错误
                if (_downloadBytesRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    var assetBundle = LoadFromMemory(_decryptor, _downloadBytesRequest.Result);
                    if (assetBundle == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "Unity engine load failed.";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        BundleResult = new AssetBundleResult(_downloadBytesRequest.Url, _options.Bundle, assetBundle, null);
                    }
                }
                else
                {
                    if (_failedTryAgain > 0)
                    {
                        _steps = ESteps.TryAgain;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _downloadBytesRequest.Error;
                    }
                }

                // 最终释放请求器
                _downloadBytesRequest.Dispose();
            }

            if (_steps == ESteps.TryAgain)
            {
                _tryAgainTimer += Time.unscaledDeltaTime;
                if (_tryAgainTimer > 1f)
                {
                    _tryAgainTimer = 0f;
                    _failedTryAgain--;
                    Progress = 0f;
                    _steps = ESteps.DownloadData;
                }
            }
        }

        private AssetBundle LoadFromMemory(IBundleMemoryDecryptor decryptor, byte[] fileData)
        {
            var args = new BundleDecryptArgs();
            args.Bundle = _options.Bundle;
            args.FileData = fileData;
            var binaryData = decryptor.GetDecryptData(args);
            return AssetBundle.LoadFromMemory(binaryData);
        }
        private string GetRequestURL()
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
