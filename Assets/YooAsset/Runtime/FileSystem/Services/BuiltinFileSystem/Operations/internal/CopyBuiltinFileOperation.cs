using System;
using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 拷贝内置文件操作
    /// </summary>
    internal class CopyBuiltinFileOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            CheckFileExist,
            TryCopyFile,
            UnpackFile,
            Done,
        }

        private readonly BuiltinFileSystem _fileSystem;
        private readonly string _sourceFilePath;
        private readonly string _destFilePath;
        private IDownloadFileRequest _downloadFileRequest;
        private ESteps _steps = ESteps.None;

        public CopyBuiltinFileOperation(BuiltinFileSystem fileSystem, string sourceFilePath, string destFilePath)
        {
            _fileSystem = fileSystem;
            _sourceFilePath = sourceFilePath;
            _destFilePath = destFilePath;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckFileExist;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckFileExist)
            {
                if (File.Exists(_destFilePath))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.TryCopyFile;
                }
            }

            if (_steps == ESteps.TryCopyFile)
            {
                if (File.Exists(_sourceFilePath))
                {
                    try
                    {
                        var directory = Path.GetDirectoryName(_destFilePath);
                        if (Directory.Exists(directory) == false)
                            Directory.CreateDirectory(directory);
                        File.Copy(_sourceFilePath, _destFilePath, true);
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        YooLogger.Warning($"Failed to copy builtin file: {ex.Message}");
                        _steps = ESteps.UnpackFile;
                    }
                }
                else
                {
                    _steps = ESteps.UnpackFile;
                }
            }

            if (_steps == ESteps.UnpackFile)
            {
                if (_downloadFileRequest == null)
                {
                    //TODO 团结引擎，在某些安卓机型（红米），通过UnityWebRequest拷贝包内文件会小概率失败！需要借助其它方式来拷贝包内文件。
                    string url = DownloadSystemTools.ToLocalUrl(_sourceFilePath);
                    var args = new DownloadFileRequestArgs(url, _destFilePath, 60, 0);
                    _downloadFileRequest = _fileSystem.DownloadBackend.CreateFileRequest(args);
                    _downloadFileRequest.SendRequest();
                }

                if (_downloadFileRequest.IsDone == false)
                    return;

                if (_downloadFileRequest.Status == EDownloadRequestStatus.Succeeded)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _downloadFileRequest.Error;
                }
            }
        }
        internal override void InternalDispose()
        {
            if (_downloadFileRequest != null)
            {
                _downloadFileRequest.Dispose();
                _downloadFileRequest = null;
            }
        }
        internal override void InternalWaitForCompletion()
        {
            //注意：等待解压本地文件完毕，该操作会挂起主线程！
            ExecuteUntilComplete();
        }
    }
}