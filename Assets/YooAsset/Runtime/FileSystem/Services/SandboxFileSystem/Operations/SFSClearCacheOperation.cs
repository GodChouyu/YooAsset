using System;
using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 沙盒文件系统的清理缓存操作
    /// </summary>
    internal class SFSClearCacheOperation : FSClearCacheOperation
    {
        private enum ESteps
        {
            None,
            ClearCache,
            Done,
        }

        private readonly SandboxFileSystem _fileSystem;
        private readonly FSClearCacheOptions _options;
        private FCClearCacheOperation _clearCacheOp;
        private ESteps _steps = ESteps.None;

        internal SFSClearCacheOperation(SandboxFileSystem fileSystem, FSClearCacheOptions options)
        {
            _fileSystem = fileSystem;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.ClearCache;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.ClearCache)
            {
                if (_clearCacheOp == null)
                {
                    _clearCacheOp = _fileSystem.FileCache.ClearCacheAsync(_options.ConvertTo());
                    _clearCacheOp.StartOperation();
                    AddChildOperation(_clearCacheOp);
                }

                _clearCacheOp.UpdateOperation();
                if (_clearCacheOp.IsDone == false)
                    return;

                if (_clearCacheOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _clearCacheOp.Error;
                }
            }
        }
    }

    /// <summary>
    /// 沙盒文件系统的清理所有缓存清单操作
    /// </summary>
    internal class SFSClearAllCacheManifestOperation : FSClearCacheOperation
    {
        private enum ESteps
        {
            None,
            ClearAllCacheFiles,
            Done,
        }

        private readonly SandboxFileSystem _fileSystem;
        private ESteps _steps = ESteps.None;

        internal SFSClearAllCacheManifestOperation(SandboxFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.ClearAllCacheFiles;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.ClearAllCacheFiles)
            {
                try
                {
                    // 注意：如果正在下载资源清单，会有几率触发异常！
                    string directoryRoot = _fileSystem.GetCacheManifestFilesRoot();
                    DirectoryInfo directoryInfo = new DirectoryInfo(directoryRoot);
                    if (directoryInfo.Exists)
                    {
                        foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                        {
                            string fileName = fileInfo.Name;
                            if (fileName == SandboxFileSystemConsts.AppFootprintFileName)
                                continue;

                            fileInfo.Delete();
                        }
                    }

                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                catch (Exception ex)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = ex.Message;
                }
            }
        }
    }

    /// <summary>
    /// 沙盒文件系统的清理未使用缓存清单操作
    /// </summary>
    internal class SFSClearUnusedCacheManifestOperation : FSClearCacheOperation
    {
        private enum ESteps
        {
            None,
            CheckManifest,
            ClearUnusedCacheFiles,
            Done,
        }

        private readonly SandboxFileSystem _fileSystem;
        private readonly PackageManifest _manifest;
        private ESteps _steps = ESteps.None;

        internal SFSClearUnusedCacheManifestOperation(SandboxFileSystem fileSystem, PackageManifest manifest)
        {
            _fileSystem = fileSystem;
            _manifest = manifest;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckManifest;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckManifest)
            {
                if (_manifest == null)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Cannot find active package manifest.";
                }
                else
                {
                    _steps = ESteps.ClearUnusedCacheFiles;
                }
            }

            if (_steps == ESteps.ClearUnusedCacheFiles)
            {
                try
                {
                    string activeManifestFileName = YooAssetSettingsData.GetManifestBinaryFileName(_manifest.PackageName, _manifest.PackageVersion);
                    string activeHashFileName = YooAssetSettingsData.GetPackageHashFileName(_manifest.PackageName, _manifest.PackageVersion);

                    // 注意：如果正在下载资源清单，会有几率触发异常！
                    string directoryRoot = _fileSystem.GetCacheManifestFilesRoot();
                    DirectoryInfo directoryInfo = new DirectoryInfo(directoryRoot);
                    if (directoryInfo.Exists)
                    {
                        foreach (FileInfo fileInfo in directoryInfo.GetFiles())
                        {
                            string fileName = fileInfo.Name;
                            if (fileName == SandboxFileSystemConsts.AppFootprintFileName)
                                continue;
                            if (fileName == activeManifestFileName || fileName == activeHashFileName)
                                continue;

                            fileInfo.Delete();
                        }
                    }

                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                catch (Exception ex)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = ex.Message;
                }
            }
        }
    }
}