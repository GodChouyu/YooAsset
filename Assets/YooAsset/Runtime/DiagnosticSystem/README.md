# DiagnosticSystem 诊断模块

## 模块概述

DiagnosticSystem 是 YooAsset 资源管理系统的**运行时诊断模块**，负责收集和传输资源加载的实时状态信息。该模块支持 Editor 与运行时 Player 之间的双向通信，为 AssetBundleDebugger 窗口提供数据支持。

### 可见性说明

DiagnosticSystem 属于 YooAsset Runtime 的内部基础模块，目录内大多数类型为 `internal`（仅供 YooAsset Runtime 内部程序集使用）。业务层建议通过 `YooAssets.GetDebugReport()` 等上层接口获取诊断数据，避免直接依赖本模块的内部类型。

### 核心职责

- 资源加载状态采集
- Provider/Bundle/Operation 诊断信息汇总
- Editor 与 Player 的远程通信
- 诊断数据的序列化与传输

---

## 边界与上层协作

DiagnosticSystem 的职责是提供"诊断数据采集 + 远程通信 + 序列化传输"的基础能力：

- **本模块不负责数据展示**：数据展示由 Editor 端的 AssetBundleDebugger 窗口实现。
- **本模块不负责数据持久化**：诊断数据为实时采集，不进行本地存储。
- **本模块不负责数据分析**：数据分析和统计由上层调试工具实现。

---

## 设计目标

| 目标 | 说明 |
|------|------|
| **实时性** | 支持单次采样和持续采样两种模式 |
| **低侵入** | 仅在需要时采集数据，不影响正常运行性能 |
| **跨平台** | 支持 Editor 模拟和真机远程调试 |
| **可扩展性** | 诊断数据结构支持版本控制 |

---

## 架构概念

### 分层架构

```
┌─────────────────────────────────────────────────────────┐
│              AssetBundleDebugger (Editor)               │
│                    (数据展示层)                          │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│              MockEditorConnection (Editor)              │
│              EditorConnection (Runtime)                 │
│                    (通信层)                              │
└─────────────────────────┬───────────────────────────────┘
                          │ 双向消息
┌─────────────────────────▼───────────────────────────────┐
│              MockPlayerConnection (Editor)              │
│              PlayerConnection (Runtime)                 │
│                    (通信层)                              │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│               RemoteDebugBehaviour                      │
│                  (运行时组件)                            │
│           接收命令、采集数据、发送报告                      │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                  DiagnosticReport                       │
│                    (数据模型)                            │
│      DiagnosticPackageData / ProviderInfo / BundleInfo  │
└─────────────────────────────────────────────────────────┘
```

### 核心组件

- **数据模型层**: 定义诊断数据的结构（DiagnosticReport、DiagnosticPackageData 等）
- **通信层**: 处理 Editor 与 Player 之间的消息传递
- **行为组件层**: 运行时 MonoBehaviour，负责采集和发送诊断数据

---

## 文件结构

```
DiagnosticSystem/
├── DiagnosticBundleInfo.cs          # 资源包诊断信息
├── DiagnosticOperationInfo.cs       # 异步操作诊断信息
├── DiagnosticPackageData.cs         # 包裹诊断数据容器
├── DiagnosticProviderInfo.cs        # 资源加载诊断信息
├── DiagnosticReport.cs              # 诊断报告（顶层数据结构）
│
├── DiagnosticSystemDefine.cs        # 诊断系统常量定义
├── EDebugCommandType.cs             # 调试命令类型枚举
├── RemoteDebugCommand.cs            # 远程调试命令
│
├── RemoteDebugBehaviour.cs          # 运行时调试组件
├── MockEditorConnection.cs          # 模拟 Editor 连接（Editor 用）
└── MockPlayerConnection.cs          # 模拟 Player 连接（Editor 用）
```

---

## 数据模型

### DiagnosticReport（诊断报告）

顶层数据结构，包含所有包裹的诊断数据。

```csharp
[Serializable]
internal class DiagnosticReport
{
    /// <summary>
    /// 调试器版本
    /// </summary>
    public string DebuggerVersion;

    /// <summary>
    /// 报告发生的游戏帧
    /// </summary>
    public int FrameCount;

    /// <summary>
    /// 包裹数据列表
    /// </summary>
    public List<DiagnosticPackageData> PackageDataList;

    // 序列化/反序列化方法
    public static byte[] Serialize(DiagnosticReport report);
    public static DiagnosticReport Deserialize(byte[] data);
}
```

### DiagnosticPackageData（包裹诊断数据）

单个包裹的诊断数据容器。

