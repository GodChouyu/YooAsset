using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    internal class FileSystemHost
    {
        public readonly string PackageName;
        public readonly List<IFileSystem> FileSystems = new List<IFileSystem>(10);

        /// <summary>
        /// 当前激活的清单
        /// </summary>
        public PackageManifest ActiveManifest { get; private set; }


        public FileSystemHost(string packageName)
        {
            PackageName = packageName;
        }

        /// <summary>
        /// 异步初始化
        /// </summary>
        public InitializeFileSystemOperation InitializeAsync(FileSystemParameters fileSystemParameter)
        {
            var fileSystemParamList = new List<FileSystemParameters>();
            if (fileSystemParameter != null)
                fileSystemParamList.Add(fileSystemParameter);
            return InitializeAsync(fileSystemParamList);
        }

        /// <summary>
        /// 异步初始化
        /// </summary>
        public InitializeFileSystemOperation InitializeAsync(FileSystemParameters fileSystemParameterA, FileSystemParameters fileSystemParameterB)
        {
            var fileSystemParamList = new List<FileSystemParameters>();
            if (fileSystemParameterA != null)
                fileSystemParamList.Add(fileSystemParameterA);
            if (fileSystemParameterB != null)
                fileSystemParamList.Add(fileSystemParameterB);
            return InitializeAsync(fileSystemParamList);
        }

        /// <summary>
        /// 异步初始化
        /// </summary>
        public InitializeFileSystemOperation InitializeAsync(List<FileSystemParameters> fileSystemParameterList)
        {
            var operation = new InitializeFileSystemOperation(this, fileSystemParameterList);
            return operation;
        }

        /// <summary>
        /// 销毁文件系统
        /// </summary>
        public void Destroy()
        {
            foreach (var fileSystem in FileSystems)
            {
                fileSystem.OnDestroy();
            }
            FileSystems.Clear();
        }

        /// <summary>
        /// 设置当前激活的资源清单
        /// </summary>
        public void SetActiveManifest(PackageManifest manifest)
        {
            ActiveManifest = manifest;
        }

        /// <summary>
        /// 获取主文件系统
        /// </summary>
        /// <remarks>
        ///  文件系统列表里，最后一个属于主文件系统
        /// </remarks>
        public IFileSystem GetMainFileSystem()
        {
            int count = FileSystems.Count;
            if (count == 0)
                return null;
            return FileSystems[count - 1];
        }

        /// <summary>
        /// 获取资源包所属文件系统
        /// </summary>
        private IFileSystem GetBelongFileSystem(PackageBundle packageBundle)
        {
            for (int i = 0; i < FileSystems.Count; i++)
            {
                IFileSystem fileSystem = FileSystems[i];
                if (fileSystem.Belong(packageBundle))
                {
                    return fileSystem;
                }
            }

            YooLogger.Error($"Can not found belong file system : {packageBundle.BundleName}");
            return null;
        }

        #region 资源包相关
        private BundleInfo CreateBundleInfo(PackageBundle packageBundle)
        {
            if (packageBundle == null)
                throw new YooInternalException();

            var fileSystem = GetBelongFileSystem(packageBundle);
            if (fileSystem != null)
            {
                var bundleInfo = new BundleInfo(fileSystem, packageBundle);
                return bundleInfo;
            }

            throw new YooFileSystemException($"Can not found belong file system : {packageBundle.BundleName}");
        }
        public BundleInfo GetMainBundleInfo(AssetInfo assetInfo)
        {
            if (assetInfo == null || assetInfo.IsInvalid)
                throw new YooInternalException();

            // 注意：如果清单里未找到资源包会抛出异常
            var packageBundle = ActiveManifest.GetMainPackageBundle(assetInfo.Asset);
            return CreateBundleInfo(packageBundle);
        }
        public string GetMainBundleName(int bundleID)
        {
            // 注意：如果清单里未找到资源包会抛出异常！
            var packageBundle = ActiveManifest.GetMainPackageBundle(bundleID);
            return packageBundle.BundleName;
        }
        public List<BundleInfo> GetDependBundleInfos(AssetInfo assetInfo)
        {
            if (assetInfo == null || assetInfo.IsInvalid)
                throw new YooInternalException();

            // 注意：如果清单里未找到资源包会抛出异常！
            List<PackageBundle> depends;
            if (assetInfo.LoadMethod == AssetInfo.ELoadMethod.LoadAllAssets)
            {
                var mainBundle = ActiveManifest.GetMainPackageBundle(assetInfo.Asset);
                depends = ActiveManifest.GetBundleAllDependencies(mainBundle);
            }
            else
            {
                depends = ActiveManifest.GetAssetAllDependencies(assetInfo.Asset);
            }

            List<BundleInfo> result = new List<BundleInfo>(depends.Count);
            foreach (var packageBundle in depends)
            {
                BundleInfo bundleInfo = CreateBundleInfo(packageBundle);
                result.Add(bundleInfo);
            }
            return result;
        }
        #endregion

        #region 下载器相关
        private const int DefaultBundleInfoCapacity = 1000;

        public bool IsNeedDownloadFromRemoteInternal(AssetInfo assetInfo)
        {
            if (assetInfo.IsInvalid)
            {
                YooLogger.Warning(assetInfo.Error);
                return false;
            }

            BundleInfo bundleInfo = GetMainBundleInfo(assetInfo);
            if (bundleInfo.IsNeedDownloadFromRemote())
                return true;

            List<BundleInfo> depends = GetDependBundleInfos(assetInfo);
            foreach (var depend in depends)
            {
                if (depend.IsNeedDownloadFromRemote())
                    return true;
            }

            return false;
        }

        private delegate bool NeedPredicate(IFileSystem fileSystem, PackageBundle bundle);
        private bool NeedDownload(IFileSystem fileSystem, PackageBundle bundle)
        {
            return fileSystem.NeedDownload(bundle);
        }
        private bool NeedUnpack(IFileSystem fileSystem, PackageBundle bundle)
        {
            return fileSystem.NeedUnpack(bundle);
        }
        private bool NeedImport(IFileSystem fileSystem, PackageBundle bundle)
        {
            return fileSystem.NeedImport(bundle);
        }

        public ResourceDownloaderOperation CreateResourceDownloader(ResourceDownloaderOptions options)
        {
            return CreateResourceDownloader(ActiveManifest, options);
        }
        public ResourceDownloaderOperation CreateResourceDownloader(PackageManifest manifest, ResourceDownloaderOptions options)
        {
            List<BundleInfo> downloadList;
            if (options.Tags == null)
                downloadList = GetBundleInfoListByAll(manifest, NeedDownload);
            else
                downloadList = GetBundleInfoListByTags(manifest, options.Tags, NeedDownload);

            var operation = new ResourceDownloaderOperation(PackageName, downloadList, options.MaximumConcurrency, options.FailedTryAgain);
            return operation;
        }

        public ResourceDownloaderOperation CreateResourceDownloader(BundleDownloaderOptions options)
        {
            return CreateResourceDownloader(ActiveManifest, options);
        }
        public ResourceDownloaderOperation CreateResourceDownloader(PackageManifest manifest, BundleDownloaderOptions options)
        {
            List<BundleInfo> downloadList;
            if (options.AssetInfos == null)
                downloadList = GetBundleInfoListByAll(manifest, NeedDownload);
            else
                downloadList = GetBundleInfoListByAssetInfos(manifest, options.AssetInfos, options.DownloadBundleDependencies, NeedDownload);

            var operation = new ResourceDownloaderOperation(PackageName, downloadList, options.MaximumConcurrency, options.FailedTryAgain);
            return operation;
        }

        public ResourceUnpackerOperation CreateResourceUnpacker(ResourceUnpackerOptions options)
        {
            return CreateResourceUnpacker(ActiveManifest, options);
        }
        public ResourceUnpackerOperation CreateResourceUnpacker(PackageManifest manifest, ResourceUnpackerOptions options)
        {
            List<BundleInfo> unpackList;
            if (options.Tags == null)
                unpackList = GetBundleInfoListByAll(manifest, NeedUnpack);
            else
                unpackList = GetBundleInfoListByTags(manifest, options.Tags, NeedUnpack);

            var operation = new ResourceUnpackerOperation(PackageName, unpackList, options.MaximumConcurrency, options.FailedTryAgain);
            return operation;
        }

        public ResourceImporterOperation CreateResourceImporter(BundleImporterOptions options)
        {
            return CreateResourceImporter(ActiveManifest, options);
        }
        public ResourceImporterOperation CreateResourceImporter(PackageManifest manifest, BundleImporterOptions options)
        {
            List<BundleInfo> importerList = GetBundleInfoListByBundleInfos(manifest, options.BundleInfos, NeedImport);
            var operation = new ResourceImporterOperation(PackageName, importerList, options.MaximumConcurrency, options.FailedTryAgain);
            return operation;
        }

        private List<BundleInfo> GetBundleInfoListByAll(PackageManifest manifest, Func<IFileSystem, PackageBundle, bool> predicate)
        {
            if (manifest == null)
                return new List<BundleInfo>();

            List<BundleInfo> result = new List<BundleInfo>(DefaultBundleInfoCapacity);
            foreach (var packageBundle in manifest.BundleList)
            {
                var fileSystem = GetBelongFileSystem(packageBundle);
                if (fileSystem == null)
                    continue;

                if (predicate(fileSystem, packageBundle))
                {
                    var bundleInfo = new BundleInfo(fileSystem, packageBundle);
                    result.Add(bundleInfo);
                }
            }
            return result;
        }
        private List<BundleInfo> GetBundleInfoListByTags(PackageManifest manifest, string[] tags, Func<IFileSystem, PackageBundle, bool> predicate)
        {
            if (manifest == null)
                return new List<BundleInfo>();

            List<BundleInfo> result = new List<BundleInfo>(DefaultBundleInfoCapacity);
            foreach (var packageBundle in manifest.BundleList)
            {
                var fileSystem = GetBelongFileSystem(packageBundle);
                if (fileSystem == null)
                    continue;

                if (predicate(fileSystem, packageBundle))
                {
                    // 注意：如果未带任何标记，则统一处理
                    if (packageBundle.HasAnyTags() == false)
                    {
                        var bundleInfo = new BundleInfo(fileSystem, packageBundle);
                        result.Add(bundleInfo);
                    }
                    else
                    {
                        if (packageBundle.HasTag(tags))
                        {
                            var bundleInfo = new BundleInfo(fileSystem, packageBundle);
                            result.Add(bundleInfo);
                        }
                    }
                }
            }
            return result;
        }
        private List<BundleInfo> GetBundleInfoListByAssetInfos(PackageManifest manifest, AssetInfo[] assetInfos, bool recursiveDepend, Func<IFileSystem, PackageBundle, bool> predicate)
        {
            if (manifest == null)
                return new List<BundleInfo>();

            // 获取资源对象的资源包和所有依赖资源包
            HashSet<string> checkSet = new HashSet<string>();
            List<PackageBundle> checkList = new List<PackageBundle>(assetInfos.Length);
            foreach (var assetInfo in assetInfos)
            {
                if (assetInfo.IsInvalid)
                {
                    YooLogger.Warning(assetInfo.Error);
                    continue;
                }

                // 注意：如果清单里未找到资源包会抛出异常！
                PackageBundle mainBundle = manifest.GetMainPackageBundle(assetInfo.Asset);
                if (checkSet.Contains(mainBundle.BundleGUID) == false)
                {
                    checkSet.Add(mainBundle.BundleGUID);
                    checkList.Add(mainBundle);
                }

                // 注意：如果清单里未找到资源包会抛出异常！
                List<PackageBundle> mainDependBundles = manifest.GetAssetAllDependencies(assetInfo.Asset);
                foreach (var dependBundle in mainDependBundles)
                {
                    if (checkSet.Contains(dependBundle.BundleGUID) == false)
                    {
                        checkSet.Add(dependBundle.BundleGUID);
                        checkList.Add(dependBundle);
                    }
                }

                // 下载主资源包内所有资源对象依赖的资源包
                if (recursiveDepend)
                {
                    foreach (var otherMainAsset in mainBundle.IncludeMainAssets)
                    {
                        var otherMainBundle = manifest.GetMainPackageBundle(otherMainAsset.BundleID);
                        if (checkSet.Contains(otherMainBundle.BundleGUID) == false)
                        {
                            checkSet.Add(otherMainBundle.BundleGUID);
                            checkList.Add(otherMainBundle);
                        }

                        List<PackageBundle> otherDependBundles = manifest.GetAssetAllDependencies(otherMainAsset);
                        foreach (var dependBundle in otherDependBundles)
                        {
                            if (checkSet.Contains(dependBundle.BundleGUID) == false)
                            {
                                checkSet.Add(dependBundle.BundleGUID);
                                checkList.Add(dependBundle);
                            }
                        }
                    }
                }
            }

            List<BundleInfo> result = new List<BundleInfo>(DefaultBundleInfoCapacity);
            foreach (var packageBundle in checkList)
            {
                var fileSystem = GetBelongFileSystem(packageBundle);
                if (fileSystem == null)
                    continue;

                if (predicate(fileSystem, packageBundle))
                {
                    var bundleInfo = new BundleInfo(fileSystem, packageBundle);
                    result.Add(bundleInfo);
                }
            }
            return result;
        }
        private List<BundleInfo> GetBundleInfoListByBundleInfos(PackageManifest manifest, ImportBundleInfo[] fileInfos, Func<IFileSystem, PackageBundle, bool> predicate)
        {
            if (manifest == null)
                return new List<BundleInfo>();

            List<BundleInfo> result = new List<BundleInfo>(DefaultBundleInfoCapacity);
            foreach (var fileInfo in fileInfos)
            {
                string filePath = fileInfo.FilePath;
                if (string.IsNullOrEmpty(filePath))
                    continue;

                PackageBundle packageBundle;
                if (string.IsNullOrEmpty(fileInfo.BundleName) == false)
                {
                    if (manifest.TryGetPackageBundleByBundleName(fileInfo.BundleName, out packageBundle) == false)
                    {
                        YooLogger.Warning($"Not found package bundle, bundle name : {fileInfo.BundleName}");
                        continue;
                    }
                }
                else if (string.IsNullOrEmpty(fileInfo.BundleGUID) == false)
                {
                    if (manifest.TryGetPackageBundleByBundleGUID(fileInfo.BundleGUID, out packageBundle) == false)
                    {
                        YooLogger.Warning($"Not found package bundle, bundle guid : {fileInfo.BundleGUID}");
                        continue;
                    }
                }
                else
                {
                    string fileName = System.IO.Path.GetFileName(filePath);
                    if (manifest.TryGetPackageBundleByFileName(fileName, out packageBundle) == false)
                    {
                        YooLogger.Warning($"Not found package bundle, file name : {fileName}");
                        continue;
                    }
                }

                if (packageBundle != null)
                {
                    var fileSystem = GetBelongFileSystem(packageBundle);
                    if (fileSystem == null)
                        continue;

                    if (predicate(fileSystem, packageBundle))
                    {
                        var bundleInfo = new BundleInfo(fileSystem, packageBundle, filePath);
                        result.Add(bundleInfo);
                    }
                }
            }
            return result;
        }
        #endregion
    }
}