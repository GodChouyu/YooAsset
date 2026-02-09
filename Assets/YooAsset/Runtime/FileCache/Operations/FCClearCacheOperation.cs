using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 清理缓存操作基类
    /// </summary>
    internal abstract class FCClearCacheOperation : AsyncOperationBase
    {
        protected readonly struct ClearResult
        {
            /// <summary>
            /// 错误信息
            /// </summary>
            public readonly string Error;

            /// <summary>
            /// 需要清理的资源标识符集合
            /// </summary>
            public readonly List<string> BundleGUIDs;

            /// <summary>
            /// 是否成功
            /// </summary>
            public bool Succeeded
            {
                get { return Error == null; }
            }

            public ClearResult(string error)
            {
                Error = error;
                BundleGUIDs = null;
            }
            public ClearResult(List<string> bundleGUIDs)
            {
                Error = null;
                BundleGUIDs = bundleGUIDs;
            }

            public static ClearResult Success(List<string> bundleGUIDs)
            {
                return new ClearResult(bundleGUIDs);
            }
            public static ClearResult Failure(string error)
            {
                return new ClearResult(error);
            }
        }

        protected ClearResult GetAllCache(IReadOnlyCollection<ICacheEntry> cacheEntries)
        {
            var bundleGUIDs = new List<string>(cacheEntries.Count);
            foreach (var entry in cacheEntries)
            {
                bundleGUIDs.Add(entry.BundleGUID);
            }
            return ClearResult.Success(bundleGUIDs);
        }
        protected ClearResult GetUnusedCache(FCClearCacheOptions options, IReadOnlyCollection<ICacheEntry> cacheEntries)
        {
            if (options.Manifest == null)
                return ClearResult.Failure("Active package manifest not found.");

            var bundleGUIDs = new List<string>(cacheEntries.Count);
            foreach (var entry in cacheEntries)
            {
                if (options.Manifest.IsIncludeBundleFile(entry.BundleGUID) == false)
                {
                    bundleGUIDs.Add(entry.BundleGUID);
                }
            }
            return ClearResult.Success(bundleGUIDs);
        }
        protected ClearResult GetCacheByLocations(FCClearCacheOptions options, IReadOnlyCollection<ICacheEntry> cacheEntries)
        {
            if (options.Manifest == null)
                return ClearResult.Failure("Active package manifest not found.");

            if (options.ClearParam == null)
                return ClearResult.Failure("Clear param is null.");

            string[] locations;
            if (options.ClearParam is string str)
                locations = new string[] { str };
            else if (options.ClearParam is List<string> list)
                locations = list.ToArray();
            else if (options.ClearParam is string[] array)
                locations = array;
            else
                return ClearResult.Failure($"Invalid clear param: {options.ClearParam.GetType().FullName}");

            var bundleGUIDs = new List<string>(locations.Length);
            foreach (var location in locations)
            {
                string assetPath = options.Manifest.TryMappingToAssetPath(location);
                if (options.Manifest.TryGetPackageAsset(assetPath, out PackageAsset packageAsset))
                {
                    PackageBundle bundle = options.Manifest.GetMainPackageBundle(packageAsset.BundleID);
                    bundleGUIDs.Add(bundle.BundleGUID);
                }
            }
            return ClearResult.Success(bundleGUIDs);
        }
        protected ClearResult GetCacheByTags(FCClearCacheOptions options, IReadOnlyCollection<ICacheEntry> cacheEntries)
        {
            if (options.Manifest == null)
                return ClearResult.Failure("Active package manifest not found.");

            if (options.ClearParam == null)
                return ClearResult.Failure("Clear param is null.");

            string[] tags;
            if (options.ClearParam is string str)
                tags = new string[] { str };
            else if (options.ClearParam is List<string> list)
                tags = list.ToArray();
            else if (options.ClearParam is string[] array)
                tags = array;
            else
                return ClearResult.Failure($"Invalid clear param: {options.ClearParam.GetType().FullName}");

            var bundleGUIDs = new List<string>(cacheEntries.Count);
            foreach (var entry in cacheEntries)
            {
                if (options.Manifest.TryGetPackageBundleByBundleGUID(entry.BundleGUID, out PackageBundle bundle))
                {
                    if (bundle.HasTag(tags))
                        bundleGUIDs.Add(bundle.BundleGUID);
                }
            }
            return ClearResult.Success(bundleGUIDs);
        }
    }

    /// <summary>
    /// 清理缓存完成操作
    /// </summary>
    internal class FCClearCacheCompleteOperation : FCClearCacheOperation
    {
        private readonly string _error;

        public FCClearCacheCompleteOperation()
        {
            _error = null;
        }
        public FCClearCacheCompleteOperation(string error)
        {
            _error = error;
        }
        internal override void InternalStart()
        {
            if (string.IsNullOrEmpty(_error))
            {
                Status = EOperationStatus.Succeeded;
            }
            else
            {
                Status = EOperationStatus.Failed;
                Error = _error;
            }
        }
        internal override void InternalUpdate()
        {
        }
    }
}
