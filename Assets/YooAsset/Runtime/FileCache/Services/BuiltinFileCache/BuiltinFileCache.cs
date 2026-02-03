using System;
using System.Collections.Generic;

namespace YooAsset
{
    internal class BuiltinFileCache : IFileCache
    {
        internal struct CacheConfig
        {
            /// <summary>
            /// AssetBundle 解密器
            /// </summary>
            public IBundleDecryptor AssetBundleDecryptor { get; set; }

            /// <summary>
            /// RawBundle 解密器
            /// </summary>
            public IBundleDecryptor RawBundleDecryptor { get; set; }

            /// <summary>
            /// 下载后台接口
            /// </summary>
            public IDownloadBackend DownloadBackend { get; set; }
        }

        private readonly Dictionary<string, BuiltinFileCacheEntry> _caches = new Dictionary<string, BuiltinFileCacheEntry>(10000);

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

        public BuiltinFileCache(string packageName, string rootPath, CacheConfig config)
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
            var operation = new BFCInitializeOperation(this);
            return operation;
        }
        public virtual FCWriteCacheOperation WriteCacheAsync(WriteCacheOptions options)
        {
            var operation = new FCWriteCacheCompleteOperation($"{nameof(BuiltinFileCache)} is readonly.");
            return operation;
        }
        public virtual FCClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            var operation = new FCClearCacheCompleteOperation($"{nameof(BuiltinFileCache)} is readonly.");
            return operation;
        }
        public virtual FCVerifyCacheOperation VerifyCacheAsync(VerifyCacheOptions options)
        {
            var operation = new FCVerifyCacheCompleteOperation();
            return operation;
        }
        public virtual FCLoadBundleOperation LoadBundleAsync(LoadBundleOptions options)
        {
            if (options.Bundle.BundleType == (int)EBundleType.AssetBundle)
            {
                var operation = new BFCLoadAssetBundleOperation(this, options.Bundle);
                return operation;
            }
            else if (options.Bundle.BundleType == (int)EBundleType.RawBundle)
            {
                var operation = new BFCLoadRawBundleOperation(this, options.Bundle);
                return operation;
            }
            else
            {
                string error = $"{nameof(BuiltinFileCache)} not support load bundle type : {options.Bundle.BundleType}";
                var operation = new FCLoadBundleErrorOperation(error);
                return operation;
            }
        }
        public virtual bool IsCached(string bundleGUID)
        {
            return _caches.ContainsKey(bundleGUID);
        }

        #region 内部方法
        /// <summary>
        /// 获取指定缓存
        /// </summary>
        public BuiltinFileCacheEntry GetEntry(string bundleGUID)
        {
            if (_caches.TryGetValue(bundleGUID, out BuiltinFileCacheEntry entry))
                return entry;
            else
                return null;
        }

        /// <summary>
        /// 添加指定缓存
        /// </summary>
        internal void AddEntry(string bundleGUID, BuiltinFileCacheEntry entry)
        {
            if (_caches.ContainsKey(bundleGUID))
                throw new YooInternalException($"Cache entry already existed: {bundleGUID}");

            _caches.Add(bundleGUID, entry);
        }

        /// <summary>
        /// 获取Catalog文件加载路径
        /// </summary>
        internal string GetCatalogBinaryFileLoadPath()
        {
            return PathUtility.Combine(RootPath, BuiltinFileCatalogDefine.BinaryFileName);
        }
        #endregion
    }
}