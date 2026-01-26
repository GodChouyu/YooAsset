using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 文件缓存系统接口
    /// </summary>
    /// <typeparam name="TEntry">缓存记录类型</typeparam>
    internal interface IFileCache<TEntry> where TEntry : ICacheEntry
    {
        #region 状态属性
        /// <summary>
        /// 包裹名称
        /// </summary>
        string PackageName { get; }

        /// <summary>
        /// 缓存根目录
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// 只读属性
        /// </summary>
        bool IsReadOnly { get; }

        /// <summary>
        /// 已占用空间（字节）
        /// </summary>
        long SpaceOccupied { get; }
        #endregion

        #region 异步操作
        /// <summary>
        /// 初始化缓存
        /// </summary>
        FCInitializeOperation InitializeAsync(FCInitializeOptions options);

        /// <summary>
        /// 存储缓存文件
        /// </summary>
        FCStoreCacheOperation StoreCacheAsync(FCStoreCacheOptions options);

        /// <summary>
        /// 清理缓存文件
        /// </summary>
        FCClearCacheOperation ClearCacheAsync(FCClearCacheOptions options);
        #endregion

        #region 查询方法
        /// <summary>
        /// 是否已缓存指定 Bundle
        /// </summary>
        bool IsCached(string bundleGUID);

        /// <summary>
        /// 获取缓存记录
        /// </summary>
        TEntry GetEntry(string bundleGUID);

        /// <summary>
        /// 获取所有的缓存记录
        /// </summary>
        IReadOnlyCollection<TEntry> GetAllEntries();
        #endregion
    }
}
