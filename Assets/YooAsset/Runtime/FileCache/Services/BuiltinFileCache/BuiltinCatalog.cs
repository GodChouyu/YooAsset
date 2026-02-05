using System;
using System.IO;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 内置资源目录
    /// </summary>
    [Serializable]
    internal class BuiltinCatalog
    {
        /// <summary>
        /// 内置资源文件条目
        /// </summary>
        [Serializable]
        public class FileEntry
        {
            /// <summary>
            /// 资源包唯一标识
            /// </summary>
            public string BundleGUID;

            /// <summary>
            /// 资源包文件名
            /// </summary>
            public string FileName;
        }

        /// <summary>
        /// 文件版本
        /// </summary>
        public string FileVersion;

        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName;

        /// <summary>
        /// 包裹版本
        /// </summary>
        public string PackageVersion;

        /// <summary>
        /// 文件条目列表
        /// </summary>
        public List<FileEntry> FileEntries = new List<FileEntry>();
    }
}