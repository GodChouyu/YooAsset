using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 清单文件
    /// </summary>
    [Serializable]
    internal class PackageManifest
    {
        /// <summary>
        /// 文件版本
        /// </summary>
        public string FileVersion;

        /// <summary>
        /// 启用可寻址资源定位
        /// </summary>
        public bool EnableAddressable;

        /// <summary>
        /// 支持无后缀名的资源定位地址
        /// </summary>
        public bool SupportExtensionless;

        /// <summary>
        /// 资源定位地址大小写不敏感
        /// </summary>
        public bool LocationToLower;

        /// <summary>
        /// 包含资源GUID数据
        /// </summary>
        public bool IncludeAssetGUID;

        /// <summary>
        /// 使用可寻址地址代替资源路径
        /// </summary>
        public bool ReplaceAssetPathWithAddress;

        /// <summary>
        /// 文件名称样式
        /// </summary>
        public int OutputNameStyle;

        /// <summary>
        /// 构建资源包类型
        /// </summary>
        public int BuildBundleType;

        /// <summary>
        /// 构建管线名称
        /// </summary>
        public string BuildPipeline;

        /// <summary>
        /// 资源包裹名称
        /// </summary>
        public string PackageName;

        /// <summary>
        /// 资源包裹的版本信息
        /// </summary>
        public string PackageVersion;

        /// <summary>
        /// 资源包裹的备注信息
        /// </summary>
        public string PackageNote;

        /// <summary>
        /// 资源列表（主动收集的资源列表）
        /// </summary>
        public List<PackageAsset> AssetList = new List<PackageAsset>();

        /// <summary>
        /// 资源包列表
        /// </summary>
        public List<PackageBundle> BundleList = new List<PackageBundle>();

        /// <summary>
        /// 资源映射集合（提供AssetPath获取PackageAsset）
        /// </summary>
        [NonSerialized]
        public Dictionary<string, PackageAsset> AssetDic;

        /// <summary>
        /// 资源路径映射集合（提供Location获取AssetPath）
        /// </summary>
        [NonSerialized]
        public Dictionary<string, string> AssetPathByLocation;

        /// <summary>
        /// 资源路径映射集合（提供AssetGUID获取AssetPath）
        /// </summary>
        [NonSerialized]
        public Dictionary<string, string> AssetPathByAssetGUID;

        /// <summary>
        /// 资源包集合（提供BundleName获取PackageBundle）
        /// </summary>
        [NonSerialized]
        public Dictionary<string, PackageBundle> BundleByBundleName;

        /// <summary>
        /// 资源包集合（提供FileName获取PackageBundle）
        /// </summary>
        [NonSerialized]
        public Dictionary<string, PackageBundle> BundleByFileName;

        /// <summary>
        /// 资源包集合（提供BundleGUID获取PackageBundle）
        /// </summary>
        [NonSerialized]
        public Dictionary<string, PackageBundle> BundleByBundleGUID;


        /// <summary>
        /// 初始化资源清单
        /// </summary>
        public void Initialize()
        {
            // 填充资源包内包含的主资源列表
            foreach (var packageAsset in AssetList)
            {
                int bundleID = packageAsset.BundleID;
                if (bundleID >= 0 && bundleID < BundleList.Count)
                {
                    var packageBundle = BundleList[bundleID];
                    packageBundle.IncludeMainAssets.Add(packageAsset);
                }
                else
                {
                    throw new YooManifestException($"Invalid bundle ID : {bundleID}. Valid range is 0 to {BundleList.Count - 1}");
                }
            }

            // 填充资源包引用关系
            for (int index = 0; index < BundleList.Count; index++)
            {
                var sourceBundle = BundleList[index];
                foreach (int dependIndex in sourceBundle.DependentBundleIDs)
                {
                    var dependBundle = BundleList[dependIndex];
                    dependBundle.AddReferenceBundleID(index);
                }
            }
        }

        /// <summary>
        /// 获取包裹的详细信息
        /// </summary>
        /// <returns>返回包含包裹配置信息的详细信息对象</returns>
        public PackageDetails GetPackageDetails()
        {
            PackageDetails details = new PackageDetails();
            details.FileVersion = FileVersion;
            details.EnableAddressable = EnableAddressable;
            details.SupportExtensionless = SupportExtensionless;
            details.LocationToLower = LocationToLower;
            details.IncludeAssetGUID = IncludeAssetGUID;
            details.ReplaceAssetPathWithAddress = ReplaceAssetPathWithAddress;
            details.OutputNameStyle = OutputNameStyle;
            details.BuildBundleType = BuildBundleType;
            details.BuildPipeline = BuildPipeline;
            details.PackageName = PackageName;
            details.PackageVersion = PackageVersion;
            details.PackageNote = PackageNote;
            details.AssetTotalCount = AssetList.Count;
            details.BundleTotalCount = BundleList.Count;
            return details;
        }

        /// <summary>
        /// 尝试将定位地址映射为资源路径
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <returns>如果映射成功返回资源路径，否则返回空字符串</returns>
        public string TryMappingToAssetPath(string location)
        {
            if (string.IsNullOrEmpty(location))
                return string.Empty;

            if (AssetPathByLocation.TryGetValue(location, out string assetPath))
                return assetPath;
            else
                return string.Empty;
        }

        /// <summary>
        /// 获取主资源包
        /// </summary>
        /// <param name="bundleID">资源包ID</param>
        /// <returns>返回对应的资源包对象</returns>
        /// <remarks>传入的资源包ID必须合法有效，否则会抛出异常</remarks>
        public PackageBundle GetMainPackageBundle(int bundleID)
        {
            if (bundleID >= 0 && bundleID < BundleList.Count)
            {
                var packageBundle = BundleList[bundleID];
                if (packageBundle == null)
                    throw new YooInternalException();
                return packageBundle;
            }
            else
            {
                throw new YooManifestException($"Invalid bundle ID : {bundleID}. Valid range is 0 to {BundleList.Count - 1}");
            }
        }

        /// <summary>
        /// 获取主资源包
        /// </summary>
        /// <param name="packageAsset">资源对象</param>
        /// <returns>返回资源对象所属的资源包</returns>
        /// <remarks>传入的资源对象必须合法有效，否则会抛出异常</remarks>
        public PackageBundle GetMainPackageBundle(PackageAsset packageAsset)
        {
            return GetMainPackageBundle(packageAsset.BundleID);
        }

        /// <summary>
        /// 获取资源对象的所有依赖资源包列表
        /// </summary>
        /// <param name="packageAsset">资源对象</param>
        /// <returns>返回依赖的资源包列表</returns>
        /// <remarks>框架层查询结果，传入的资源对象必须合法有效</remarks>
        public List<PackageBundle> GetAssetAllDependencies(PackageAsset packageAsset)
        {
            List<PackageBundle> result = new List<PackageBundle>(packageAsset.DependentBundleIDs.Length);
            foreach (var dependID in packageAsset.DependentBundleIDs)
            {
                var dependBundle = GetMainPackageBundle(dependID);
                result.Add(dependBundle);
            }
            return result;
        }

        /// <summary>
        /// 获取资源包的所有依赖资源包列表
        /// </summary>
        /// <param name="packageBundle">资源包对象</param>
        /// <returns>返回依赖的资源包列表</returns>
        /// <remarks>引擎层查询结果，传入的资源包对象必须合法有效</remarks>
        public List<PackageBundle> GetBundleAllDependencies(PackageBundle packageBundle)
        {
            List<PackageBundle> result = new List<PackageBundle>(packageBundle.DependentBundleIDs.Length);
            foreach (var dependID in packageBundle.DependentBundleIDs)
            {
                var dependBundle = GetMainPackageBundle(dependID);
                result.Add(dependBundle);
            }
            return result;
        }

        /// <summary>
        /// 尝试获取包裹的资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <param name="result">输出的资源对象</param>
        /// <returns>如果找到返回true，否则返回false</returns>
        public bool TryGetPackageAsset(string assetPath, out PackageAsset result)
        {
            return AssetDic.TryGetValue(assetPath, out result);
        }

        /// <summary>
        /// 尝试通过文件名获取包裹的资源包
        /// </summary>
        /// <param name="fileName">文件名称</param>
        /// <param name="result">输出的资源包对象</param>
        /// <returns>如果找到返回true，否则返回false</returns>
        public bool TryGetPackageBundleByFileName(string fileName, out PackageBundle result)
        {
            return BundleByFileName.TryGetValue(fileName, out result);
        }

        /// <summary>
        /// 尝试通过资源包名称获取包裹的资源包
        /// </summary>
        /// <param name="bundleName">资源包名称</param>
        /// <param name="result">输出的资源包对象</param>
        /// <returns>如果找到返回true，否则返回false</returns>
        public bool TryGetPackageBundleByBundleName(string bundleName, out PackageBundle result)
        {
            return BundleByBundleName.TryGetValue(bundleName, out result);
        }

        /// <summary>
        /// 尝试通过资源包GUID获取包裹的资源包
        /// </summary>
        /// <param name="bundleGUID">资源包GUID</param>
        /// <param name="result">输出的资源包对象</param>
        /// <returns>如果找到返回true，否则返回false</returns>
        public bool TryGetPackageBundleByBundleGUID(string bundleGUID, out PackageBundle result)
        {
            return BundleByBundleGUID.TryGetValue(bundleGUID, out result);
        }

        /// <summary>
        /// 是否包含指定的资源文件
        /// </summary>
        /// <param name="bundleGUID">资源包GUID</param>
        /// <returns>如果包含返回true，否则返回false</returns>
        public bool IsIncludeBundleFile(string bundleGUID)
        {
            return BundleByBundleGUID.ContainsKey(bundleGUID);
        }

        /// <summary>
        /// 获取所有的资源信息
        /// </summary>
        /// <returns>返回包含所有资源信息的数组</returns>
        public AssetInfo[] GetAllAssetInfos()
        {
            AssetInfo[] result = new AssetInfo[AssetList.Count];
            for (int i = 0; i < AssetList.Count; i++)
            {
                var packageAsset = AssetList[i];
                AssetInfo assetInfo = new AssetInfo(PackageName, packageAsset, null);
                result[i] = assetInfo;
            }
            return result;
        }

        /// <summary>
        /// 根据标签获取资源信息列表
        /// </summary>
        /// <param name="tags">资源标签数组</param>
        /// <returns>返回包含指定标签的资源信息数组</returns>
        public AssetInfo[] GetAssetInfosByTags(string[] tags)
        {
            List<AssetInfo> result = new List<AssetInfo>(AssetList.Count);
            foreach (var packageAsset in AssetList)
            {
                if (packageAsset.HasTag(tags))
                {
                    AssetInfo assetInfo = new AssetInfo(PackageName, packageAsset, null);
                    result.Add(assetInfo);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// 将资源定位地址转换为资源信息
        /// </summary>
        /// <param name="location">资源定位地址</param>
        /// <param name="assetType">资源类型</param>
        /// <returns>返回资源信息对象，如果转换失败会返回一个无效的资源信息</returns>
        public AssetInfo ConvertLocationToAssetInfo(string location, System.Type assetType)
        {
            DebugCheckLocation(location);

            string assetPath = ConvertLocationToAssetInfoMapping(location);
            if (TryGetPackageAsset(assetPath, out PackageAsset packageAsset))
            {
                AssetInfo assetInfo = new AssetInfo(PackageName, packageAsset, assetType);
                return assetInfo;
            }
            else
            {
                string error;
                if (string.IsNullOrEmpty(location))
                    error = $"The location is null or empty.";
                else
                    error = $"The location is invalid : {location}";
                AssetInfo assetInfo = new AssetInfo(PackageName, error);
                return assetInfo;
            }
        }
        private string ConvertLocationToAssetInfoMapping(string location)
        {
            if (string.IsNullOrEmpty(location))
            {
                YooLogger.Error("Failed to map location to asset path, the location is null or empty.");
                return string.Empty;
            }

            if (AssetPathByLocation.TryGetValue(location, out string assetPath))
            {
                return assetPath;
            }
            else
            {
                YooLogger.Warning($"Failed to map location to asset path: {location}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 将资源GUID转换为资源信息
        /// </summary>
        /// <param name="assetGUID">资源GUID</param>
        /// <param name="assetType">资源类型</param>
        /// <returns>返回资源信息对象，如果转换失败会返回一个无效的资源信息</returns>
        public AssetInfo ConvertAssetGUIDToAssetInfo(string assetGUID, System.Type assetType)
        {
            if (IncludeAssetGUID == false)
            {
                YooLogger.Warning("Package manifest does not include asset GUID. Please check asset bundle collector settings.");
                AssetInfo assetInfo = new AssetInfo(PackageName, "AssetGUID data is empty.");
                return assetInfo;
            }

            string assetPath = ConvertAssetGUIDToAssetInfoMapping(assetGUID);
            if (TryGetPackageAsset(assetPath, out PackageAsset packageAsset))
            {
                AssetInfo assetInfo = new AssetInfo(PackageName, packageAsset, assetType);
                return assetInfo;
            }
            else
            {
                string error;
                if (string.IsNullOrEmpty(assetGUID))
                    error = $"The assetGUID is null or empty.";
                else
                    error = $"The assetGUID is invalid : {assetGUID}";
                AssetInfo assetInfo = new AssetInfo(PackageName, error);
                return assetInfo;
            }
        }
        private string ConvertAssetGUIDToAssetInfoMapping(string assetGUID)
        {
            if (string.IsNullOrEmpty(assetGUID))
            {
                YooLogger.Error("Failed to map asset GUID to asset path, the asset GUID is null or empty.");
                return string.Empty;
            }

            if (AssetPathByAssetGUID.TryGetValue(assetGUID, out string assetPath))
            {
                return assetPath;
            }
            else
            {
                YooLogger.Warning($"Failed to map asset GUID to asset path: {assetGUID}");
                return string.Empty;
            }
        }

        #region 调试方法
        [Conditional("DEBUG")]
        private void DebugCheckLocation(string location)
        {
            if (string.IsNullOrEmpty(location) == false)
            {
                // 检查路径末尾是否有空格
                int index = location.LastIndexOf(' ');
                if (index != -1)
                {
                    if (location.Length == index + 1)
                        YooLogger.Warning($"Found blank character in location : \"{location}\"");
                }

                if (location.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
                    YooLogger.Warning($"Found illegal character in location : \"{location}\"");
            }
        }
        #endregion
    }
}