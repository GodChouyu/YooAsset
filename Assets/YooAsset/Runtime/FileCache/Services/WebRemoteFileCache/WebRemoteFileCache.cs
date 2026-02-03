using System;
using System.Collections.Generic;

namespace YooAsset
{
    internal class WebRemoteFileCache : IFileCache
    {
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
        }

        private readonly Dictionary<string, WebRemoteFileCacheEntry> _caches = new Dictionary<string, WebRemoteFileCacheEntry>(10000);

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
                return 0;
            }
        }

        /// <summary>
        /// 已占用空间
        /// 说明：按缓存索引累计
        /// </summary>
        public long SpaceOccupied { get; private set; }
        #endregion

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
        public virtual FCWriteCacheOperation WriteCacheAsync(WriteCacheOptions options)
        {
            var operation = new FCWriteCacheCompleteOperation($"{nameof(WebRemoteFileCache)} is readonly.");
            return operation;
        }
        public virtual FCClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            var operation = new FCClearCacheCompleteOperation($"{nameof(WebRemoteFileCache)} is readonly.");
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
                var operation = new WRFCLoadAssetBundleOperation(this, options);
                return operation;
            }
            else
            {
                string error = $"{nameof(WebServerFileCache)} not support load bundle type : {options.Bundle.BundleType}";
                var operation = new FCLoadBundleErrorOperation(error);
                return operation;
            }
        }
        public virtual bool IsCached(string bundleGUID)
        {
            return true;
        }

        #region 内部方法
        public WebRemoteFileCacheEntry GetEntry(PackageBundle bundle)
        {
            if (_caches.TryGetValue(bundle.BundleGUID, out WebRemoteFileCacheEntry entry))
            {
                return entry;
            }
            else
            {
                string mainURL = Config.RemoteServices.GetRemoteMainURL(bundle.FileName);
                string fallbackURL = Config.RemoteServices.GetRemoteFallbackURL(bundle.FileName);
                var newEntry = new WebRemoteFileCacheEntry(bundle.BundleGUID, mainURL, fallbackURL);
                _caches.Add(bundle.BundleGUID, newEntry);
                return newEntry;
            }
        }
        #endregion
    }
}