using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using YooAsset;

/// <summary>
/// 拷贝内置清单文件到沙盒目录
/// </summary>
public class CopyBuildinManifestOperation : AsyncOperationBase
{
    private enum ESteps
    {
        None,
        CheckHashFile,
        UnpackHashFile,
        CheckManifestFile,
        UnpackManifestFile,
        Done,
    }

    private readonly string _packageName;
    private readonly string _packageVersion;
    private readonly IDownloadBackend _backend;
    private IDownloadFileRequest _hashFileRequest;
    private IDownloadFileRequest _manifestFileRequest;
    private ESteps _steps = ESteps.None;

    public CopyBuildinManifestOperation(string packageName, string packageVersion)
    {
        _packageName = packageName;
        _packageVersion = packageVersion;
        _backend = new UnityWebRequestBackend();
    }
    internal override void InternalStart()
    {
        _steps = ESteps.CheckHashFile;
    }
    internal override void InternalUpdate()
    {
        if (_steps == ESteps.None || _steps == ESteps.Done)
            return;

        if (_steps == ESteps.CheckHashFile)
        {
            string hashFilePath = GetCacheHashFilePath();
            if (File.Exists(hashFilePath))
            {
                _steps = ESteps.CheckManifestFile;
                return;
            }

            _steps = ESteps.UnpackHashFile;
        }

        if (_steps == ESteps.UnpackHashFile)
        {
            if(_hashFileRequest == null)
            {
                string sourcePath = GetBuildinHashFilePath();
                string destPath = GetCacheHashFilePath();
                string url = DownloadSystemTools.ToLocalUrl(sourcePath);
                var args = new DownloadFileRequestArgs(url, destPath, 60, 0);
                _hashFileRequest = _backend.CreateFileRequest(args);
                _hashFileRequest.SendRequest();
            }

            if (_hashFileRequest.IsDone == false)
                return;

            if (_hashFileRequest.Status == EDownloadRequestStatus.Succeeded)
            {
                _steps = ESteps.CheckManifestFile;
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = _hashFileRequest.Error;
            }
        }

        if (_steps == ESteps.CheckManifestFile)
        {
            string manifestFilePath = GetCacheManifestFilePath();
            if (File.Exists(manifestFilePath))
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
                return;
            }

            _steps = ESteps.UnpackManifestFile;
        }

        if (_steps == ESteps.UnpackManifestFile)
        {
            if (_manifestFileRequest == null)
            {
                string sourcePath = GetBuildinManifestFilePath();
                string destPath = GetCacheManifestFilePath();
                string url = DownloadSystemTools.ToLocalUrl(sourcePath);
                var args = new DownloadFileRequestArgs(url, destPath, 60, 0);
                _manifestFileRequest = _backend.CreateFileRequest(args);
                _manifestFileRequest.SendRequest();
            }

            if (_manifestFileRequest.IsDone == false)
                return;

            if (_manifestFileRequest.Status == EDownloadRequestStatus.Succeeded)
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
            }
            else
            {
                _steps = ESteps.Done;
                Status = EOperationStatus.Failed;
                Error = _manifestFileRequest.Error;
            }
        }
    }

    private string GetBuildinYooRoot()
    {
        return YooAssetSettingsData.GetYooDefaultBuiltinRoot();
    }
    private string GetBuildinHashFilePath()
    {
        string fileRoot = GetBuildinYooRoot();
        string fileName = YooAssetSettingsData.GetPackageHashFileName(_packageName, _packageVersion);
        return PathUtility.Combine(fileRoot, _packageName, fileName);
    }
    private string GetBuildinManifestFilePath()
    {
        string fileRoot = GetBuildinYooRoot();
        string fileName = YooAssetSettingsData.GetManifestBinaryFileName(_packageName, _packageVersion);
        return PathUtility.Combine(fileRoot, _packageName, fileName);
    }

    private string GetCacheYooRoot()
    {
        return YooAssetSettingsData.GetYooDefaultCacheRoot();
    }
    private string GetCacheHashFilePath()
    {
        string fileRoot = GetCacheYooRoot();
        string fileName = YooAssetSettingsData.GetPackageHashFileName(_packageName, _packageVersion);
        return PathUtility.Combine(fileRoot, _packageName, fileName);
    }
    private string GetCacheManifestFilePath()
    {
        string fileRoot = GetCacheYooRoot();
        string fileName = YooAssetSettingsData.GetManifestBinaryFileName(_packageName, _packageVersion);
        return PathUtility.Combine(fileRoot, _packageName, fileName);
    }
}
