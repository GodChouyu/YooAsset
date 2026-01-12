# OperationSystem 异步操作模块

## 模块概述

OperationSystem 是 YooAsset 资源管理系统的**异步操作调度层**，负责管理和调度所有异步操作的生命周期。该模块提供了统一的异步操作抽象，支持协程、Task（async/await）、回调等多种异步编程模式，以及基于优先级的时间切片调度机制。

### 可见性说明

OperationSystem 属于 YooAsset Runtime 的内部基础模块，目录内 `OperationSystem`、`OperationScheduler` 等类型为 `internal`（仅供 YooAsset Runtime 内部程序集使用）。`AsyncOperationBase` 和 `EOperationStatus` 为 `public`，供上层和用户代码使用。

### 核心职责

- 异步操作生命周期管理（启动、更新、完成、中止）
- 基于优先级的调度排序
- 时间切片执行（防止主线程阻塞）
- 多包裹独立调度
- 父子任务依赖管理

---

## 边界与上层协作

OperationSystem 的职责是提供"统一异步操作抽象 + 优先级调度 + 时间切片执行"的基础能力：

- **本模块不负责具体业务逻辑**：具体的资源加载、下载等逻辑由子类实现
- **本模块不负责错误重试**：失败后的重试策略由上层或子类实现
- **本模块不负责资源释放**：资源的引用计数和卸载由 ResourceManager 管理

---

## 设计目标

| 目标 | 说明 |
|------|------|
| **统一抽象** | 所有异步操作继承 AsyncOperationBase，提供一致的 API |
| **多模式支持** | 支持协程（yield return）、Task（async/await）、回调三种异步模式 |
| **时间切片** | 可配置每帧最大执行时间，防止主线程卡顿 |
| **优先级调度** | 支持任务和调度器双层优先级，高优先级任务优先执行 |
| **多包裹隔离** | 每个资源包裹拥有独立调度器，互不干扰 |

---

## 架构概念

### 分层架构

```
┌─────────────────────────────────────────────────────────┐
│                    上层调用者                             │
│         (ResourcePackage / ResourceManager)             │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                  OperationSystem                        │
│                  (静态调度系统)                           │
│         管理所有调度器，提供时间切片机制                     │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                 OperationScheduler                      │
│                   (包裹调度器)                            │
│            管理单个包裹的异步操作队列                       │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                 AsyncOperationBase                      │
│                   (异步操作基类)                          │
│              定义操作生命周期和状态机                       │
└─────────────────────────────────────────────────────────┘
```

### 核心组件

- **OperationSystem**: 静态调度系统，管理所有包裹调度器，提供时间切片执行机制
- **OperationScheduler**: 包裹调度器，管理单个包裹的异步操作队列和优先级排序
- **AsyncOperationBase**: 异步操作抽象基类，定义操作生命周期和多种异步编程接口
- **EOperationStatus**: 操作状态枚举，表示操作的当前状态

---

## 文件结构

```
OperationSystem/
├── AsyncOperationBase.cs       # 异步操作抽象基类
├── EOperationStatus.cs         # 操作状态枚举
├── OperationScheduler.cs       # 包裹调度器
├── OperationSystem.cs          # 静态调度系统
└── README.md                   # 本文档
```

---

## 接口说明

### EOperationStatus（操作状态枚举）

定义异步操作的生命周期状态。

```csharp
public enum EOperationStatus
{
    None,       // 未开始
    Processing, // 处理中
    Succeed,    // 已成功
    Failed,     // 已失败
    Aborted     // 已中止（用户主动取消）
}
```

### AsyncOperationBase（异步操作基类）

所有异步操作的抽象基类，实现 `IEnumerator`（协程支持）和 `IComparable<T>`（优先级排序）。

```csharp
public abstract class AsyncOperationBase : IEnumerator, IComparable<AsyncOperationBase>
{
    // 状态属性
    public EOperationStatus Status { get; }     // 当前状态
    public bool IsDone { get; }                  // 是否完成（Succeed/Failed/Aborted）
    public string Error { get; }                 // 错误信息
    public float Progress { get; }               // 进度（0-1）
    public uint Priority { get; set; }           // 优先级（值越大越优先）

    // 异步编程支持
    public Task Task { get; }                    // 用于 async/await
    public event Action<AsyncOperationBase> Completed; // 完成回调

    // 同步等待
    public void WaitForAsyncComplete();          // 同步等待完成（阻塞当前线程）

    // 调试信息（仅 DEBUG 模式有效）
    public string StartTime { get; }             // 开始时间（HH:MM:SS）
    public long ElapsedMS { get; }               // 耗时（毫秒）
}
```

