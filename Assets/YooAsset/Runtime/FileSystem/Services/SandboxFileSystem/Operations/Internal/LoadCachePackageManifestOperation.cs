using System.IO;

namespace YooAsset
{
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
        private DeserializeManifestOperation _deserializer;
        private byte[] _fileData;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹清单
        /// </summary>
        public PackageManifest Manifest { private set; get; }


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
                    Error = $"Can not found cache manifest file : {manifestFilePath}";
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
                if (_deserializer == null)
                {
                    _deserializer = new DeserializeManifestOperation(_fileSystem.ManifestDecryptor, _fileData);
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