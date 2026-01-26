using System.IO;

namespace YooAsset
{
    internal class LoadBuiltinPackageManifestOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            TryLoadFileData,
            RequestFileData,
            VerifyFileData,
            LoadManifest,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private readonly string _packageVersion;
        private readonly string _packageHash;
        private IDownloadBytesRequest _webDataRequestOp;
        private DeserializeManifestOperation _deserializer;
        private byte[] _fileData;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹清单
        /// </summary>
        public PackageManifest Manifest { private set; get; }


        internal LoadBuiltinPackageManifestOperation(BuiltinFileSystem fileSystem, string packageVersion, string packageHash)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
            _packageHash = packageHash;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.TryLoadFileData;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.TryLoadFileData)
            {
                string filePath = _fileSystem.GetBuiltinPackageManifestFilePath(_packageVersion);
                if (File.Exists(filePath))
                {
                    _fileData = File.ReadAllBytes(filePath);
                    _steps = ESteps.VerifyFileData;
                }
                else
                {
                    _steps = ESteps.RequestFileData;
                }
            }

            if (_steps == ESteps.RequestFileData)
            {
                if (_webDataRequestOp == null)
                {
                    string filePath = _fileSystem.GetBuiltinPackageManifestFilePath(_packageVersion);
                    string url = DownloadSystemTools.ToLocalURL(filePath);
                    var args = new DownloadDataRequestArgs(url, 60, 0);
                    _webDataRequestOp = _fileSystem.DownloadBackend.CreateBytesRequest(args);
                    _webDataRequestOp.SendRequest();
                }

                if (_webDataRequestOp.IsDone == false)
                    return;

                if (_webDataRequestOp.Status == EDownloadRequestStatus.Succeed)
                {
                    _fileData = _webDataRequestOp.Result;
                    _steps = ESteps.VerifyFileData;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _webDataRequestOp.Error;
                }
            }

            if (_steps == ESteps.VerifyFileData)
            {
                if (PackageManifestTools.VerifyManifestData(_fileData, _packageHash))
                {
                    _steps = ESteps.LoadManifest;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Failed to verify buildin package manifest file.";
                }
            }

            if (_steps == ESteps.LoadManifest)
            {
                if (_deserializer == null)
                {
                    _deserializer = new DeserializeManifestOperation(_fileSystem.ManifestRestoreServices, _fileData);
                    _deserializer.StartOperation();
                    AddChildOperation(_deserializer);
                }

                _deserializer.UpdateOperation();
                Progress = _deserializer.Progress;
                if (_deserializer.IsDone == false)
                    return;

                if (_deserializer.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Manifest = _deserializer.Manifest;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _deserializer.Error;
                }
            }
        }
        internal override string InternalGetDescription()
        {
            return $"PackageVersion : {_packageVersion} PackageHash : {_packageHash}";
        }
    }
}