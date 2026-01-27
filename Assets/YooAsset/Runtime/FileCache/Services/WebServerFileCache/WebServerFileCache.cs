using System;
using System.Collections.Generic;

namespace YooAsset
{
    internal class WebServerFileCache : IFileCache
    {
        internal struct CacheConfig
        {
            /// <summary>
            /// 禁用Unity的网络缓存
            /// </summary>
            public bool DisableUnityWebCache { get; set; }

            /// <summary>
            /// 下载后台接口
            /// </summary>
            public IDownloadBackend DownloadBackend { get; set; }

            /// <summary>
            /// 看门狗超时时间
            /// </summary>
            public int WatchdogTimeout { get; set; }

            /// <summary>
            /// 失败后重试次数
            /// </summary>
            public int RetryCount { get; set; }
        }

        private readonly Dictionary<string, WebServerFileCacheEntry> _caches = new Dictionary<string, WebServerFileCacheEntry>(10000);

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

        public WebServerFileCache(string packageName, string rootPath, CacheConfig config)
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
            var operation = new WSFCInitializeOperation(this);
            return operation;
        }
        public virtual FCWriteCacheOperation WriteCacheAsync(WriteCacheOptions options)
        {
            var operation = new FCWriteCacheCompleteOperation($"{nameof(WebServerFileCache)} is readonly.");
            return operation;
        }
        public virtual FCClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            var operation = new FCClearCacheCompleteOperation($"{nameof(WebServerFileCache)} is readonly.");
            return operation;
        }
        public virtual FCVerifyCacheOperation VerifyCacheAsync(VerifyCacheOptions options)
        {
            var operation = new FCVerifyCacheCompleteOperation();
            return operation;
        }
        public virtual FCLoadBundleOperation LoadBundleAsync(LoadBundleOptions options)
        {
            var operation = new WFCLoadAssetBundleOperation(this, options);
            return operation;
        }
        public virtual bool IsCached(string bundleGUID)
        {
            return _caches.ContainsKey(bundleGUID);
        }

        #region 内部方法
        public WebServerFileCacheEntry GetEntry(string bundleGUID)
        {
            if (_caches.TryGetValue(bundleGUID, out WebServerFileCacheEntry entry))
                return entry;
            else
                return null;
        }

        /// <summary>
        /// 添加指定缓存
        /// </summary>
        internal void AddEntry(string bundleGUID, WebServerFileCacheEntry entry)
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