using System;

namespace YooAsset
{
    internal sealed class LoadWebServerCatalogOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            RequestFileData,
            LoadCatalog,
            CheckResut,
            Done,
        }

        private readonly WebServerFileCache _fileCache;
        private IDownloadBytesRequest _webDataRequestOp;
        private byte[] _fileData;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 内置资源目录
        /// </summary>
        public BuiltinFileCatalog Catalog;

        internal LoadWebServerCatalogOperation(WebServerFileCache fileCache)
        {
            _fileCache = fileCache;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.RequestFileData;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.RequestFileData)
            {
                if (_webDataRequestOp == null)
                {
                    string filePath = _fileCache.GetCatalogBinaryFileLoadPath();
                    string url = DownloadSystemTools.ToLocalUrl(filePath);
                    var args = new DownloadDataRequestArgs(url, 60, 0);
                    _webDataRequestOp = _fileCache.Config.DownloadBackend.CreateBytesRequest(args);
                    _webDataRequestOp.SendRequest();
                }

                if (_webDataRequestOp.IsDone == false)
                    return;

                if (_webDataRequestOp.Status == EDownloadRequestStatus.Succeeded)
                {
                    _fileData = _webDataRequestOp.Result;
                    _steps = ESteps.LoadCatalog;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _webDataRequestOp.Error;
                }
            }

            if (_steps == ESteps.LoadCatalog)
            {
                try
                {
                    Catalog = BuiltinFileCatalogTools.DeserializeFromBinary(_fileData);
                    _steps = ESteps.CheckResut;
                }
                catch (Exception ex)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Failed to load catalog file : {ex.Message}";
                }
            }

            if (_steps == ESteps.CheckResut)
            {
                if (Catalog.PackageName != _fileCache.PackageName)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Catalog file package name {Catalog.PackageName} cannot match the file cache package name {_fileCache.PackageName}";
                    return;
                }

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
        }
    }
}