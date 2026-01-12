using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 资源系统的诊断报告
    /// </summary>
    [Serializable]
    internal class DiagnosticReport
    {
        /// <summary>
        /// 调试器版本
        /// </summary>
        public string DebuggerVersion = DiagnosticSystemDefine.DebuggerVersion;

        /// <summary>
        /// 报告发生的游戏帧
        /// </summary>
        public int FrameCount;

        /// <summary>
        /// 包裹数据列表
        /// </summary>
        public List<DiagnosticPackageData> PackageDataList = new List<DiagnosticPackageData>(10);

        /// <summary>
        /// 序列化诊断报告为字节数组
        /// </summary>
        /// <param name="report">要序列化的诊断报告</param>
        /// <returns>UTF-8 编码的 JSON 字节数组</returns>
        public static byte[] Serialize(DiagnosticReport report)
        {
            return Encoding.UTF8.GetBytes(JsonUtility.ToJson(report));
        }

        /// <summary>
        /// 从字节数组反序列化诊断报告
        /// </summary>
        /// <param name="data">UTF-8 编码的 JSON 字节数组</param>
        /// <returns>反序列化后的诊断报告</returns>
        public static DiagnosticReport Deserialize(byte[] data)
        {
            return JsonUtility.FromJson<DiagnosticReport>(Encoding.UTF8.GetString(data));
        }
    }
}