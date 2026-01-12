using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 包裹的诊断数据容器
    /// </summary>
    [Serializable]
    internal class DiagnosticPackageData
    {
        /// <summary>
        /// 包裹名称
        /// </summary>
        public string PackageName;

        /// <summary>
        /// 资源加载的诊断信息列表
        /// </summary>
        public List<DiagnosticProviderInfo> ProviderInfos = new List<DiagnosticProviderInfo>(1000);

        /// <summary>
        /// 资源包的诊断信息列表
        /// </summary>
        public List<DiagnosticBundleInfo> BundleInfos = new List<DiagnosticBundleInfo>(1000);

        /// <summary>
        /// 异步操作的诊断信息列表
        /// </summary>
        public List<DiagnosticOperationInfo> OperationInfos = new List<DiagnosticOperationInfo>(1000);

        private readonly Dictionary<string, DiagnosticBundleInfo> _bundleInfoDict = new Dictionary<string, DiagnosticBundleInfo>();
        private bool _isParsed = false;

        /// <summary>
        /// 获取资源包的诊断信息
        /// </summary>
        public DiagnosticBundleInfo GetBundleInfo(string bundleName)
        {
            // 解析数据
            if (_isParsed == false)
            {
                _isParsed = true;
                foreach (var bundleInfo in BundleInfos)
                {
                    if (_bundleInfoDict.ContainsKey(bundleInfo.BundleName) == false)
                    {
                        _bundleInfoDict.Add(bundleInfo.BundleName, bundleInfo);
                    }
                }
            }

            if (_bundleInfoDict.TryGetValue(bundleName, out DiagnosticBundleInfo value))
            {
                return value;
            }
            else
            {
                UnityEngine.Debug.LogError($"Cannot find {nameof(DiagnosticBundleInfo)} : {bundleName}");
                return default;
            }
        }
    }
}