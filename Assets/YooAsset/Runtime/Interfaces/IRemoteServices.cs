using System.Collections.Generic;

namespace YooAsset
{
    public interface IRemoteServices
    {
        /// <summary>
        /// 获取指定文件的所有远端候选地址，按优先级排序。
        /// 列表至少包含一个 URL。
        /// </summary>
        /// <param name="fileName">请求的文件名称</param>
        IReadOnlyList<string> GetRemoteURLs(string fileName);
    }
}
