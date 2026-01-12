using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 初始化参数
    /// </summary>
    public abstract class InitializePackageOptions
    {
        /// <summary>
        /// 同时加载Bundle文件的最大并发数
        /// </summary>
        public int BundleLoadingMaxConcurrency = int.MaxValue;

        /// <summary>
        /// 当资源引用计数为零的时候自动释放资源包
        /// </summary>
        public bool AutoUnloadBundleWhenUnused = false;

        /// <summary>
        /// WebGL平台强制同步加载资源对象
        /// </summary>
        public bool WebGLForceSyncLoadAsset = false;
    }

    /// <summary>
    /// 编辑器下模拟运行模式的初始化参数
    /// </summary>
    public class EditorSimulateModeOptions : InitializePackageOptions
    {
        public FileSystemParameters EditorFileSystemParameters;
    }

    /// <summary>
    /// 离线运行模式的初始化参数
    /// </summary>
    public class OfflinePlayModeOptions : InitializePackageOptions
    {
        public FileSystemParameters BuildinFileSystemParameters;
    }

    /// <summary>
    /// 联机运行模式的初始化参数
    /// </summary>
    public class HostPlayModeOptions : InitializePackageOptions
    {
        public FileSystemParameters BuildinFileSystemParameters;
        public FileSystemParameters CacheFileSystemParameters;
    }

    /// <summary>
    /// WebGL运行模式的初始化参数
    /// </summary>
    public class WebPlayModeOptions : InitializePackageOptions
    {
        public FileSystemParameters WebServerFileSystemParameters;
        public FileSystemParameters WebRemoteFileSystemParameters;
    }

    /// <summary>
    /// 自定义运行模式的初始化参数
    /// </summary>
    public class CustomPlayModeOptions : InitializePackageOptions
    {
        /// <summary>
        /// 文件系统初始化参数列表
        /// 注意：列表最后一个元素作为主文件系统！
        /// </summary>
        public readonly List<FileSystemParameters> FileSystemParameterList = new List<FileSystemParameters>();
    }
}