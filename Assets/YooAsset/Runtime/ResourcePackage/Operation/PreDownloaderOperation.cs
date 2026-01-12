using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    public sealed class PreDownloaderOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            CheckParams,
            CheckActiveManifest,
            LoadPackageManifest,
            Done,
        }

        private readonly FileSystemHost _host;
        private readonly PreDownloaderOptions _options;
        private FSLoadManifestOperation _loadPackageManifestOp;
        private PackageManifest _manifest;
        private ESteps _steps = ESteps.None;


        internal PreDownloaderOperation(FileSystemHost host, PreDownloaderOptions options)
        {
            _host = host;
            _options = options;
        }
        internal override void InternalStart()
        {
            _steps = ESteps.CheckParams;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckParams)
            {
                if (string.IsNullOrEmpty(_options.PackageVersion))
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = "Package version is null or empty.";
                    return;
                }

                _steps = ESteps.CheckActiveManifest;
            }

            if (_steps == ESteps.CheckActiveManifest)
            {
                // 检测当前激活的清单对象
                if (_host.ActiveManifest != null)
                {
                    if (_host.ActiveManifest.PackageVersion == _options.PackageVersion)
                    {
                        _manifest = _host.ActiveManifest;
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Succeed;
                        return;
                    }
                }
                _steps = ESteps.LoadPackageManifest;
            }

            if (_steps == ESteps.LoadPackageManifest)
            {
                if (_loadPackageManifestOp == null)
                {
                    var mainFileSystem = _host.GetMainFileSystem();
                    var options = new LoadManifestOptions(_options.PackageVersion, _options.Timeout);
                    _loadPackageManifestOp = mainFileSystem.LoadManifestAsync(options);
                    _loadPackageManifestOp.StartOperation();
                    AddChildOperation(_loadPackageManifestOp);
                }

                _loadPackageManifestOp.UpdateOperation();
                if (_loadPackageManifestOp.IsDone == false)
                    return;

                if (_loadPackageManifestOp.Status == EOperationStatus.Succeed)
                {
                    _manifest = _loadPackageManifestOp.Manifest;
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeed;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _loadPackageManifestOp.Error;
                }
            }
        }

        /// <summary>
        /// 创建资源下载器，用于下载指定的资源标签列表关联的资源包文件
        /// </summary>
        public ResourceDownloaderOperation CreateResourceDownloader(ResourceDownloaderOptions options)
        {
            if (Status != EOperationStatus.Succeed)
            {
                YooLogger.Error($"{nameof(PreDownloaderOperation)} status is not succeed.");
                return ResourceDownloaderOperation.CreateEmptyDownloader(_host.PackageName);
            }

            return _host.CreateResourceDownloader(_manifest, options);
        }

        /// <summary>
        /// 创建资源下载器，用于下载指定的资源信息列表依赖的资源包文件
        /// </summary>
        public ResourceDownloaderOperation CreateBundleDownloader(BundleDownloaderOptions options)
        {
            if (Status != EOperationStatus.Succeed)
            {
                YooLogger.Error($"{nameof(PreDownloaderOperation)} status is not succeed.");
                return ResourceDownloaderOperation.CreateEmptyDownloader(_host.PackageName);
            }

            return _host.CreateResourceDownloader(_manifest, options);
        }
    }
}