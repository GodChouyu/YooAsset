using System;
using System.Collections.Generic;

namespace YooAsset
{
    internal class SandboxFileCache : IFileCache
    {
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

        private const int HashFolderLength = 2;
        private readonly Dictionary<string, SandboxFileCacheEntry> _caches = new Dictionary<string, SandboxFileCacheEntry>(10000);
        private readonly Dictionary<string, string> _dataFilePathMapping = new Dictionary<string, string>(10000);
        private readonly Dictionary<string, string> _infoFilePathMapping = new Dictionary<string, string>(10000);

        // 缓存配置
        internal readonly CacheConfig Config;

        // 共享缓冲区
        internal readonly BufferWriter SharedBuffer = new BufferWriter(1024);

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
        public virtual FCWriteCacheOperation WriteCacheAsync(WriteCacheOptions options)
        {
            var operation = new SFCWriteCacheOperation(this, options);
            return operation;
        }
        public virtual FCClearCacheOperation ClearCacheAsync(ClearCacheOptions options)
        {
            if (options.ClearMode == EFileClearMode.ClearAllBundleFiles.ToString())
            {
                var operation = new SFCClearAllCacheOperation(this, options);
                return operation;
            }
            else if (options.ClearMode == EFileClearMode.ClearUnusedBundleFiles.ToString())
            {
                var operation = new SFCClearUnusedCacheOperation(this, options);
                return operation;
            }
            else if (options.ClearMode == EFileClearMode.ClearBundleFilesByLocations.ToString())
            {
                var operation = new SFCClearCacheByLocationsOperation(this, options);
                return operation;
            }
            else if (options.ClearMode == EFileClearMode.ClearBundleFilesByTags.ToString())
            {
                var operation = new SFCClearCacheByTagsOperation(this, options);
                return operation;
            }
            else
            {
                string error = $"Invalid clear mode : {options.ClearMode}";
                var operation = new FCClearCacheCompleteOperation(error);
                return operation;
            }
        }
        public virtual FCVerifyCacheOperation VerifyCacheAsync(VerifyCacheOptions options)
        {
            var operation = new SFCVerifyCacheOperation(this, options);
            return operation;
        }
        public virtual FCLoadBundleOperation LoadBundleAsync(LoadBundleOptions options)
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
                string error = $"{nameof(SandboxFileCache)} not support load bundle type : {options.Bundle.BundleType}";
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
        /// 获取 Bundle 数据文件路径
        /// </summary>
        internal string GetDataFilePath(PackageBundle bundle)
        {
            if (_dataFilePathMapping.TryGetValue(bundle.BundleGUID, out string filePath) == false)
            {
                string folderName = GetHashFolderName(bundle.FileHash);
                filePath = PathUtility.Combine(RootPath, folderName, bundle.BundleGUID, SandboxFileCacheDefine.BundleDataFileName);
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
                filePath = PathUtility.Combine(RootPath, folderName, bundle.BundleGUID, SandboxFileCacheDefine.BundleInfoFileName);
                _infoFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 获取指定缓存
        /// </summary>
        internal SandboxFileCacheEntry GetEntry(string bundleGUID)
        {
            if (_caches.TryGetValue(bundleGUID, out SandboxFileCacheEntry entry))
                return entry;
            else
                return null;
        }

        /// <summary>
        /// 获取所有缓存
        /// </summary>
        internal IReadOnlyCollection<SandboxFileCacheEntry> GetAllEntries()
        {
            return _caches.Values;
        }

        /// <summary>
        /// 添加指定缓存
        /// </summary>
        internal void AddEntry(string bundleGUID, SandboxFileCacheEntry entry)
        {
            if (_caches.ContainsKey(bundleGUID))
                throw new YooInternalException($"Cache entry already existed: {bundleGUID}");

            _caches.Add(bundleGUID, entry);
            SpaceOccupied += entry.GetFileSize();
        }

        /// <summary>
        /// 删除指定缓存
        /// </summary>
        internal void RemoveEntry(string bundleGUID)
        {
            if (_caches.TryGetValue(bundleGUID, out SandboxFileCacheEntry entry))
            {
                _caches.Remove(bundleGUID);
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

            if (fileHash.Length <= HashFolderLength)
                return fileHash;
            return fileHash.Substring(0, HashFolderLength);
        }
        #endregion
    }
}