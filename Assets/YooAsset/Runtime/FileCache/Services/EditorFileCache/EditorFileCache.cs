using System;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 编辑器文件缓存系统，用于编辑器模式下的资源模拟加载
    /// </summary>
    internal class EditorFileCache : IFileCache
    {
        /// <summary>
        /// 编辑器文件缓存配置
        /// </summary>
        internal struct CacheConfig
        {
            /// <summary>
            /// 虚拟下载模式，模拟资源下载流程
            /// </summary>
            public bool VirtualDownloadMode { get; set; }

            /// <summary>
            /// 虚拟WebGL模式
            /// </summary>
            public bool VirtualWebGLMode { get; set; }

            /// <summary>
            /// 异步模拟最小帧数
            /// </summary>
            public int AsyncSimulateMinFrame { get; set; }

            /// <summary>
            /// 异步模拟最大帧数
            /// </summary>
            public int AsyncSimulateMaxFrame { get; set; }
        }

        private readonly Dictionary<string, EditorFileCacheEntry> _cacheEntries = new Dictionary<string, EditorFileCacheEntry>(10000);

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
        /// 创建编辑器文件缓存系统实例
        /// </summary>
        /// <param name="packageName">包裹名称</param>
        /// <param name="rootPath">缓存根目录</param>
        /// <param name="config">缓存配置</param>
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
        public virtual FCWriteCacheOperation WriteCacheAsync(FCWriteCacheOptions options)
        {
            var operation = new EFCWriteCacheOperation(this, options);
            return operation;
        }
        public virtual FCClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            var operation = new FCClearCacheCompleteOperation();
            return operation;
        }
        public virtual FCVerifyCacheOperation VerifyCacheAsync(FCVerifyCacheOptions options)
        {
            var operation = new FCVerifyCacheCompleteOperation();
            return operation;
        }
        public virtual FCLoadBundleOperation LoadBundleAsync(FCLoadBundleOptions options)
        {
            if (options.Bundle.BundleType == (int)EBundleType.VirtualBundle)
            {
                var operation = new EFCLoadBundleOperation(this, options.Bundle);
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
                return _cacheEntries.ContainsKey(bundleGUID);
            else
                return true;
        }

        #region 内部方法
        /// <summary>
        /// 添加指定缓存
        /// </summary>
        internal void AddEntry(string bundleGUID, EditorFileCacheEntry cacheEntry)
        {
            if (_cacheEntries.ContainsKey(bundleGUID))
                throw new YooInternalException($"Cache entry already exists: {bundleGUID}");

            _cacheEntries.Add(bundleGUID, cacheEntry);
        }
        #endregion
    }
}