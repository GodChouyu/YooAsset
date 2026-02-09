using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 从网络加载 AssetBundle 操作的抽象基类
    /// </summary>
    internal abstract class LoadWebAssetBundleOperation : FCLoadBundleOperation
    {
    }

    /// <summary>
    /// 从网络加载未加密 AssetBundle 操作
    /// </summary>
    internal class LoadWebNormalAssetBundleOperation : LoadWebAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            BundleRequest,
            CheckRequest,
            TryAgain,
            Done,
        }

        protected readonly LoadWebAssetBundleOptions _options;
        private readonly DownloadRetry _downloadRetry;
        private IDownloadAssetBundleRequest _downloadAssetBundleRequest;
        private ESteps _steps = ESteps.None;

        public LoadWebNormalAssetBundleOperation(LoadWebAssetBundleOptions options)
        {
            _options = options;

            // 注意：网络原因失败后，重新尝试直到成功
            _downloadRetry = new DownloadRetry(int.MaxValue, options.RetryPolicy);
        }
        internal override void InternalStart()
        {
            _steps = ESteps.BundleRequest;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.BundleRequest)
            {
                string url = GetRequestURL();
                var args = new DownloadAssetBundleRequestArgs(url, 0, _options.WatchdogTimeout, _options.DisableUnityWebCache, _options.Bundle.FileHash, _options.Bundle.UnityCRC);
                _downloadAssetBundleRequest = _options.DownloadBackend.CreateAssetBundleRequest(args);
                _downloadAssetBundleRequest.SendRequest();
                _steps = ESteps.CheckRequest;
            }

            if (_steps == ESteps.CheckRequest)
            {
                Progress = _downloadAssetBundleRequest.DownloadProgress;
                if (_downloadAssetBundleRequest.IsDone == false)
                    return;

                if (_downloadAssetBundleRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    _options.URLPolicy.OnSuccess(_downloadAssetBundleRequest.Url);
                    var assetBundle = _downloadAssetBundleRequest.Result;
                    if (assetBundle == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Fatal error: downloaded asset bundle is null.";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                        BundleHandle = new AssetBundleHandle(_downloadAssetBundleRequest.Url, _options.Bundle, assetBundle, null);
                    }
                }
                else
                {
                    string url = _downloadAssetBundleRequest.Url;
                    long httpCode = _downloadAssetBundleRequest.HttpCode;
                    string httpError = _downloadAssetBundleRequest.HttpError;
                    _options.URLPolicy.OnFailure(url, httpCode, httpError);
                    if (IsWaitForCompletion == false && _downloadRetry.CanRetry(url, httpCode, httpError))
                    {
                        _downloadRetry.BeginWait();
                        _steps = ESteps.TryAgain;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _downloadAssetBundleRequest.Error;
                    }
                }
            }

            if (_steps == ESteps.TryAgain)
            {
                // 注意：失败后释放网络请求
                if (_downloadAssetBundleRequest != null)
                {
                    _downloadAssetBundleRequest.Dispose();
                    _downloadAssetBundleRequest = null;
                }

                if (_downloadRetry.Tick())
                {
                    Progress = 0f;
                    _steps = ESteps.BundleRequest;
                }
            }
        }
        internal override void InternalDispose()
        {
            if (_downloadAssetBundleRequest != null)
            {
                _downloadAssetBundleRequest.Dispose();
                _downloadAssetBundleRequest = null;
            }
        }

        private string GetRequestURL()
        {
            return _options.URLPolicy.SelectURL(_options.CandidateURLs);
        }
    }

    /// <summary>
    /// 从网络加载加密的 AssetBundle 操作
    /// </summary>
    internal class LoadWebEncryptedAssetBundleOperation : LoadWebAssetBundleOperation
    {
        private enum ESteps
        {
            None,
            DataRequest,
            CheckRequest,
            VerifyData,
            LoadBundle,
            CheckResult,
            TryAgain,
            Done,
        }

        protected readonly LoadWebAssetBundleOptions _options;
        private readonly DownloadRetry _downloadRetry;
        private IDownloadBytesRequest _downloadBytesRequest;
        private IBundleMemoryDecryptor _decryptor;
        private AssetBundleCreateRequest _createRequest;
        private ESteps _steps = ESteps.None;

        public LoadWebEncryptedAssetBundleOperation(LoadWebAssetBundleOptions options)
        {
            _options = options;

            // 注意：网络原因失败后，重新尝试直到成功
            _downloadRetry = new DownloadRetry(int.MaxValue, options.RetryPolicy);
        }
        internal override void InternalStart()
        {
            _steps = ESteps.DataRequest;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.DataRequest)
            {
                var decryptor = _options.AssetBundleDecryptor;
                if (decryptor == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"{_options.CacheName} decryptor is null.";
                    return;
                }

                if (decryptor is IBundleMemoryDecryptor)
                {
                    _decryptor = decryptor as IBundleMemoryDecryptor;
                    string url = GetRequestURL();
                    var args = new DownloadDataRequestArgs(url, 0, _options.WatchdogTimeout);
                    _downloadBytesRequest = _options.DownloadBackend.CreateBytesRequest(args);
                    _downloadBytesRequest.SendRequest();
                    _steps = ESteps.CheckRequest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"{_options.CacheName} does not support {decryptor.GetType().Name}";
                    return;
                }
            }

            if (_steps == ESteps.CheckRequest)
            {
                Progress = _downloadBytesRequest.DownloadProgress;
                if (_downloadBytesRequest.IsDone == false)
                    return;

                if (_downloadBytesRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    _options.URLPolicy.OnSuccess(_downloadBytesRequest.Url);
                    _steps = ESteps.VerifyData;
                }
                else
                {
                    string url = _downloadBytesRequest.Url;
                    long httpCode = _downloadBytesRequest.HttpCode;
                    string httpError = _downloadBytesRequest.HttpError;
                    _options.URLPolicy.OnFailure(url, httpCode, httpError);
                    if (IsWaitForCompletion == false && _downloadRetry.CanRetry(url, httpCode, httpError))
                    {
                        _downloadRetry.BeginWait();
                        _steps = ESteps.TryAgain;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = _downloadBytesRequest.Error;
                    }
                }
            }

            if (_steps == ESteps.VerifyData)
            {
                // 注意：网络/代理/服务器异常导致内容不完整但请求仍成功
                EFileVerifyResult verifyResult;
                if (_options.DownloadVerifyLevel == EFileVerifyLevel.Low || _options.DownloadVerifyLevel == EFileVerifyLevel.Middle)
                    verifyResult = FileVerifyTools.FileVerify(_downloadBytesRequest.Result, _options.Bundle.FileSize, 0);
                else if (_options.DownloadVerifyLevel == EFileVerifyLevel.High)
                    verifyResult = FileVerifyTools.FileVerify(_downloadBytesRequest.Result, _options.Bundle.FileSize, _options.Bundle.FileCRC);
                else
                    throw new System.NotImplementedException(_options.DownloadVerifyLevel.ToString());

                if (verifyResult == EFileVerifyResult.Succeed)
                {
                    _steps = ESteps.LoadBundle;
                }
                else
                {
                    string error = $"[WebBundleVerify] Verify failed. Url:{_downloadBytesRequest.Url} Level: {_options.DownloadVerifyLevel} Result: {verifyResult}";
                    YooLogger.Warning(error);

                    if (IsWaitForCompletion == false && _downloadRetry.CanRetry())
                    {
                        _downloadRetry.BeginWait();
                        _steps = ESteps.TryAgain;
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = error;
                    }
                }
            }

            if (_steps == ESteps.LoadBundle)
            {
                LoadResult result = LoadFromMemory(_decryptor, _downloadBytesRequest.Result);
                if (result.Succeeded == false)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = result.Error;
                    return;
                }

                _steps = ESteps.CheckResult;
            }

            if (_steps == ESteps.CheckResult)
            {
                if (_createRequest.isDone == false)
                    return;

                var assetBundle = _createRequest.assetBundle;
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
                    BundleHandle = new AssetBundleHandle(_downloadBytesRequest.Url, _options.Bundle, assetBundle, null);
                }
            }

            if (_steps == ESteps.TryAgain)
            {
                // 注意：失败后释放网络请求
                if (_downloadBytesRequest != null)
                {
                    _downloadBytesRequest.Dispose();
                    _downloadBytesRequest = null;
                }

                if (_downloadRetry.Tick())
                {
                    Progress = 0f;
                    _steps = ESteps.DataRequest;
                }
            }
        }
        internal override void InternalDispose()
        {
            if (_downloadBytesRequest != null)
            {
                _downloadBytesRequest.Dispose();
                _downloadBytesRequest = null;
            }
        }

        private LoadResult LoadFromMemory(IBundleMemoryDecryptor decryptor, byte[] fileData)
        {
            var args = new BundleDecryptArgs();
            args.Bundle = _options.Bundle;
            args.FileData = fileData;
            var binaryData = decryptor.GetDecryptData(args);
            if (binaryData == null)
                return LoadResult.Failure($"{_options.CacheName} decryptor returned null data.");

            _createRequest = AssetBundle.LoadFromMemoryAsync(binaryData);
            return LoadResult.Default();
        }
        private string GetRequestURL()
        {
            return _options.URLPolicy.SelectURL(_options.CandidateURLs);
        }
    }
}
