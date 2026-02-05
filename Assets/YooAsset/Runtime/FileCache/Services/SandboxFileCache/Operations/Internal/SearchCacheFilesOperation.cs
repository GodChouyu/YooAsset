using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 搜索缓存文件操作，扫描缓存目录中的文件
    /// </summary>
    internal sealed class SearchCacheFilesOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            Prepare,
            SearchFiles,
            Done,
        }

        private readonly SandboxFileCache _fileCache;
        private IEnumerator<string> _filesEnumerator = null;
        private double _verifyStartTime;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 需要验证的元素
        /// </summary>
        public readonly List<SearchFileInfo> Result = new List<SearchFileInfo>(5000);


        internal SearchCacheFilesOperation(SandboxFileCache fileCache)
        {
            _fileCache = fileCache;
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
                if (Directory.Exists(_fileCache.RootPath))
                {
                    var directories = Directory.EnumerateDirectories(_fileCache.RootPath);
                    _filesEnumerator = directories.GetEnumerator();
                    _verifyStartTime = TimeUtility.RealtimeSinceStartup;
                    _steps = ESteps.SearchFiles;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Succeeded;
                }
            }

            if (_steps == ESteps.SearchFiles)
            {
                if (SearchFiles())
                    return;

                _filesEnumerator.Dispose();
                _filesEnumerator = null;

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeeded;
                double costTime = TimeUtility.RealtimeSinceStartup - _verifyStartTime;
                YooLogger.Log($"Search cache files elapsed time {costTime:f1} seconds");
            }
        }

        private bool SearchFiles()
        {
            bool isFindItem;
            while (true)
            {
                isFindItem = _filesEnumerator.MoveNext();
                if (isFindItem == false)
                    break;

                var rootFolder = _filesEnumerator.Current;
                var childDirectories = Directory.EnumerateDirectories(rootFolder);
                foreach (var childDirectory in childDirectories)
                {
                    string bundleGUID = Path.GetFileName(childDirectory);
                    if (_fileCache.IsCached(bundleGUID))
                        continue;

                    // 创建验证元素类
                    string fileRootPath = childDirectory;
                    string dataFilePath = PathUtility.Combine(fileRootPath, SandboxFileCacheDefine.BundleDataFileName);
                    string infoFilePath = PathUtility.Combine(fileRootPath, SandboxFileCacheDefine.BundleInfoFileName);
                    var element = new SearchFileInfo(bundleGUID, fileRootPath, dataFilePath, infoFilePath);
                    Result.Add(element);
                }

                if (IsBusy)
                    break;
            }

            return isFindItem;
        }
    }
}