### 子类必须实现的方法

```csharp
// 启动时调用（必须实现）
internal abstract void InternalStart();

// 每帧更新（必须实现）
internal abstract void InternalUpdate();

// 中止时调用（可选实现）
internal virtual void InternalAbort() { }

// 同步等待实现（可选实现，不实现则抛出异常）
internal virtual void InternalWaitForAsyncComplete();

// 获取操作描述（可选实现，用于调试）
internal virtual string InternalGetDescription();
```

---

## 核心类说明

### OperationSystem

静态调度系统，管理所有包裹的调度器，提供全局时间切片机制。

**主要功能：**
- 管理多个包裹调度器的生命周期
- 提供全局时间切片预算控制
- 按优先级更新各调度器

```csharp
internal static class OperationSystem
{
    // 时间切片配置
    public static long MaxTimeSlice { get; set; }  // 每帧最大执行时间（毫秒）
    public static bool IsBusy { get; }             // 当前帧时间切片是否已用完

    // 生命周期
    public static void Initialize();               // 初始化系统
    public static void Update();                   // 每帧更新
    public static void DestroyAll();               // 销毁系统

    // 调度器管理
    public static OperationScheduler CreatePackageScheduler(string packageName, uint priority);
    public static void DestroyPackageScheduler(string packageName);
    public static void ClearPackageOperations(string packageName);

    // 任务管理
    public static void StartOperation(string packageName, AsyncOperationBase operation);
}
```

### OperationScheduler

包裹调度器，管理单个包裹的异步操作队列。

**主要功能：**
- 管理操作队列（待处理队列 + 执行队列）
- 按优先级排序操作
- 更新和完成操作

```csharp
internal class OperationScheduler : IComparable<OperationScheduler>
{
    public string PackageName { get; }    // 所属包裹名称
    public uint Priority { get; set; }    // 调度器优先级
    public int CreationOrder { get; }     // 创建顺序（同优先级稳定排序）

    public void StartOperation(AsyncOperationBase operation);  // 启动操作
    public void Update();                                       // 更新调度
    public void ClearAll();                                     // 清空并中止所有任务
}
```

---

## 使用示例

### 协程方式

```csharp
IEnumerator LoadAssetCoroutine()
{
    var operation = package.LoadAssetAsync<GameObject>("Assets/Prefab.prefab");
    yield return operation;

    if (operation.Status == EOperationStatus.Succeed)
    {
        GameObject prefab = operation.AssetObject as GameObject;
        Instantiate(prefab);
    }
    else
    {
        Debug.LogError($"加载失败: {operation.Error}");
    }
}
```

### async/await 方式

```csharp
async void LoadAssetAsync()
{
    var operation = package.LoadAssetAsync<GameObject>("Assets/Prefab.prefab");
    await operation.Task;

    if (operation.Status == EOperationStatus.Succeed)
    {
        GameObject prefab = operation.AssetObject as GameObject;
        Instantiate(prefab);
    }
    else
    {
        Debug.LogError($"加载失败: {operation.Error}");
    }
}
```

### 回调方式

```csharp
void LoadAssetWithCallback()
{
    var operation = package.LoadAssetAsync<GameObject>("Assets/Prefab.prefab");
    operation.Completed += OnLoadCompleted;
}

void OnLoadCompleted(AsyncOperationBase op)
{
    var operation = op as AssetHandle;
    if (operation.Status == EOperationStatus.Succeed)
    {
        GameObject prefab = operation.AssetObject as GameObject;
        Instantiate(prefab);
    }
    else
    {
        Debug.LogError($"加载失败: {operation.Error}");
    }
}
```

### 同步等待方式

```csharp
void LoadAssetSync()
{
    var operation = package.LoadAssetAsync<GameObject>("Assets/Prefab.prefab");
    operation.WaitForAsyncComplete(); // 注意：会阻塞当前线程

    if (operation.Status == EOperationStatus.Succeed)
    {
        GameObject prefab = operation.AssetObject as GameObject;
        Instantiate(prefab);
    }
}
```

### 设置优先级

