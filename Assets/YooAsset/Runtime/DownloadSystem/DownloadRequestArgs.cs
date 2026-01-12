using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 文件下载请求参数
    /// </summary>
    /// <remarks>
    /// 用于将下载内容保存到本地文件的请求配置。
    /// 支持断点续传和追加写入模式。
    /// </remarks>
    internal struct DownloadFileRequestArgs
    {
        /// <summary>
        /// 请求地址
        /// </summary>
        public readonly string URL;

        /// <summary>
        /// 响应的超时时间（单位：秒）
        /// </summary>
        /// <remarks>
        /// 当 Timeout 设置为 0 时，不应用超时。
        /// 设置的超时值可能应用于Android上的每个URL重定向，这可能会导致响应时间增加。
        /// </remarks>
        public readonly int Timeout;

        /// <summary>
        /// 看门狗超时时间（单位：秒）
        /// </summary>
        /// <remarks>
        /// 用于监控下载任务的数据接收情况。
        /// 规则说明：
        /// 1. 当设置值为 0 时，表示禁用看门狗监控。
        /// 2. 每次接收到下载数据时，看门狗计时器会重置。
        /// 3. 若在设定的时间范围内未收到任何数据，任务将被自动终止。
        /// </remarks>
        public readonly int WatchdogTimeout;

        /// <summary>
        /// 文件保存路径
        /// </summary>
        public readonly string SavePath;

        /// <summary>
        /// 是否追加写入文件
        /// </summary>
        /// <remarks>
        /// 配合 ResumeOffset 使用，用于断点续传场景。
        /// </remarks>
        public readonly bool AppendToFile;

        /// <summary>
        /// 中止请求时是否删除目标文件
        /// </summary>
        public readonly bool RemoveFileOnAbort;

        /// <summary>
        /// 断点续传的起始字节（小于等于 0 表示不启用）
        /// </summary>
        /// <remarks>
        /// 推荐由后端自动设置 Range 请求头："bytes={ResumeOffset}-"。
        /// </remarks>
        public readonly long ResumeOffset;

        /// <summary>
        /// 自定义请求头（可选）
        /// </summary>
        /// <remarks>
        /// 使用 AddRequestHeader 方法添加请求头。
        /// 注意：相同 key 重复添加会抛出异常。
        /// </remarks>
        public Dictionary<string, string> Headers;

        /// <summary>
        /// 构造文件下载请求参数
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="savePath">文件保存路径</param>
        /// <param name="timeout">响应超时时间（秒），0 表示不应用超时</param>
        /// <param name="watchdogTimeout">看门狗超时时间（秒），0 表示禁用</param>
        /// <param name="appendToFile">是否追加写入文件（用于断点续传）</param>
        /// <param name="removeFileOnAbort">中止时是否删除目标文件</param>
        /// <param name="resumeOffset">断点续传的起始字节位置</param>
        public DownloadFileRequestArgs(
            string url,
            string savePath,
            int timeout,
            int watchdogTimeout,
            bool appendToFile = false,
            bool removeFileOnAbort = true,
            long resumeOffset = 0)
        {
            URL = url;
            SavePath = savePath;
            Timeout = timeout;
            WatchdogTimeout = watchdogTimeout;
            AppendToFile = appendToFile;
            RemoveFileOnAbort = removeFileOnAbort;
            ResumeOffset = resumeOffset;
            Headers = null;
        }

        /// <summary>
        /// 添加请求头数据
        /// </summary>
        public void AddRequestHeader(string name, string value)
        {
            if (Headers == null)
                Headers = new Dictionary<string, string>(10);
            Headers.Add(name, value);
        }
    }

    /// <summary>
    /// 数据下载请求参数（通用）
    /// </summary>
    /// <remarks>
    /// 用于下载到内存的请求配置。
    /// 可用于字节数组（bytes）或文本（text）下载。
    /// </remarks>
    internal struct DownloadDataRequestArgs
    {
        /// <summary>
        /// 请求地址
        /// </summary>
        public readonly string URL;

        /// <summary>
        /// 响应的超时时间（单位：秒）
        /// </summary>
        /// <remarks>
        /// 当 Timeout 设置为 0 时，不应用超时。
        /// 设置的超时值可能应用于Android上的每个URL重定向，这可能会导致响应时间增加。
        /// </remarks>
        public readonly int Timeout;

        /// <summary>
        /// 看门狗超时时间（单位：秒）
        /// </summary>
        /// <remarks>
        /// 用于监控下载任务的数据接收情况。
        /// 规则说明：
        /// 1. 当设置值为 0 时，表示禁用看门狗监控。
        /// 2. 每次接收到下载数据时，看门狗计时器会重置。
        /// 3. 若在设定的时间范围内未收到任何数据，任务将被自动终止。
        /// </remarks>
        public readonly int WatchdogTimeout;

        /// <summary>
        /// 自定义请求头（可选）
        /// </summary>
        /// <remarks>
        /// 使用 AddRequestHeader 方法添加请求头。
        /// 注意：相同 key 重复添加会抛出异常。
        /// </remarks>
        public Dictionary<string, string> Headers;

        /// <summary>
        /// 构造数据下载请求参数
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="timeout">响应超时时间（秒），0 表示不应用超时</param>
        /// <param name="watchdogTimeout">看门狗超时时间（秒），0 表示禁用</param>
        public DownloadDataRequestArgs(string url, int timeout, int watchdogTimeout)
        {
            URL = url;
            Timeout = timeout;
            WatchdogTimeout = watchdogTimeout;
            Headers = null;
        }

        /// <summary>
        /// 添加请求头数据
        /// </summary>
        public void AddRequestHeader(string name, string value)
        {
            if (Headers == null)
                Headers = new Dictionary<string, string>(10);
            Headers.Add(name, value);
        }
    }

    /// <summary>
    /// AssetBundle 下载请求参数
    /// </summary>
    /// <remarks>
    /// 用于下载并加载 Unity AssetBundle 的请求配置。
    /// 支持 Unity 内置缓存机制和 CRC 校验。
    /// </remarks>
    internal struct DownloadAssetBundleRequestArgs
    {
        /// <summary>
        /// 请求地址
        /// </summary>
        public readonly string URL;

        /// <summary>
        /// 响应的超时时间（单位：秒）
        /// </summary>
        /// <remarks>
        /// 当 Timeout 设置为 0 时，不应用超时。
        /// 设置的超时值可能应用于Android上的每个URL重定向，这可能会导致响应时间增加。
        /// </remarks>
        public readonly int Timeout;

        /// <summary>
        /// 看门狗超时时间（单位：秒）
        /// </summary>
        /// <remarks>
        /// 用于监控下载任务的数据接收情况。
        /// 规则说明：
        /// 1. 当设置值为 0 时，表示禁用看门狗监控。
        /// 2. 每次接收到下载数据时，看门狗计时器会重置。
        /// 3. 若在设定的时间范围内未收到任何数据，任务将被自动终止。
        /// </remarks>
        public readonly int WatchdogTimeout;

        /// <summary>
        /// 禁用 Unity 的网络缓存
        /// </summary>
        public readonly bool DisableUnityWebCache;

        /// <summary>
        /// AssetBundle 文件哈希（用于 UnityWebRequest 的缓存）
        /// </summary>
        /// <remarks>
        /// 仅当 DisableUnityWebCache 为 false 时需要。
        /// </remarks>
        public readonly string FileHash;

        /// <summary>
        /// Unity CRC 校验值
        /// </summary>
        public readonly uint UnityCRC;

        /// <summary>
        /// 自定义请求头（可选）
        /// </summary>
        /// <remarks>
        /// 使用 AddRequestHeader 方法添加请求头。
        /// 注意：相同 key 重复添加会抛出异常。
        /// </remarks>
        public Dictionary<string, string> Headers;

        /// <summary>
        /// 构造 AssetBundle 下载请求参数
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="timeout">响应超时时间（秒），0 表示不应用超时</param>
        /// <param name="watchdogTimeout">看门狗超时时间（秒），0 表示禁用</param>
        /// <param name="disableUnityWebCache">是否禁用 Unity 内置缓存</param>
        /// <param name="fileHash">文件哈希（启用缓存时必须提供）</param>
        /// <param name="unityCrc">Unity CRC 校验值</param>
        public DownloadAssetBundleRequestArgs(
            string url,
            int timeout,
            int watchdogTimeout,
            bool disableUnityWebCache = true,
            string fileHash = null,
            uint unityCrc = 0)
        {
            URL = url;
            Timeout = timeout;
            WatchdogTimeout = watchdogTimeout;
            DisableUnityWebCache = disableUnityWebCache;
            FileHash = fileHash;
            UnityCRC = unityCrc;
            Headers = null;
        }

        /// <summary>
        /// 添加请求头数据
        /// </summary>
        public void AddRequestHeader(string name, string value)
        {
            if (Headers == null)
                Headers = new Dictionary<string, string>(10);
            Headers.Add(name, value);
        }
    }

    /// <summary>
    /// 模拟下载请求参数
    /// </summary>
    /// <remarks>
    /// 用于编辑器模式下模拟下载进度，不进行实际网络请求。
    /// </remarks>
    internal struct DownloadSimulateRequestArgs
    {
        /// <summary>
        /// 请求地址（仅用于标识）
        /// </summary>
        public readonly string URL;

        /// <summary>
        /// 模拟的文件大小（字节）
        /// </summary>
        public readonly long FileSize;

        /// <summary>
        /// 模拟的下载速度（字节/秒）
        /// </summary>
        /// <remarks>
        /// 用于计算模拟的下载进度。
        /// </remarks>
        public readonly long DownloadSpeed;

        /// <summary>
        /// 构造模拟下载请求参数
        /// </summary>
        /// <param name="url">请求地址（仅用于标识）</param>
        /// <param name="fileSize">模拟的文件大小（字节）</param>
        /// <param name="downloadSpeed">模拟的下载速度（字节/秒），默认 1MB/s</param>
        public DownloadSimulateRequestArgs(string url, long fileSize, long downloadSpeed = 1024 * 1024)
        {
            URL = url;
            FileSize = fileSize;
            DownloadSpeed = downloadSpeed > 0 ? downloadSpeed : 1024 * 1024;
        }
    }
}
