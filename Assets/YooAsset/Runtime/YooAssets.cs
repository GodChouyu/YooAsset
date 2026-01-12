using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YooAsset
{
    public static partial class YooAssets
    {
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize()
        {
            _isInitialized = false;
            _packages.Clear();
        }
#endif

        private static bool _isInitialized;
        private static GameObject _driver;
        private static readonly Dictionary<string, ResourcePackage> _packages = new Dictionary<string, ResourcePackage>(10);

        /// <summary>
        /// 是否已经初始化
        /// </summary>
        public static bool Initialized
        {
            get { return _isInitialized; }
        }

        /// <summary>
        /// 初始化资源系统
        /// </summary>
        /// <param name="logger">自定义日志处理</param>
        public static void Initialize(ILogger logger = null)
        {
            if (_isInitialized)
            {
                YooLogger.Warning("YooAssets is already initialized.");
                return;
            }

            YooLogger.Logger = logger;

            // 创建驱动器
            _isInitialized = true;
            _driver = new UnityEngine.GameObject($"[{nameof(YooAssets)}]");
            _driver.AddComponent<YooAssetsDriver>();
            UnityEngine.Object.DontDestroyOnLoad(_driver);

#if DEBUG
            // 添加远程调试脚本
            _driver.AddComponent<RemoteDebugBehaviour>();
#endif

            // 初始化异步操作系统
            OperationSystem.Initialize();
        }

        /// <summary>
        /// 销毁资源系统
        /// </summary>
        public static void Destroy()
        {
            if (_isInitialized)
            {
                _isInitialized = false;

                if (_driver != null)
                    GameObject.Destroy(_driver);

                // 销毁异步操作系统
                OperationSystem.DestroyAll();

                // 卸载所有AssetBundle
                AssetBundle.UnloadAllAssetBundles(true);

                // 清空资源包裹列表
                _packages.Clear();
            }
        }

        /// <summary>
        /// 更新资源系统
        /// </summary>
        internal static void Update()
        {
            if (_isInitialized)
            {
                OperationSystem.Update();
            }
        }

        /// <summary>
        /// 创建资源包裹
        /// </summary>
        /// <param name="packageName">包裹名称</param>
        /// <param name="packagePriority">包裹优先级（值越大越优先更新）</param>
        public static ResourcePackage CreatePackage(string packageName, uint packagePriority = 0)
        {
            EnsureInitialized(packageName);
            if (ContainsPackage(packageName))
                throw new YooPackageException(packageName, $"Resource package {packageName} already existed. Cannot create duplicate packages.");

            ResourcePackage package = new ResourcePackage(packageName);
            _packages.Add(packageName, package);

            // 注册包裹调度器
            OperationSystem.CreatePackageScheduler(packageName, packagePriority);

            return package;
        }

        /// <summary>
        /// 获取资源包裹
        /// </summary>
        /// <param name="packageName">包裹名称</param>
        public static ResourcePackage GetPackage(string packageName)
        {
            EnsureInitialized(packageName);
            var package = GetPackageInternal(packageName);
            if (package == null)
                YooLogger.Error($"Can not found resource package : {packageName}");
            return package;
        }

        /// <summary>
        /// 尝试获取资源包裹
        /// </summary>
        /// <param name="packageName">包裹名称</param>
        public static ResourcePackage TryGetPackage(string packageName)
        {
            EnsureInitialized(packageName);
            return GetPackageInternal(packageName);
        }

        /// <summary>
        /// 获取所有资源包裹
        /// </summary>
        public static IReadOnlyList<ResourcePackage> GetAllPackages()
        {
            return _packages.Values.ToList();
        }

        /// <summary>
        /// 移除资源包裹
        /// </summary>
        /// <param name="packageName">包裹名称</param>
        public static bool RemovePackage(string packageName)
        {
            EnsureInitialized(packageName);
            ResourcePackage package = GetPackageInternal(packageName);
            if (package == null)
            {
                YooLogger.Error($"Can not found resource package : {packageName}");
                return false;
            }

            if (package.InitializeStatus != EOperationStatus.None)
            {
                YooLogger.Error($"The resource package {packageName} has not been destroyed, please call the method {nameof(ResourcePackage.DestroyPackageAsync)} to destroy.");
                return false;
            }

            // 先销毁调度器，再移除包裹
            OperationSystem.DestroyPackageScheduler(packageName);

            return _packages.Remove(packageName);
        }

        /// <summary>
        /// 检测资源包裹是否存在
        /// </summary>
        /// <param name="packageName">包裹名称</param>
        public static bool ContainsPackage(string packageName)
        {
            EnsureInitialized(packageName);
            var package = GetPackageInternal(packageName);
            return package != null;
        }

        private static ResourcePackage GetPackageInternal(string packageName)
        {
            _packages.TryGetValue(packageName, out var package);
            return package;
        }
        private static void EnsureInitialized(string packageName)
        {
            if (_isInitialized == false)
                throw new YooInitializeException($"YooAssets not initialized. Please call {nameof(YooAssets.Initialize)} first.");

            if (string.IsNullOrEmpty(packageName))
                throw new YooInitializeException("Package name cannot be null or empty.");
        }

        #region 系统参数
        /// <summary>
        /// 设置异步系统参数，每帧执行消耗的最大时间切片（单位：毫秒）
        /// </summary>
        public static void SetOperationSystemMaxTimeSlice(long milliseconds)
        {
            OperationSystem.MaxTimeSlice = milliseconds;
        }
        #endregion

        #region 调试信息
        internal static DiagnosticReport GetDebugReport()
        {
            DiagnosticReport report = new DiagnosticReport();
            report.FrameCount = Time.frameCount;

            foreach (var kv in _packages)
            {
                var packageData = kv.Value.GetDebugPackageData();
                report.PackageDataList.Add(packageData);
            }
            return report;
        }
        #endregion
    }
}