```csharp
[Serializable]
internal class DiagnosticPackageData
{
    /// <summary>
    /// 包裹名称
    /// </summary>
    public string PackageName;

    /// <summary>
    /// 资源加载的诊断信息列表
    /// </summary>
    public List<DiagnosticProviderInfo> ProviderInfos;

    /// <summary>
    /// 资源包的诊断信息列表
    /// </summary>
    public List<DiagnosticBundleInfo> BundleInfos;

    /// <summary>
    /// 异步操作的诊断信息列表
    /// </summary>
    public List<DiagnosticOperationInfo> OperationInfos;

    // 快速查找方法
    public DiagnosticBundleInfo GetBundleInfo(string bundleName);
}
```

### DiagnosticProviderInfo（资源加载诊断信息）

描述单个资源加载操作的状态。

```csharp
[Serializable]
internal struct DiagnosticProviderInfo
{
    public string AssetPath;        // 资源对象路径
    public string OriginScene;      // 资源加载时所在的场景
    public string StartTime;        // 资源加载开始时间
    public long ElapsedMS;          // 加载耗时（毫秒）
    public int ReferenceCount;      // 引用计数
    public string Status;           // 资源的加载状态
    public List<string> DependentBundles;  // 依赖的资源包列表
}
```

### DiagnosticBundleInfo（资源包诊断信息）

描述单个 AssetBundle 的状态。

```csharp
[Serializable]
internal struct DiagnosticBundleInfo
{
    public string BundleName;       // 资源包名称
    public int ReferenceCount;      // 引用计数
    public string Status;           // 资源包的加载状态
    public List<string> ReferencedByBundles;  // 该资源包被谁引用
}
```

### DiagnosticOperationInfo（异步操作诊断信息）

描述异步操作的执行状态。

```csharp
[Serializable]
internal struct DiagnosticOperationInfo
{
    public string OperationName;    // 异步操作的名称
    public string OperationDesc;    // 异步操作的说明
    public uint Priority;           // 异步操作的优先级
    public string StartTime;        // 开始的时间
    public long ElapsedMS;          // 处理耗时（毫秒）
    public float Progress;          // 异步操作的执行进度
    public string Status;           // 异步操作的执行状态
    public List<DiagnosticOperationInfo> Children;  // 子任务列表
}
```

---

## 通信协议

### 命令类型

```csharp
/// <summary>
/// 远程调试命令类型
/// </summary>
internal enum EDebugCommandType
{
    /// <summary>
    /// 采样一次
    /// </summary>
    SampleOnce = 0,

    /// <summary>
    /// 持续采样
    /// </summary>
    AutoSampling = 1,
}
```

### RemoteDebugCommand（远程调试命令）

Editor 向 Player 发送的调试指令。

```csharp
[Serializable]
internal class RemoteDebugCommand
{
    /// <summary>
    /// 命令类型
    /// </summary>
    public int CommandType;

    /// <summary>
    /// 命令附加参数
    /// </summary>
    public string Parameter;

    // 序列化/反序列化方法
    public static byte[] Serialize(RemoteDebugCommand command);
    public static RemoteDebugCommand Deserialize(byte[] data);
}
```

### 消息标识符

```csharp
internal class DiagnosticSystemDefine
{
    /// <summary>
    /// 调试器版本号
    /// </summary>
    public const string DebuggerVersion = "1.0";

    /// <summary>
    /// Player 向 Editor 发送消息的标识符
    /// </summary>
    public static readonly Guid PlayerToEditorMessageId;

    /// <summary>
    /// Editor 向 Player 发送消息的标识符
    /// </summary>
    public static readonly Guid EditorToPlayerMessageId;
}
```

---

## 核心类说明

### RemoteDebugBehaviour

运行时调试组件，负责接收 Editor 命令并发送诊断数据。

**职责：**
- 监听 Editor 发送的调试命令
- 根据命令采集诊断数据
- 将诊断报告发送回 Editor

**采样模式：**
- `SampleOnce`: 单次采样，采集一帧数据后停止
- `AutoSampling`: 持续采样，每帧自动采集并发送数据

**生命周期：**

```
Awake()     ──► 初始化连接
OnEnable()  ──► 注册消息处理器
LateUpdate()──► 检查采样标志，采集并发送数据
OnDisable() ──► 注销消息处理器
```

### MockEditorConnection / MockPlayerConnection

Editor 模式下的模拟连接，用于本地调试。

**特性：**
- 在 Editor 中模拟 `EditorConnection` 和 `PlayerConnection` 的行为
- 实现消息的本地传递，无需真机连接
- 支持消息注册、注销和发送

