using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking.PlayerConnection;
using UnityEngine;

namespace YooAsset
{
    /// <summary>
    /// 模拟的 Player 连接
    /// 在 Editor 模式下模拟 PlayerConnection 的行为，用于本地调试
    /// </summary>
    internal class MockPlayerConnection
    {
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize()
        {
            _instance = null;
        }
#endif

        private static MockPlayerConnection _instance;

        /// <summary>
        /// 获取单例实例
        /// </summary>
        public static MockPlayerConnection Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new MockPlayerConnection();
                return _instance;
            }
        }

        private readonly Dictionary<Guid, UnityAction<MessageEventArgs>> _messageHandlers = new Dictionary<Guid, UnityAction<MessageEventArgs>>();

        /// <summary>
        /// 初始化连接，清空所有已注册的消息处理器
        /// </summary>
        public void Initialize()
        {
            _messageHandlers.Clear();
        }

        /// <summary>
        /// 注册消息处理回调
        /// </summary>
        /// <param name="messageID">消息标识符</param>
        /// <param name="callback">收到消息时的回调函数</param>
        public void Register(Guid messageID, UnityAction<MessageEventArgs> callback)
        {
            if (messageID == Guid.Empty)
                throw new ArgumentException("messageID is empty.");

            if (_messageHandlers.ContainsKey(messageID) == false)
                _messageHandlers.Add(messageID, callback);
        }

        /// <summary>
        /// 注销消息处理回调
        /// </summary>
        /// <param name="messageID">消息标识符</param>
        public void Unregister(Guid messageID)
        {
            if (_messageHandlers.ContainsKey(messageID))
                _messageHandlers.Remove(messageID);
        }

        /// <summary>
        /// 向 Editor 端发送消息
        /// </summary>
        /// <param name="messageID">消息标识符</param>
        /// <param name="data">消息数据</param>
        public void Send(Guid messageID, byte[] data)
        {
            if (messageID == Guid.Empty)
                throw new ArgumentException("messageID is empty.");

            // 接收对方的消息
            MockEditorConnection.Instance.HandlePlayerMessage(messageID, data);
        }

        /// <summary>
        /// 处理来自 Editor 端的消息
        /// </summary>
        /// <param name="messageID">消息标识符</param>
        /// <param name="data">消息数据</param>
        internal void HandleEditorMessage(Guid messageID, byte[] data)
        {
            if (_messageHandlers.TryGetValue(messageID, out UnityAction<MessageEventArgs> value))
            {
                var args = new MessageEventArgs();
                args.playerId = 0;
                args.data = data;
                value?.Invoke(args);
            }
        }
    }
}