```csharp
// 高优先级任务优先执行
var highPriorityOp = package.LoadAssetAsync<GameObject>("ImportantAsset");
highPriorityOp.Priority = 100;

var lowPriorityOp = package.LoadAssetAsync<GameObject>("BackgroundAsset");
lowPriorityOp.Priority = 1;
```

### 配置时间切片

```csharp
// 设置每帧最大执行时间为 10 毫秒
OperationSystem.MaxTimeSlice = 10;
```

---

## 设计模式

### 模板方法模式

`AsyncOperationBase` 定义操作的骨架流程，子类实现具体步骤：

```
AsyncOperationBase (抽象类)
    │
    ├── StartOperation()   ──► InternalStart()    [子类实现]
    ├── UpdateOperation()  ──► InternalUpdate()   [子类实现]
    ├── AbortOperation()   ──► InternalAbort()    [子类可选实现]
    └── WaitForAsyncComplete() ──► InternalWaitForAsyncComplete() [子类可选实现]
```

### 状态机模式

操作生命周期通过状态机管理：

```
┌──────┐   StartOperation()   ┌────────────┐
│ None │ ────────────────────► │ Processing │
└──────┘                      └─────┬──────┘
                                    │ UpdateOperation()
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
              ┌─────────┐     ┌──────────┐    ┌─────────┐
              │ Succeed │     │  Failed  │    │ Aborted │
              └─────────┘     └──────────┘    └─────────┘
```

### 组合模式

支持父子任务关系，父任务可以管理多个子任务：

```
ParentOperation
    │
    ├── ChildOperation1
    ├── ChildOperation2
    └── ChildOperation3
```

### 时间切片模式

防止主线程阻塞，每帧限制执行时间：

```
每帧 Update()
    │
    ├── 记录帧开始时间
    │
    ├── 遍历调度器（按优先级）
    │       │
    │       ├── 遍历操作（按优先级）
    │       │       │
    │       │       └── 检查 IsBusy ──► 超时则跳出
    │       │
    │       └── 检查 IsBusy ──► 超时则跳出
    │
    └── 结束
```

---

## 类继承关系

```
AsyncOperationBase (抽象基类)
    │
    ├── InitializePackageOperation      (包裹初始化)
    ├── LoadManifestOperation           (清单加载)
    ├── DownloaderOperation             (下载器操作)
    ├── UnloadAllAssetsOperation        (卸载所有资源)
    ├── DestroyPackageOperation         (销毁包裹)
    │
    ├── ProviderOperation               (资源提供者基类)
    │       ├── AssetProvider           (单个资源)
    │       ├── SubAssetsProvider       (子资源)
    │       ├── AllAssetsProvider       (全部资源)
    │       └── SceneProvider           (场景)
    │
    └── ... (更多子类)

OperationScheduler
    └── 管理 List<AsyncOperationBase>

OperationSystem
    └── 管理 Dictionary<string, OperationScheduler>
```

---

## 调试信息

### 调试属性

在 DEBUG 模式下，`AsyncOperationBase` 提供以下调试信息：

| 属性 | 类型 | 说明 |
|------|------|------|
| `StartTime` | `string` | 操作开始时间（格式：HH:MM:SS） |
| `ElapsedMS` | `long` | 操作耗时（毫秒） |

### DiagnosticOperationInfo

通过 `GetDebugOperationInfo()` 获取操作的诊断信息结构体，包含：

- 操作名称、描述
- 优先级、进度、状态
- 开始时间、耗时
- 子操作列表（递归）

---

## 注意事项

1. **主线程调用**：所有操作的创建和更新应在 Unity 主线程进行
2. **同步等待风险**：`WaitForAsyncComplete()` 会阻塞当前线程，可能导致死锁，谨慎使用
3. **优先级变更**：修改 `Priority` 后，下一帧才会重新排序
4. **时间切片粒度**：时间切片检测在每个操作更新后进行，单个操作内部不会被中断
5. **子任务中止**：调用 `AbortOperation()` 会递归中止所有子任务
6. **回调异常隔离**：完成回调中的异常会被捕获并记录，不会影响其他回调
7. **状态不可逆**：一旦进入终态（Succeed/Failed/Aborted），状态不可更改
8. **Progress 语义**：`Progress` 在操作完成时自动设置为 1.0f，子类应在处理过程中更新进度
