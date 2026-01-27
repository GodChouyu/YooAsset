using System;
using System.Linq;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 清单中的资源包对象
    /// </summary>
    [Serializable]
    internal class PackageBundle
    {
        /// <summary>
        /// 资源包名称
        /// </summary>
        public string BundleName;

        /// <summary>
        /// Unity引擎生成的CRC
        /// </summary>
        public uint UnityCRC;

        /// <summary>
        /// 文件哈希值
        /// </summary>
        public string FileHash;

        /// <summary>
        /// 文件校验码
        /// </summary>
        public uint FileCRC;

        /// <summary>
        /// 文件大小（字节数）
        /// </summary>
        public long FileSize;

        /// <summary>
        /// 文件是否加密
        /// </summary>
        public bool IsEncrypted;

        /// <summary>
        /// 资源包的分类标签
        /// </summary>
        public string[] Tags;

        /// <summary>
        /// 依赖的资源包ID集合
        /// 注意：引擎层构建查询结果
        /// </summary>
        public int[] DependentBundleIDs;

        /// <summary>
        /// 资源包唯一标识符
        /// </summary>
        /// <remarks>使用FileHash作为GUID</remarks>
        public string BundleGUID
        {
            get { return FileHash; }
        }

        /// <summary>
        /// 资源包类型
        /// </summary>
        public int BundleType
        {
            get
            {
                return _bundleType;
            }
        }
        private int _bundleType;

        /// <summary>
        /// 文件名称
        /// </summary>  
        public string FileName
        {
            get
            {
                if (string.IsNullOrEmpty(_fileName))
                    throw new YooInternalException("File name cannot be null or empty.");
                return _fileName;
            }
        }
        private string _fileName;

        /// <summary>
        /// 文件后缀名
        /// </summary>
        public string FileExtension
        {
            get
            {
                if (string.IsNullOrEmpty(_fileExtension))
                    throw new YooInternalException("File extension cannot be null or empty.");
                return _fileExtension;
            }
        }
        private string _fileExtension;

        /// <summary>
        /// 包含的主资源集合
        /// </summary>
        [NonSerialized]
        public readonly List<PackageAsset> IncludeMainAssets = new List<PackageAsset>(10);

        /// <summary>
        /// 引用该资源包的资源包列表
        /// 说明：谁引用了该资源包
        /// </summary>
        [NonSerialized]
        public readonly List<int> ReferenceBundleIDs = new List<int>(10);
        private readonly HashSet<int> _referenceBundleIDs = new HashSet<int>();


        /// <summary>
        /// 创建资源包实例
        /// </summary>
        public PackageBundle()
        {
        }

        /// <summary>
        /// 初始化资源包
        /// </summary>
        /// <param name="manifest">所属的资源清单</param>
        public void Initialize(PackageManifest manifest)
        {
            _manifest = manifest;
            _bundleType = manifest.BuildBundleType;
            _fileExtension = PackageManifestTools.GetRemoteBundleFileExtension(BundleName);
            _fileName = PackageManifestTools.GetRemoteBundleFileName(manifest.OutputNameStyle, BundleName, _fileExtension, FileHash);
        }

        /// <summary>
        /// 添加引用该资源包的资源包ID
        /// </summary>
        /// <param name="bundleID">引用该资源包的资源包ID</param>
        /// <remarks>记录谁引用了该资源包</remarks>
        public void AddReferenceBundleID(int bundleID)
        {
            if (_referenceBundleIDs.Contains(bundleID) == false)
            {
                _referenceBundleIDs.Add(bundleID);
                ReferenceBundleIDs.Add(bundleID);
            }
        }

        /// <summary>
        /// 是否包含指定的标签
        /// </summary>
        /// <param name="tags">要检查的标签数组</param>
        /// <returns>如果包含任意一个标签返回true，否则返回false</returns>
        public bool HasTag(string[] tags)
        {
            if (tags == null || tags.Length == 0)
                return false;
            if (Tags == null || Tags.Length == 0)
                return false;

            foreach (var tag in tags)
            {
                if (Tags.Contains(tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 是否包含任意标签
        /// </summary>
        /// <returns>如果包含至少一个标签返回true，否则返回false</returns>
        public bool HasTags()
        {
            return Tags != null && Tags.Length > 0;
        }

        /// <summary>
        /// 检测资源包文件内容是否相同
        /// </summary>
        /// <param name="otherBundle">要比较的资源包对象</param>
        /// <returns>如果文件哈希值相同返回true，否则返回false</returns>
        public bool Equals(PackageBundle otherBundle)
        {
            if (FileHash == otherBundle.FileHash)
                return true;

            return false;
        }

        #region 调试信息
        private PackageManifest _manifest;
        private List<string> _debugReferenceBundles;

        /// <summary>
        /// 获取引用该资源包的资源包名称列表（仅调试用）
        /// </summary>
        /// <returns>返回引用该资源包的所有资源包名称列表</returns>
        public List<string> GetDebugReferenceBundles()
        {
            if (_debugReferenceBundles == null)
            {
                _debugReferenceBundles = new List<string>(ReferenceBundleIDs.Count);
                foreach (int bundleID in ReferenceBundleIDs)
                {
                    var packageBundle = _manifest.BundleList[bundleID];
                    _debugReferenceBundles.Add(packageBundle.BundleName);
                }
            }
            return _debugReferenceBundles;
        }
        #endregion
    }
}