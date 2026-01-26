using System.Collections.Generic;

namespace YooAsset
{
    internal struct FCClearCacheOptions
    {
        /// <summary>
        /// 清理指定缓存列表
        /// </summary>
        public IReadOnlyList<string> BundleGUIDs { get; set; }
    }
}
