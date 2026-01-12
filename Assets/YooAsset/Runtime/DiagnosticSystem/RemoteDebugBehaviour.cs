using System;
using UnityEngine;
using UnityEngine.Networking.PlayerConnection;

namespace YooAsset
{
    /// <summary>
    /// 远程调试行为组件
    /// 负责接收 Editor 命令并发送诊断数据
    /// </summary>
    internal class RemoteDebugBehaviour : MonoBehaviour
    {
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize()
        {
            _sampleOnce = false;
            _autoSampling = false;
        }
#endif

        private static bool _sampleOnce = false;
        private static bool _autoSampling = false;

        private void Awake()
        {
#if UNITY_EDITOR
            MockPlayerConnection.Instance.Initialize();
#endif
        }
        private void OnEnable()
        {
#if UNITY_EDITOR
            MockPlayerConnection.Instance.Register(DiagnosticSystemDefine.EditorToPlayerMessageId, HandleEditorMessage);
#else
            PlayerConnection.instance.Register(DiagnosticSystemDefine.EditorToPlayerMessageId, HandleEditorMessage);
#endif
        }
        private void OnDisable()
        {
#if UNITY_EDITOR
            MockPlayerConnection.Instance.Unregister(DiagnosticSystemDefine.EditorToPlayerMessageId);
#else
            PlayerConnection.instance.Unregister(DiagnosticSystemDefine.EditorToPlayerMessageId, HandleEditorMessage);
#endif
        }
        private void LateUpdate()
        {
            if (_autoSampling || _sampleOnce)
            {
                _sampleOnce = false;
                var debugReport = YooAssets.GetDebugReport();
                var data = DiagnosticReport.Serialize(debugReport);

#if UNITY_EDITOR
                MockPlayerConnection.Instance.Send(DiagnosticSystemDefine.PlayerToEditorMessageId, data);
#else
                PlayerConnection.instance.Send(DiagnosticSystemDefine.PlayerToEditorMessageId, data);
#endif
            }
        }

        private static void HandleEditorMessage(MessageEventArgs args)
        {
            var command = RemoteDebugCommand.Deserialize(args.data);
            YooLogger.Log($"Handle remote command: {command.CommandType} Param: {command.Parameter}");
            if (command.CommandType == (int)EDebugCommandType.SampleOnce)
            {
                _sampleOnce = true;
            }
            else if (command.CommandType == (int)EDebugCommandType.AutoSampling)
            {
                if (command.Parameter == "open")
                    _autoSampling = true;
                else
                    _autoSampling = false;
            }
            else
            {
                throw new NotImplementedException(command.CommandType.ToString());
            }
        }
    }
}