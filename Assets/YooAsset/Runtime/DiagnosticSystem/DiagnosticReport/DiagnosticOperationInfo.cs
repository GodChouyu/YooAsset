using System;
using System.Collections;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 描述异步操作的运行时诊断信息
    /// </summary>
    [Serializable]
    internal struct DiagnosticOperationInfo : IComparer<DiagnosticOperationInfo>, IComparable<DiagnosticOperationInfo>
    {
        /// <summary>
        /// 异步操作的名称
        /// </summary>
        public string OperationName;

        /// <summary>
        /// 异步操作的说明
        /// </summary>
        public string OperationDesc;

        /// <summary>
        /// 异步操作的优先级
        /// </summary>
        public uint Priority;

        /// <summary>
        /// 开始的时间
        /// </summary>
        public string StartTime;

        /// <summary>
        /// 处理耗时（单位：毫秒）
        /// </summary>
        public long ElapsedMilliseconds;

        /// <summary>
        /// 异步操作的执行进度
        /// </summary>
        public float Progress;

        /// <summary>
        /// 异步操作的执行状态
        /// </summary>
        public string Status;

        /// <summary>
        /// 子任务列表
        /// TODO：序列化深度限制为10层
        /// </summary>
        public List<DiagnosticOperationInfo> Children;

        public int CompareTo(DiagnosticOperationInfo other)
        {
            return Compare(this, other);
        }
        public int Compare(DiagnosticOperationInfo a, DiagnosticOperationInfo b)
        {
            return string.CompareOrdinal(a.OperationName, b.OperationName);
        }
    }
}