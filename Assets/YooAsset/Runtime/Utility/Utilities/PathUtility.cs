using System;

namespace YooAsset
{
    /// <summary>
    /// 路径工具类
    /// </summary>
    internal static class PathUtility
    {
        /// <summary>
        /// 路径归一化
        /// 注意：替换为Linux路径格式
        /// </summary>
        public static string NormalizePath(string path)
        {
            return path.Replace('\\', '/').Replace("\\", "/");
        }

        /// <summary>
        /// 移除路径里的后缀名
        /// </summary>
        public static string RemoveExtension(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            int index = str.LastIndexOf('.');
            if (index == -1)
                return str;
            else
                return str.Remove(index); //"assets/config/test.unity3d" --> "assets/config/test"
        }

        /// <summary>
        /// URL地址是否包含双斜杠
        /// 注意：只检查协议之后的部分
        /// </summary>
        public static bool HasDoubleSlashes(string url)
        {
            if (url == null)
                throw new ArgumentNullException();

            int protocolIndex = url.IndexOf("://");
            string partToCheck = protocolIndex == -1 ? url : url.Substring(protocolIndex + 3);
            return partToCheck.Contains("//") || partToCheck.Contains(@"\\");
        }

        /// <summary>
        /// 合并路径
        /// </summary>
        public static string Combine(string path1, string path2)
        {
            return StringUtility.Format("{0}/{1}", path1, path2);
        }

        /// <summary>
        /// 合并路径
        /// </summary>
        public static string Combine(string path1, string path2, string path3)
        {
            return StringUtility.Format("{0}/{1}/{2}", path1, path2, path3);
        }

        /// <summary>
        /// 合并路径
        /// </summary>
        public static string Combine(string path1, string path2, string path3, string path4)
        {
            return StringUtility.Format("{0}/{1}/{2}/{3}", path1, path2, path3, path4);
        }
    }
}