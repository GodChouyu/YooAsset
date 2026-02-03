using System;
using System.Collections.Generic;

namespace YooAsset
{
    internal class EditorFileCache : IFileCache
    {
        internal struct CacheConfig
        {
            public bool VirtualDownloadMode { get; set; }
            public bool VirtualWebGLMode { get; set; }
            public int AsyncSimulateMinFrame { get; set; }
            public int AsyncSimulateMaxFrame { get; set; }
        }

        private readonly Dictionary<string, EditorFileCacheEntry> _caches = new Dictionary<string, EditorFileCacheEntry>(10000);

        // 缓存配置
        internal readonly CacheConfig Config;

        #region 接口属性
        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName { get; }

        /// <summary>
        /// 缓存根目录
        /// </summary>
        public string RootPath { get; }

        /// <summary>
        /// 只读属性
        /// </summary>
        public bool IsReadOnly { get; }

        /// <summary>
        /// 缓存文件数量
        /// </summary>
        public int FileCount
        {
            get
            {
                return _caches.Count;
            }
        }

        /// <summary>
        /// 已占用空间
        /// 说明：按缓存索引累计
        /// </summary>
        public long SpaceOccupied { get; private set; }
        #endregion

        public EditorFileCache(string packageName, string rootPath, CacheConfig config)
        {
            PackageName = packageName;
            RootPath = rootPath;
            Config = config;
            IsReadOnly = true;
        }
        public void Dispose()
        {
        }
        public virtual FCInitializeOperation InitializeAsync()
        {
            var operation = new EFCInitializeOperation(this);
            return operation;
        }
        public virtual FCWriteCacheOperation WriteCacheAsync(WriteCacheOptions options)
        {
            var operation = new EFCWriteCacheOperation(this, options);
            return operation;
        }
        public virtual FCClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            var operation = new FCClearCacheCompleteOperation();
            return operation;
        }
        public virtual FCVerifyCacheOperation VerifyCacheAsync(VerifyCacheOptions options)
        {
            var operation = new FCVerifyCacheCompleteOperation();
            return operation;
        }
        public virtual FCLoadBundleOperation LoadBundleAsync(LoadBundleOptions options)
        {
            if (options.Bundle.BundleType == (int)EBundleType.VirtualBundle)
            {
                var operation = new EFCLoadVirtualBundleOperation(this, options.Bundle);
                return operation;
            }
            else
            {
                string error = $"{nameof(EditorFileCache)} not support load bundle type : {options.Bundle.BundleType}";
                var operation = new FCLoadBundleErrorOperation(error);
                return operation;
            }
        }
        public virtual bool IsCached(string bundleGUID)
        {
            if (Config.VirtualDownloadMode)
                return _caches.ContainsKey(bundleGUID);
            else
                return true;
        }

        #region 内部方法
        /// <summary>
        /// 获取指定缓存
        /// </summary>
        public EditorFileCacheEntry GetEntry(string bundleGUID)
        {
            if (_caches.TryGetValue(bundleGUID, out EditorFileCacheEntry entry))
                return entry;
            else
                return null;
        }

        /// <summary>
        /// 添加指定缓存
        /// </summary>
        internal void AddEntry(string bundleGUID, EditorFileCacheEntry entry)
        {
            if (_caches.ContainsKey(bundleGUID))
                throw new YooInternalException($"Cache entry already existed: {bundleGUID}");

            _caches.Add(bundleGUID, entry);
        }
        #endregion
    }
}