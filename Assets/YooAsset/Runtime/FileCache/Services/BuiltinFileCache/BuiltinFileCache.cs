using System;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 内置文件缓存系统，用于管理 StreamingAssets 中的资源包
    /// </summary>
    internal class BuiltinFileCache : IFileCache
    {
        /// <summary>
        /// 内置文件缓存配置
        /// </summary>
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
            /// 下载后台
            /// </summary>
            public IDownloadBackend DownloadBackend { get; set; }
        }

        private readonly Dictionary<string, BuiltinFileCacheEntry> _cacheEntries = new Dictionary<string, BuiltinFileCacheEntry>(10000);

        /// <summary>
        /// 缓存配置
        /// </summary>
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
                return _cacheEntries.Count;
            }
        }

        /// <summary>
        /// 已占用空间
        /// 说明：按缓存索引累计
        /// </summary>
        public long SpaceOccupied { get; private set; }
        #endregion

        /// <summary>
        /// 创建内置文件缓存系统实例
        /// </summary>
        /// <param name="packageName">包裹名称</param>
        /// <param name="rootPath">缓存根目录</param>
        /// <param name="config">缓存配置</param>
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
        public virtual FCWriteCacheOperation WriteCacheAsync(FCWriteCacheOptions options)
        {
            var operation = new FCWriteCacheCompleteOperation($"{nameof(BuiltinFileCache)} is readonly.");
            return operation;
        }
        public virtual FCClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            var operation = new FCClearCacheCompleteOperation($"{nameof(BuiltinFileCache)} is readonly.");
            return operation;
        }
        public virtual FCVerifyCacheOperation VerifyCacheAsync(FCVerifyCacheOptions options)
        {
            var operation = new FCVerifyCacheCompleteOperation();
            return operation;
        }
        public virtual FCLoadBundleOperation LoadBundleAsync(FCLoadBundleOptions options)
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
            return _cacheEntries.ContainsKey(bundleGUID);
        }

        #region 内部方法
        /// <summary>
        /// 获取指定缓存
        /// </summary>
        internal BuiltinFileCacheEntry GetEntry(string bundleGUID)
        {
            if (_cacheEntries.TryGetValue(bundleGUID, out BuiltinFileCacheEntry entry))
                return entry;
            else
                return null;
        }

        /// <summary>
        /// 添加指定缓存
        /// </summary>
        internal void AddEntry(string bundleGUID, BuiltinFileCacheEntry cacheEntry)
        {
            if (_cacheEntries.ContainsKey(bundleGUID))
                throw new YooInternalException($"Cache entry already exists: {bundleGUID}");

            _cacheEntries.Add(bundleGUID, cacheEntry);
        }

        /// <summary>
        /// 获取Catalog文件加载路径
        /// </summary>
        internal string GetCatalogBinaryFileLoadPath()
        {
            return PathUtility.Combine(RootPath, BuiltinCatalogDefine.BinaryFileName);
        }
        #endregion
    }
}