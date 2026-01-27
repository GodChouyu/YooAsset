using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace YooAsset
{
    internal sealed class SearchCacheFilesOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            Prepare,
            SearchFiles,
            Done,
        }

        private readonly SandboxFileCache _cache;
        private IEnumerator<string> _filesEnumerator = null;
        private double _verifyStartTime;
        private ESteps _steps = ESteps.None;

        /// <summary>
        /// 需要验证的元素
        /// </summary>
        public readonly List<VerifyFileInfo> Result = new List<VerifyFileInfo>(5000);


        internal SearchCacheFilesOperation(SandboxFileCache cache)
        {
            _cache = cache;
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
                if (Directory.Exists(_cache.RootPath))
                {
                    var directories = Directory.EnumerateDirectories(_cache.RootPath);
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

                var rootFoder = _filesEnumerator.Current;
                var childDirectories = Directory.EnumerateDirectories(rootFoder);
                foreach (var chidDirectory in childDirectories)
                {
                    string bundleGUID = Path.GetFileName(chidDirectory);
                    if (_cache.IsCached(bundleGUID))
                        continue;

                    // 创建验证元素类
                    string fileRootPath = chidDirectory;
                    string dataFilePath = PathUtility.Combine(fileRootPath, SandboxFileCacheDefine.BundleDataFileName);
                    string infoFilePath = PathUtility.Combine(fileRootPath, SandboxFileCacheDefine.BundleInfoFileName);
                    var element = new VerifyFileInfo(bundleGUID, fileRootPath, dataFilePath, infoFilePath);
                    Result.Add(element);
                }

                if (IsBusy)
                    break;
            }

            return isFindItem;
        }
    }
}