using System;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 沙盒文件缓存系统，用于管理下载到本地的资源包缓存
    /// </summary>
    internal class SandboxFileCache : IFileCache
    {
        /// <summary>
        /// 沙盒文件缓存配置
        /// </summary>
        internal struct CacheConfig
        {
            /// <summary>
            /// 文件校验最大并发数
            /// </summary>
            public int FileVerifyMaxConcurrency { get; set; }

            /// <summary>
            /// 文件校验级别
            /// </summary>
            public EFileVerifyLevel FileVerifyLevel { get; set; }

            /// <summary>
            /// AssetBundle 解密器
            /// </summary>
            public IBundleDecryptor AssetBundleDecryptor { get; set; }

            /// <summary>
            /// RawBundle 解密器
            /// </summary>
            public IBundleDecryptor RawBundleDecryptor { get; set; }

            /// <summary>
            /// AssetBundle 备用解密器
            /// </summary>
            public IBundleMemoryDecryptor AssetBundleFallbackDecryptor { get; set; }
        }

        private const int HashFolderNameLength = 2;
        private readonly Dictionary<string, SandboxFileCacheEntry> _cacheEntries = new Dictionary<string, SandboxFileCacheEntry>(10000);
        private readonly Dictionary<string, string> _dataFilePathMapping = new Dictionary<string, string>(10000);
        private readonly Dictionary<string, string> _infoFilePathMapping = new Dictionary<string, string>(10000);

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
        /// 创建沙盒文件缓存系统实例
        /// </summary>
        /// <param name="packageName">包裹名称</param>
        /// <param name="rootPath">缓存根目录</param>
        /// <param name="config">缓存配置</param>
        public SandboxFileCache(string packageName, string rootPath, CacheConfig config)
        {
            PackageName = packageName;
            RootPath = rootPath;
            Config = config;
            IsReadOnly = false;
        }
        public void Dispose()
        {
        }
        public virtual FCInitializeOperation InitializeAsync()
        {
            var operation = new SFCInitializeOperation(this);
            return operation;
        }
        public virtual FCWriteCacheOperation WriteCacheAsync(FCWriteCacheOptions options)
        {
            var operation = new SFCWriteCacheOperation(this, options);
            return operation;
        }
        public virtual FCClearCacheOperation ClearCacheAsync(FCClearCacheOptions options)
        {
            var operation = new SFCClearCacheOperation(this, options);
            return operation;
        }
        public virtual FCVerifyCacheOperation VerifyCacheAsync(FCVerifyCacheOptions options)
        {
            var operation = new SFCVerifyCacheOperation(this, options);
            return operation;
        }
        public virtual FCLoadBundleOperation LoadBundleAsync(FCLoadBundleOptions options)
        {
            if (options.Bundle.BundleType == (int)EBundleType.AssetBundle)
            {
                var operation = new SFCLoadAssetBundleOperation(this, options.Bundle);
                return operation;
            }
            else if (options.Bundle.BundleType == (int)EBundleType.RawBundle)
            {
                var operation = new SFCLoadRawBundleOperation(this, options.Bundle);
                return operation;
            }
            else
            {
                string error = $"{nameof(SandboxFileCache)} does not support bundle type: {options.Bundle.BundleType}";
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
        /// 获取 Bundle 数据文件路径
        /// </summary>
        internal string GetDataFilePath(PackageBundle bundle)
        {
            if (_dataFilePathMapping.TryGetValue(bundle.BundleGUID, out string filePath) == false)
            {
                string folderName = GetHashFolderName(bundle.FileHash);
                filePath = PathUtility.Combine(RootPath, folderName, bundle.BundleGUID, SandboxFileCacheConsts.BundleDataFileName);
                _dataFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 获取 Bundle 信息文件路径
        /// </summary>
        internal string GetInfoFilePath(PackageBundle bundle)
        {
            if (_infoFilePathMapping.TryGetValue(bundle.BundleGUID, out string filePath) == false)
            {
                string folderName = GetHashFolderName(bundle.FileHash);
                filePath = PathUtility.Combine(RootPath, folderName, bundle.BundleGUID, SandboxFileCacheConsts.BundleInfoFileName);
                _infoFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 获取 Bundle 数据临时文件路径
        /// </summary>
        internal string GetDataTempFilePath(PackageBundle bundle)
        {
            string folderName = GetHashFolderName(bundle.FileHash);
            return PathUtility.Combine(RootPath, folderName, bundle.BundleGUID, SandboxFileCacheConsts.BundleDataTempFileName);
        }

        /// <summary>
        /// 获取 Bundle 信息临时文件路径
        /// </summary>
        internal string GetInfoTempFilePath(PackageBundle bundle)
        {
            string folderName = GetHashFolderName(bundle.FileHash);
            return PathUtility.Combine(RootPath, folderName, bundle.BundleGUID, SandboxFileCacheConsts.BundleInfoTempFileName);
        }

        /// <summary>
        /// 获取指定缓存条目
        /// </summary>
        internal SandboxFileCacheEntry GetEntry(string bundleGUID)
        {
            if (_cacheEntries.TryGetValue(bundleGUID, out SandboxFileCacheEntry entry))
                return entry;
            else
                return null;
        }

        /// <summary>
        /// 获取所有缓存条目
        /// </summary>
        internal IReadOnlyCollection<SandboxFileCacheEntry> GetAllEntries()
        {
            return _cacheEntries.Values;
        }

        /// <summary>
        /// 添加指定缓存条目
        /// </summary>
        internal void AddEntry(string bundleGUID, SandboxFileCacheEntry cacheEntry)
        {
            if (_cacheEntries.ContainsKey(bundleGUID))
                throw new YooInternalException($"Cache entry already exists: {bundleGUID}");

            _cacheEntries.Add(bundleGUID, cacheEntry);
            SpaceOccupied += cacheEntry.GetFileSize();
        }

        /// <summary>
        /// 删除指定缓存条目
        /// </summary>
        internal void RemoveEntry(string bundleGUID)
        {
            if (_cacheEntries.TryGetValue(bundleGUID, out SandboxFileCacheEntry entry))
            {
                _cacheEntries.Remove(bundleGUID);
                _dataFilePathMapping.Remove(bundleGUID);
                _infoFilePathMapping.Remove(bundleGUID);
                SpaceOccupied -= entry.GetFileSize();
                entry.Delete();
            }
        }

        private string GetHashFolderName(string fileHash)
        {
            if (string.IsNullOrEmpty(fileHash))
                throw new YooInternalException();

            if (fileHash.Length <= HashFolderNameLength)
                return fileHash;
            return fileHash.Substring(0, HashFolderNameLength);
        }
        #endregion
    }
}