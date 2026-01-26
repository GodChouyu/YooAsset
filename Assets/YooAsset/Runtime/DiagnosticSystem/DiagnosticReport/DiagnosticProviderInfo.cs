using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 描述资源加载的运行时诊断信息
    /// </summary>
    [Serializable]
    internal struct DiagnosticProviderInfo : IComparer<DiagnosticProviderInfo>, IComparable<DiagnosticProviderInfo>
    {
        /// <summary>
        /// 资源对象路径
        /// </summary>
        public string AssetPath;

        /// <summary>
        /// 资源加载时的活跃场景
        /// </summary>
        public string SpawnScene;
        
        /// <summary>
        /// 资源加载开始时间
        /// </summary>
        public string StartTime;

        /// <summary>
        /// 加载耗时（单位：毫秒）
        /// </summary>
        public long ElapsedMilliseconds;

        /// <summary>
        /// 引用计数
        /// </summary>
        public int ReferenceCount;

        /// <summary>
        /// 资源的加载状态
        /// </summary>
        public string Status;

        /// <summary>
        /// 资源依赖的资源包列表
        /// </summary>
        public List<string> Dependencies;

        public int CompareTo(DiagnosticProviderInfo other)
        {
            return Compare(this, other);
        }
        public int Compare(DiagnosticProviderInfo a, DiagnosticProviderInfo b)
        {
            return string.CompareOrdinal(a.AssetPath, b.AssetPath);
        }
    }
}