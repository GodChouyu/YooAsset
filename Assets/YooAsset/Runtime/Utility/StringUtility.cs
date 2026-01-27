using System;
using System.Text;

namespace YooAsset
{
    /// <summary>
    /// 字符串工具类
    /// </summary>
    internal static class StringUtility
    {
        [ThreadStatic]
        private static StringBuilder _cacheBuilder = new StringBuilder(2048);

        /// <summary>
        /// 格式化字符串（无GC优化版本）
        /// </summary>
        public static string Format(string format, object arg0)
        {
            if (string.IsNullOrEmpty(format))
                throw new ArgumentNullException();

            _cacheBuilder.Length = 0;
            _cacheBuilder.AppendFormat(format, arg0);
            return _cacheBuilder.ToString();
        }

        /// <summary>
        /// 格式化字符串（无GC优化版本）
        /// </summary>
        public static string Format(string format, object arg0, object arg1)
        {
            if (string.IsNullOrEmpty(format))
                throw new ArgumentNullException();

            _cacheBuilder.Length = 0;
            _cacheBuilder.AppendFormat(format, arg0, arg1);
            return _cacheBuilder.ToString();
        }

        /// <summary>
        /// 格式化字符串（无GC优化版本）
        /// </summary>
        public static string Format(string format, object arg0, object arg1, object arg2)
        {
            if (string.IsNullOrEmpty(format))
                throw new ArgumentNullException();

            _cacheBuilder.Length = 0;
            _cacheBuilder.AppendFormat(format, arg0, arg1, arg2);
            return _cacheBuilder.ToString();
        }

        /// <summary>
        /// 格式化字符串（无GC优化版本）
        /// </summary>
        public static string Format(string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
                throw new ArgumentNullException();

            if (args == null)
                throw new ArgumentNullException();

            _cacheBuilder.Length = 0;
            _cacheBuilder.AppendFormat(format, args);
            return _cacheBuilder.ToString();
        }
    }
}