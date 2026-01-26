using System.Collections.Generic;
using System.IO;

namespace YooAsset
{
    internal class BundleCache : IFileCache<BundleCacheEntry>
    {
        private const int HashFolderLength = 2;

        // 缓存索引
        private readonly Dictionary<string, BundleCacheEntry> _caches = new Dictionary<string, BundleCacheEntry>(10000);

        // 路径缓存
        private readonly Dictionary<string, string> _dataFilePathMapping = new Dictionary<string, string>(10000);
        private readonly Dictionary<string, string> _infoFilePathMapping = new Dictionary<string, string>(10000);

        // 共享缓冲区
        internal readonly BufferWriter SharedBuffer = new BufferWriter(1024);
        

        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName { get; }

        /// <summary>
        /// 缓存根目录
        /// </summary>
        public string RootPath { get; }

        /// <summary>
        /// 追加文件扩展名
        /// </summary>
        public readonly bool AppendFileExtension;

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


        public BundleCache(string packageName, string rootPath, bool appendFileExtension)
        {
            PackageName = packageName;
            RootPath = rootPath;
            AppendFileExtension = appendFileExtension;
            IsReadOnly = false;
        }

        /// <summary>
        /// 初始化缓存
        /// </summary>
        public FCInitializeOperation InitializeAsync(FCInitializeOptions options)
        {
            var operation = new FCInitializeOperation(this, options);
            return operation;
        }

        /// <summary>
        /// 存储缓存文件
        /// </summary>
        public FCStoreCacheOperation StoreCacheAsync(FCStoreCacheOptions options)
        {
            var operation = new FCStoreCacheOperation(this, options);
            return operation;
        }

        /// <summary>
        /// 清理缓存文件
        /// </summary>
        public FCClearCacheOperation ClearCacheAsync(FCClearCacheOptions options)
        {
            var operation = new FCClearCacheOperation(this, options);
            return operation;
        }

        /// <summary>
        /// 是否已缓存指定 Bundle
        /// </summary>
        public bool IsCached(string bundleGUID)
        {
            return _caches.ContainsKey(bundleGUID);
        }

        /// <summary>
        /// 获取缓存记录
        /// </summary>
        public BundleCacheEntry GetEntry(string bundleGUID)
        {
            if (_caches.TryGetValue(bundleGUID, out BundleCacheEntry entry))
                return entry;
            else
                return null;
        }

        /// <summary>
        /// 获取所有的缓存记录
        /// </summary>
        public IReadOnlyCollection<BundleCacheEntry> GetAllEntries()
        {
            return _caches.Values;
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
                filePath = PathUtility.Combine(RootPath, folderName, bundle.BundleGUID, BundleCacheDefine.BundleDataFileName);
                if (AppendFileExtension)
                    filePath += bundle.FileExtension;
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
                filePath = PathUtility.Combine(RootPath, folderName, bundle.BundleGUID, BundleCacheDefine.BundleInfoFileName);
                _infoFilePathMapping.Add(bundle.BundleGUID, filePath);
            }
            return filePath;
        }

        /// <summary>
        /// 添加指定缓存
        /// </summary>
        internal void AddEntry(string bundleGUID, BundleCacheEntry entry)
        {
            if (_caches.ContainsKey(bundleGUID))
                throw new YooInternalException($"Cache already existed: {bundleGUID}");

            _caches.Add(bundleGUID, entry);
            SpaceOccupied += entry.DataFileSize;
        }

        /// <summary>
        /// 删除指定缓存
        /// </summary>
        internal void RemoveEntry(string bundleGUID)
        {
            if (_caches.TryGetValue(bundleGUID, out BundleCacheEntry entry))
            {
                _caches.Remove(bundleGUID);
                _dataFilePathMapping.Remove(bundleGUID);
                _infoFilePathMapping.Remove(bundleGUID);
                SpaceOccupied -= entry.DataFileSize;
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