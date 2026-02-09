using System;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// Web远端文件缓存系统，用于从远程服务器加载资源
    /// </summary>
    internal class WebRemoteFileCache : IFileCache
    {
        /// <summary>
        /// Web远端文件缓存配置
        /// </summary>
        internal struct CacheConfig
        {
            /// <summary>
            /// 看门狗超时时间
            /// </summary>
            public int WatchdogTimeout { get; set; }

            /// <summary>
            /// 禁用Unity的网络缓存
            /// </summary>
            public bool DisableUnityWebCache { get; set; }

            /// <summary>
            /// 下载数据校验级别
            /// </summary>
            public EFileVerifyLevel DownloadVerifyLevel { get; set; }

            /// <summary>
            /// AssetBundle 解密器
            /// </summary>
            public IBundleDecryptor AssetBundleDecryptor { get; set; }

            /// <summary>
            /// 远程服务接口
            /// </summary>
            public IRemoteServices RemoteServices { get; set; }

            /// <summary>
            /// 下载后台接口
            /// </summary>
            public IDownloadBackend DownloadBackend { get; set; }

            /// <summary>
            /// 下载重试判定策略
            /// </summary>
            public IDownloadRetryPolicy RetryPolicy { get; set; }

            /// <summary>
            /// URL 选择策略
            /// </summary>
            public IDownloadURLPolicy URLPolicy { get; set; }
        }

        private readonly Dictionary<string, WebRemoteFileCacheEntry> _cacheEntries = new Dictionary<string, WebRemoteFileCacheEntry>(10000);

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
        /// 创建Web远端文件缓存系统实例
        /// </summary>
        /// <param name="packageName">包裹名称</param>
        /// <param name="rootPath">缓存根目录</param>
        /// <param name="config">缓存配置</param>
        public WebRemoteFileCache(string packageName, string rootPath, CacheConfig config)
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
            var operation = new WRFCInitializeOperation(this);
            return operation;
        }
        public virtual FCWriteCacheOperation WriteCacheAsync(FCWriteCacheOptions options)
        {
            var operation = new FCWriteCacheCompleteOperation($"{nameof(WebRemoteFileCache)} is readonly.");
            return operation;
        }
        public virtual FCClearCacheOperation ClearCacheAsync(FCClearCacheOptions options)
        {
            var operation = new FCClearCacheCompleteOperation($"{nameof(WebRemoteFileCache)} is readonly.");
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
                var operation = new WRFCLoadAssetBundleOperation(this, options);
                return operation;
            }
            else
            {
                string error = $"{nameof(WebRemoteFileCache)} does not support bundle type: {options.Bundle.BundleType}";
                var operation = new FCLoadBundleErrorOperation(error);
                return operation;
            }
        }
        public virtual bool IsCached(string bundleGUID)
        {
            return true;
        }

        #region 内部方法
        /// <summary>
        /// 获取或创建指定资源包的缓存条目
        /// </summary>
        internal WebRemoteFileCacheEntry GetEntry(PackageBundle bundle)
        {
            if (_cacheEntries.TryGetValue(bundle.BundleGUID, out WebRemoteFileCacheEntry entry))
            {
                return entry;
            }
            else
            {
                var urls = Config.RemoteServices.GetRemoteURLs(bundle.FileName);
                var newEntry = new WebRemoteFileCacheEntry(bundle.BundleGUID, urls);
                _cacheEntries.Add(bundle.BundleGUID, newEntry);
                return newEntry;
            }
        }
        #endregion
    }
}