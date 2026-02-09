using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 加载缓存包裹清单文件操作
    /// </summary>
    internal class LoadCachePackageManifestOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            LoadFileData,
            VerifyFileData,
            LoadManifest,
            Done,
        }

        private readonly SandboxFileSystem _fileSystem;
        private readonly string _packageVersion;
        private readonly string _packageHash;
        private DeserializeManifestOperation _deserializeManifestOp;
        private byte[] _fileData;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹清单
        /// </summary>
        public PackageManifest Manifest { get; private set; }


        internal LoadCachePackageManifestOperation(SandboxFileSystem fileSystem, string packageVersion, string packageHash)
        {
            _fileSystem = fileSystem;
            _packageVersion = packageVersion;
            _packageHash = packageHash;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadFileData;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadFileData)
            {
                string manifestFilePath = _fileSystem.GetCachePackageManifestFilePath(_packageVersion);
                if (File.Exists(manifestFilePath))
                {
                    _steps = ESteps.VerifyFileData;
                    _fileData = File.ReadAllBytes(manifestFilePath);
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Cannot find cache manifest file: {manifestFilePath}";
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
                    Error = "Failed to verify cache package manifest file.";
                }
            }

            if (_steps == ESteps.LoadManifest)
            {
                if (_deserializeManifestOp == null)
                {
                    _deserializeManifestOp = new DeserializeManifestOperation(_fileSystem.ManifestDecryptor, _fileData);
                    _deserializeManifestOp.StartOperation();
                    AddChildOperation(_deserializeManifestOp);
                }

                _deserializeManifestOp.UpdateOperation();
                Progress = _deserializeManifestOp.Progress;
                if (_deserializeManifestOp.IsDone == false)
                    return;

                if (_deserializeManifestOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Manifest = _deserializeManifestOp.Manifest;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _deserializeManifestOp.Error;
                }
            }
        }
        internal override string InternalGetDescription()
        {
            return $"PackageVersion: {_packageVersion} PackageHash: {_packageHash}";
        }
    }
}