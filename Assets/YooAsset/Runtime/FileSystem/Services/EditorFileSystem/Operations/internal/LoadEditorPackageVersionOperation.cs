using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 加载编辑器包裹版本文件操作
    /// </summary>
    internal class LoadEditorPackageVersionOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            LoadVersion,
            Done,
        }

        private readonly EditorFileSystem _fileSystem;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 包裹版本
        /// </summary>
        public string PackageVersion { get; private set; }


        internal LoadEditorPackageVersionOperation(EditorFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.LoadVersion;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.LoadVersion)
            {
                string versionFilePath = _fileSystem.GetEditorPackageVersionFilePath();
                if (File.Exists(versionFilePath))
                {
                    PackageVersion = FileUtility.ReadAllText(versionFilePath);
                    if (TextUtility.ValidateContent(PackageVersion, out string validateError) == false)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = $"Simulation package version file validate failed: {validateError}";
                    }
                    else
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                    }
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = $"Cannot find simulation package version file: {versionFilePath}";
                }
            }
        }
    }
}