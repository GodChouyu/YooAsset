
namespace YooAsset
{
    /// <summary>
    /// 下载系统工具类
    /// </summary>
    /// <remarks>
    /// 提供跨平台的 URL 转换和判断功能。
    /// </remarks>
    internal class DownloadSystemTools
    {
        /// <summary>
        /// 转换为本地文件请求地址
        /// </summary>
        /// <param name="path">本地文件路径</param>
        /// <returns>可用于 UnityWebRequest 的文件协议 URL</returns>
        public static string ToLocalUrl(string path)
        {
            string url;

            // 获取对应平台的URL地址
            // 说明：苹果不同设备上操作系统不同。
            // 说明：iPhone和iPod对应的是iOS系统。
            // 说明：iPad对应的是iPadOS系统。
            // 说明：AppleTV对应的是tvOS系统。
            // TODO 安卓平台未考虑外部存储器
            // TODO Linux平台确认路径正确
#if UNITY_EDITOR_OSX
            url = StringUtility.Format("file://{0}", path);
#elif UNITY_EDITOR_WIN
            url = StringUtility.Format("file:///{0}", path);
#elif UNITY_WEBGL
            url = path;
#elif UNITY_IOS || UNITY_IPHONE
            url = StringUtility.Format("file://{0}", path);
#elif UNITY_ANDROID
            if (path.StartsWith("jar:file://"))
                url = path;
            else
                url = StringUtility.Format("jar:file://{0}", path);
#elif UNITY_OPENHARMONY
            if (UnityEngine.Application.streamingAssetsPath.StartsWith("jar:file://"))
            {
                if (path.StartsWith("jar:file://"))
                    url = path;
                else
                    url = StringUtility.Format("jar:file://{0}", path);
            }
            else
            {
                if (path.StartsWith("file://"))
                    url = path;
                else
                    url = StringUtility.Format("file://{0}", path);
            }

#elif UNITY_WSA
            url = StringUtility.Format("file:///{0}", path);
#elif UNITY_TVOS
            url = StringUtility.Format("file:///{0}", path);
#elif UNITY_STANDALONE_OSX
            url = new System.Uri(path).ToString();
#elif UNITY_STANDALONE_WIN
            url = StringUtility.Format("file:///{0}", path);
#elif UNITY_STANDALONE_LINUX
            url = StringUtility.Format("file:///root/{0}", path);
#else
            throw new System.NotSupportedException($"Platform '{UnityEngine.Application.platform}' is not supported.");
#endif

            // 处理特殊字符：用户设备路径可能包含特殊字符导致 URL 无法正确识别
            return url.Replace("+", "%2B").Replace("#", "%23").Replace("?", "%3F");
        }

        /// <summary>
        /// 判断是否为本地文件 URL
        /// </summary>
        /// <param name="url">要判断的 URL</param>
        /// <returns>如果是本地文件 URL 返回 true，否则返回 false</returns>
        public static bool IsLocalFileUrl(string url)
        {
            //TODO UNITY_STANDALONE_OSX平台目前无法确定

            // 本地文件传输协议
            if (url.StartsWith("file:"))
                return true;

            // JAR文件协议
            if (url.StartsWith("jar:file:"))
                return true;

            return false;
        }
    }
}
