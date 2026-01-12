using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 描述资源包的运行时诊断信息
    /// </summary>
    [Serializable]
    internal struct DiagnosticBundleInfo : IComparer<DiagnosticBundleInfo>, IComparable<DiagnosticBundleInfo>
    {
        /// <summary>
        /// 资源包名称
        /// </summary>
        public string BundleName;

        /// <summary>
        /// 引用计数
        /// </summary>
        public int ReferenceCount;

        /// <summary>
        /// 资源包的加载状态
        /// </summary>
        public string Status;

        /// <summary>
        /// 该资源包被谁引用
        /// </summary>
        public List<string> ReferencedByBundles;

        public int CompareTo(DiagnosticBundleInfo other)
        {
            return Compare(this, other);
        }
        public int Compare(DiagnosticBundleInfo a, DiagnosticBundleInfo b)
        {
            return string.CompareOrdinal(a.BundleName, b.BundleName);
        }
    }
}