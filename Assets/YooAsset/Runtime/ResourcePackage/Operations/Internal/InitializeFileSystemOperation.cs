using System.Collections.Generic;
using System.Linq;

namespace YooAsset
{
    public class InitializeFileSystemOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            Prepare,
            InitFileSystem,
            CheckInitResult,
            Done,
        }

        private readonly FileSystemHost _host;
        private readonly List<FileSystemParameters> _parametersList;
        private List<FileSystemParameters> _cloneList;
        private FSInitializeOperation _initFileSystemOp;
        private ESteps _steps = ESteps.None;
        
        internal InitializeFileSystemOperation(FileSystemHost host, List<FileSystemParameters> parametersList)
        {
            _host = host;
            _parametersList = parametersList;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.Prepare;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.Prepare)
            {
                if (_parametersList == null || _parametersList.Count == 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "The file system parameters is empty.";
                    return;
                }

                foreach (var fileSystemParam in _parametersList)
                {
                    if (fileSystemParam == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "An empty object exists in the list.";
                        return;
                    }
                }

                _cloneList = _parametersList.ToList();
                _steps = ESteps.InitFileSystem;
            }

            if (_steps == ESteps.InitFileSystem)
            {
                if (_cloneList.Count == 0)
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
                else
                {
                    var fileSystemParams = _cloneList[0];
                    _cloneList.RemoveAt(0);

                    IFileSystem fileSystemInstance = fileSystemParams.CreateFileSystem(_host.PackageName);
                    if (fileSystemInstance == null)
                    {
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "Failed to create file system instance.";
                        return;
                    }

                    _host.FileSystems.Add(fileSystemInstance);
                    _initFileSystemOp = fileSystemInstance.InitializeAsync();
                    _initFileSystemOp.StartOperation();
                    AddChildOperation(_initFileSystemOp);
                    _steps = ESteps.CheckInitResult;
                }
            }

            if (_steps == ESteps.CheckInitResult)
            {
                _initFileSystemOp.UpdateOperation();
                Progress = _initFileSystemOp.Progress;
                if (_initFileSystemOp.IsDone == false)
                    return;

                if (_initFileSystemOp.Status == EOperationStatus.Succeeded)
                {
                    _steps = ESteps.InitFileSystem;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _initFileSystemOp.Error;
                    return;
                }
            }
        }
    }
}