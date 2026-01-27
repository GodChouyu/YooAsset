
namespace YooAsset
{
    /// <summary>
    /// 资源信息类
    /// </summary>
    public class AssetInfo
    {
        /// <summary>
        /// 资源加载方法枚举
        /// </summary>
        internal enum ELoadMethod
        {
            /// <summary>
            /// 无加载方法
            /// </summary>
            None = 0,

            /// <summary>
            /// 加载单个资源
            /// </summary> 
            LoadAsset,

            /// <summary>
            /// 加载子资源集合
            /// </summary>
            LoadSubAssets,

            /// <summary>
            /// 加载所有资源
            /// </summary>
            LoadAllAssets,

            /// <summary>
            /// 加载场景
            /// </summary>
            LoadScene,

            /// <summary>
            /// 加载原生文件
            /// </summary>
            LoadRawFile,
        }

        private readonly PackageAsset _packageAsset;
        private string _providerGUID;

        /// <summary>
        /// 所属包裹
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// 资源类型
        /// </summary>
        public System.Type AssetType { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; private set; }

        /// <summary>
        /// 加载方法
        /// </summary>
        internal ELoadMethod LoadMethod;

        /// <summary>
        /// 资源对象
        /// </summary>
        internal PackageAsset Asset
        {
            get { return _packageAsset; }
        }

        /// <summary>
        /// 唯一标识符
        /// </summary>
        internal string GUID
        {
            get
            {
                if (string.IsNullOrEmpty(_providerGUID) == false)
                    return _providerGUID;

                if (AssetType == null)
                    _providerGUID = $"[{AssetPath}][null]";
                else
                    _providerGUID = $"[{AssetPath}][{AssetType.Name}]";
                return _providerGUID;
            }
        }

        /// <summary>
        /// 资源信息是否无效
        /// </summary>
        /// <remarks>当内部PackageAsset为空时返回true</remarks>
        public bool IsInvalid
        {
            get
            {
                return _packageAsset == null;
            }
        }

        /// <summary>
        /// 可寻址地址
        /// </summary>
        public string Address
        {
            get
            {
                if (_packageAsset == null)
                    return string.Empty;
                return _packageAsset.Address;
            }
        }

        /// <summary>
        /// 资源路径
        /// </summary>
        public string AssetPath
        {
            get
            {
                if (_packageAsset == null)
                    return string.Empty;
                return _packageAsset.AssetPath;
            }
        }

        /// <summary>
        /// 创建有效的资源信息
        /// </summary>
        /// <param name="packageName">所属包裹名称</param>
        /// <param name="packageAsset">清单中的资源对象</param>
        /// <param name="assetType">资源类型</param>
        internal AssetInfo(string packageName, PackageAsset packageAsset, System.Type assetType)
        {
            if (packageAsset == null)
                throw new YooInternalException("Package asset cannot be null.");

            _providerGUID = string.Empty;
            _packageAsset = packageAsset;
            PackageName = packageName;
            AssetType = assetType;
            Error = string.Empty;
        }

        /// <summary>
        /// 创建无效的资源信息
        /// </summary>
        /// <param name="packageName">所属包裹名称</param>
        /// <param name="error">错误信息</param>
        internal AssetInfo(string packageName, string error)
        {
            _providerGUID = string.Empty;
            _packageAsset = null;
            PackageName = packageName;
            AssetType = null;
            Error = error;
        }
    }
}