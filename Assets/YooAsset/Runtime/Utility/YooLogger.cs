using System.Diagnostics;

namespace YooAsset
{
    /// <summary>
    /// 自定义日志处理接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 输出普通日志
        /// </summary>
        void Log(string message);

        /// <summary>
        /// 输出警告日志
        /// </summary>
        void Warning(string message);

        /// <summary>
        /// 输出错误日志
        /// </summary>
        void Error(string message);

        /// <summary>
        /// 输出异常日志
        /// </summary>
        void Exception(System.Exception exception);
    }

    /// <summary>
    /// YooAsset内部日志系统
    /// </summary>
    internal static class YooLogger
    {
        /// <summary>
        /// 自定义日志处理器实例
        /// </summary>
        public static ILogger LoggerInstance = null;

        /// <summary>
        /// 日志
        /// </summary>
        [Conditional("DEBUG")]
        public static void Log(string info)
        {
            if (LoggerInstance != null)
            {
                LoggerInstance.Log(info);
            }
            else
            {
                UnityEngine.Debug.Log(info);
            }
        }

        /// <summary>
        /// 警告
        /// </summary>
        public static void Warning(string info)
        {
            if (LoggerInstance != null)
            {
                LoggerInstance.Warning(info);
            }
            else
            {
                UnityEngine.Debug.LogWarning(info);
            }
        }

        /// <summary>
        /// 错误
        /// </summary>
        public static void Error(string info)
        {
            if (LoggerInstance != null)
            {
                LoggerInstance.Error(info);
            }
            else
            {
                UnityEngine.Debug.LogError(info);
            }
        }

        /// <summary>
        /// 异常
        /// </summary>
        public static void Exception(System.Exception exception)
        {
            if (LoggerInstance != null)
            {
                LoggerInstance.Exception(exception);
            }
            else
            {
                UnityEngine.Debug.LogException(exception);
            }
        }
    }
}