```csharp
// 注册消息处理器
MockPlayerConnection.Instance.Register(messageId, callback);

// 发送消息
MockPlayerConnection.Instance.Send(messageId, data);

// 注销消息处理器
MockPlayerConnection.Instance.Unregister(messageId);
```

---

## 通信流程

### Editor 模式（本地调试）

```
┌───────────────────┐         ┌───────────────────┐
│  Debugger Window  │         │ RemoteDebugBehaviour│
│     (Editor)      │         │    (PlayMode)     │
└────────┬──────────┘         └────────┬──────────┘
         │                              │
         │  1. 发送采样命令              │
         │ ─────────────────────────────►
         │  MockEditorConnection.Send() │
         │                              │
         │                              │ 2. 接收命令
         │                              │    HandleEditorMessage()
         │                              │
         │                              │ 3. 采集诊断数据
         │                              │    YooAssets.GetDebugReport()
         │                              │
         │  4. 返回诊断报告              │
         │ ◄─────────────────────────────
         │  MockPlayerConnection.Send() │
         │                              │
         │ 5. 解析并展示数据             │
         ▼                              ▼
```

### 真机模式（远程调试）

```
┌───────────────────┐         ┌───────────────────┐
│  Debugger Window  │         │ RemoteDebugBehaviour│
│     (Editor)      │   USB   │    (Device)       │
└────────┬──────────┘  ════   └────────┬──────────┘
         │                              │
         │  1. 发送采样命令              │
         │ ─────────────────────────────►
         │  EditorConnection.Send()     │
         │                              │
         │                              │ 2. 接收命令
         │                              │    PlayerConnection.Register()
         │                              │
         │                              │ 3. 采集诊断数据
         │                              │
         │  4. 返回诊断报告              │
         │ ◄─────────────────────────────
         │  PlayerConnection.Send()     │
         │                              │
         │ 5. 解析并展示数据             │
         ▼                              ▼
```

---

## 设计模式

### 单例模式

`MockEditorConnection` 和 `MockPlayerConnection` 使用单例模式：

```csharp
public static MockPlayerConnection Instance
{
    get
    {
        if (_instance == null)
            _instance = new MockPlayerConnection();
        return _instance;
    }
}
```

### 观察者模式

通过消息注册机制实现观察者模式：

```
Register(messageId, callback)   ──► 订阅消息
Unregister(messageId)           ──► 取消订阅
HandleXxxMessage(messageId, data) ──► 通知订阅者
```

### 命令模式

`RemoteDebugCommand` 封装调试指令：

```
Editor                          Player
   │                              │
   │  RemoteDebugCommand          │
   │  ┌──────────────────┐        │
   │  │ CommandType: 0   │        │
   │  │ Parameter: ""    │ ──────►│ 执行采样
   │  └──────────────────┘        │
   │                              │
   │  RemoteDebugCommand          │
   │  ┌──────────────────┐        │
   │  │ CommandType: 1   │        │
   │  │ Parameter: "open"│ ──────►│ 开启持续采样
   │  └──────────────────┘        │
```

---

## 类关系图

```
DiagnosticReport (诊断报告)
    │
    └── List<DiagnosticPackageData> (包裹数据)
            │
            ├── List<DiagnosticProviderInfo>  (资源加载信息)
            ├── List<DiagnosticBundleInfo>    (资源包信息)
            └── List<DiagnosticOperationInfo> (异步操作信息)
                    │
                    └── List<DiagnosticOperationInfo> (子任务)

RemoteDebugBehaviour (运行时组件)
    │
    ├── 接收 ──► RemoteDebugCommand
    │
    └── 发送 ──► DiagnosticReport

MockEditorConnection ◄────► MockPlayerConnection (Editor 模拟通信)
EditorConnection     ◄────► PlayerConnection     (真机通信)
```

---

## 注意事项

1. **采样性能**：持续采样模式会每帧采集数据，可能影响性能，建议仅在调试时使用
2. **数据序列化**：使用 Unity 的 `JsonUtility` 进行序列化，受其限制（如不支持 Dictionary）
3. **版本兼容**：`DebuggerVersion` 用于版本控制，Editor 和 Player 版本不匹配时可能无法正常工作
4. **子任务深度**：`DiagnosticOperationInfo.Children` 存在序列化深度限制（10层）
5. **Editor 模式**：在 Editor 模式下使用 Mock 连接进行本地通信，无需真机
6. **真机调试**：真机调试需要通过 USB 连接，使用 Unity 的 `PlayerConnection` API
7. **线程安全**：所有操作应在主线程进行
8. **资源释放**：`MockEditorConnection` 和 `MockPlayerConnection` 在 Domain Reload 时会自动重置
