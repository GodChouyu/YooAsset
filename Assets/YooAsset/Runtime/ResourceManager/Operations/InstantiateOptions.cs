using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 游戏对象实例化选项
    /// </summary>
    public struct InstantiateOptions
    {
        /// <summary>
        /// 是否激活实例化对象
        /// </summary>
        public bool Actived { private set; get; }

        /// <summary>
        /// 将指定给新对象的父对象
        /// </summary>
        public Transform Parent { private set; get; }

        /// <summary>
        /// 分配父对象时， 定位新对象关系。
        /// true 在世界空间中定位新对象。
        /// false 相对于父对象来设置新对象。
        /// </summary>
        public bool InWorldSpace { private set; get; }

        /// <summary>
        /// 新对象的位置
        /// </summary>
        public Vector3 Position { private set; get; }

        /// <summary>
        /// 新对象的方向
        /// </summary>
        public Quaternion Rotation { private set; get; }

        internal bool SetPositionAndRotation { private set; get; }

        /// <summary>
        /// 创建实例化选项（仅指定激活状态）
        /// </summary>
        /// <param name="actived">是否激活实例化对象</param>
        public InstantiateOptions(bool actived)
        {
            Actived = actived;
            Parent = null;
            InWorldSpace = false;

            SetPositionAndRotation = false;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
        }

        /// <summary>
        /// 创建实例化选项（指定父对象）
        /// </summary>
        /// <param name="actived">是否激活实例化对象</param>
        /// <param name="parent">父对象</param>
        /// <param name="inWorldSpace">是否在世界空间中定位</param>
        public InstantiateOptions(bool actived, Transform parent, bool inWorldSpace)
        {
            Actived = actived;
            Parent = parent;
            InWorldSpace = inWorldSpace;

            SetPositionAndRotation = false;
            Position = Vector3.zero;
            Rotation = Quaternion.identity;
        }

        /// <summary>
        /// 创建实例化选项（指定父对象和位置旋转）
        /// </summary>
        /// <param name="actived">是否激活实例化对象</param>
        /// <param name="parent">父对象</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        public InstantiateOptions(bool actived, Transform parent, Vector3 position, Quaternion rotation)
        {
            Actived = actived;
            Parent = parent;
            InWorldSpace = false;

            SetPositionAndRotation = true;
            Position = position;
            Rotation = rotation;
        }

        /// <summary>
        /// 创建实例化选项（指定位置旋转）
        /// </summary>
        /// <param name="actived">是否激活实例化对象</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        public InstantiateOptions(bool actived, Vector3 position, Quaternion rotation)
        {
            Actived = actived;
            Parent = null;
            InWorldSpace = false;

            SetPositionAndRotation = true;
            Position = position;
            Rotation = rotation;
        }
    }
}