using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 网络请求失败计数器（诊断用）
    /// </summary>
    /// <remarks>
    /// 线程安全：内部使用 Dictionary 且未加锁，约定只在 Unity 主线程调用。
    /// 如需在多线程/回调线程调用，请在外层加锁或改为并发容器实现。
    /// </remarks>
    internal class DownloadFailureCounter
    {
#if UNITY_EDITOR
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize()
        {
            _failureRecords.Clear();
        }
#endif

        /// <summary>
        /// 失败计数记录表
        /// </summary>
        /// <remarks>
        /// Key 格式：$"{packageName}_{eventName}"
        /// </remarks>
        private static readonly Dictionary<string, int> _failureRecords = new Dictionary<string, int>(1000);

        /// <summary>
        /// 记录一次失败
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <param name="eventName">事件名称</param>
        public static void RecordFailure(string packageName, string eventName)
        {
            string key = $"{packageName}_{eventName}";
            if (_failureRecords.ContainsKey(key) == false)
                _failureRecords.Add(key, 0);
            _failureRecords[key]++;
        }

        /// <summary>
        /// 获取失败次数
        /// </summary>
        /// <param name="packageName">资源包名称</param>
        /// <param name="eventName">事件名称</param>
        /// <returns>失败次数，如果未记录过则返回 0</returns>
        public static int GetFailureCount(string packageName, string eventName)
        {
            string key = $"{packageName}_{eventName}";
            if (_failureRecords.TryGetValue(key, out int count))
                return count;
            return 0;
        }
    }
}
