using System;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 文件缓存系统接口
    /// </summary>
    internal interface IFileCache : IDisposable
    {
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
        /// 缓存文件数量
        /// </summary>
        int FileCount { get; }

        /// <summary>
        /// 已占用空间（字节）
        /// </summary>
        long SpaceOccupied { get; }


        /// <summary>
        /// 初始化文件缓存系统
        /// </summary>
        FCInitializeOperation InitializeAsync();

        /// <summary>
        /// 写入缓存文件
        /// </summary>
        FCWriteCacheOperation WriteCacheAsync(WriteCacheOptions options);

        /// <summary>
        /// 清理缓存文件
        /// </summary>
        FCClearCacheOperation ClearCacheAsync(ClearCacheOptions options);

        /// <summary>
        /// 验证缓存文件
        /// </summary>
        FCVerifyCacheOperation VerifyCacheAsync(VerifyCacheOptions options);

        /// <summary>
        /// 加载资源包
        /// </summary>
        FCLoadBundleOperation LoadBundleAsync(LoadBundleOptions options);

        /// <summary>
        /// 是否已缓存指定 Bundle
        /// </summary>
        bool IsCached(string bundleGUID);
    }
}
