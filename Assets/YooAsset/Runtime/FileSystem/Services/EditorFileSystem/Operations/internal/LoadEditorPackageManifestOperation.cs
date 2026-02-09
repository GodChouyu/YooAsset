using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 加载编辑器包裹清单文件操作
    /// </summary>
    internal class LoadEditorPackageManifestOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            LoadFileData,
            VerifyFileData,
            LoadManifest,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private readonly string _packageVersion;
        private readonly string _packageHash;
        private DeserializeManifestOperation _deserializeManifestOp;
        private byte[] _fileData;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹清单
        /// </summary>
        public PackageManifest Manifest { get; private set; }


        internal LoadEditorPackageManifestOperation(EditorFileSystem fileSystem, string packageVersion, string packageHash)
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
                string manifestFilePath = _fileSystem.GetEditorPackageManifestFilePath(_packageVersion);
                if (File.Exists(manifestFilePath))
                {
                    _steps = ESteps.VerifyFileData;
                    _fileData = FileUtility.ReadAllBytes(manifestFilePath);
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Cannot find simulation package manifest file: {manifestFilePath}";
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
                    Error = "Failed to verify simulation package manifest file.";
                }
            }

            if (_steps == ESteps.LoadManifest)
            {
                if (_deserializeManifestOp == null)
                {
                    _deserializeManifestOp = new DeserializeManifestOperation(null, _fileData);
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