# DownloadSystem 下载模块

## 模块概述

DownloadSystem 是 YooAsset 资源管理系统的**底层网络下载层**，负责处理所有 HTTP 网络请求。该模块提供了统一的下载接口抽象，支持文件下载、断点续传、并发请求（由上层调度）、看门狗监控等功能。

### 可见性说明

DownloadSystem 属于 YooAsset Runtime 的内部基础模块，目录内大多数类型为 `internal`（仅供 YooAsset Runtime 内部程序集使用）。本文示例以"模块内部调用方式"展示；业务层建议优先通过 `ResourcePackage / FileSystem / ResourceManager` 等上层接口使用下载能力，避免直接依赖本模块的内部类型。

### 核心职责

- HTTP/HTTPS 文件下载
- 断点续传支持
- 看门狗超时保护
- 多种下载类型（文件/字节/文本/AssetBundle）
- 可插拔的网络库后端

---

## 边界与上层协作

DownloadSystem 的职责是提供"可替换后端 + 统一请求接口 + 轮询式生命周期"的基础能力：

- **本模块不负责并发队列/限流调度**：并发通常由上层同时创建多个 request 并自行控制并发数。
- **本模块不负责重试/回退策略**：失败后的重试、切换 CDN、降级等策略通常由上层系统实现。
- **本模块不负责持久化下载任务**：断点续传依赖本地已有文件与 `Range` 请求头，并由上层管理断点信息。

---

## 设计目标

| 目标 | 说明 |
|------|------|
| **可扩展性** | 支持可插拔的网络库后端（UnityWebRequest/BestHTTP/自研） |
| **鲁棒性** | 看门狗超时保护、自动清理失败文件、完整错误信息 |
| **高性能** | 轮询模式无阻塞、支持并发请求（并发数由上层调度） |
| **易用性** | 流畅的参数构建 API、清晰的状态转换 |

---

## 架构概念

### 分层架构

```
┌─────────────────────────────────────────────────────────┐
│                    上层调用者                             │
│              (FileSystem / ResourceManager)             │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                  IDownloadBackend                       │
│                    (后端接口)                            │
│         定义网络库合约，工厂模式创建请求                     │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│                  IDownloadRequest                       │
│                    (请求接口)                            │
│           轮询式生命周期管理，状态机驱动                     │
└─────────────────────────┬───────────────────────────────┘
                          │
┌─────────────────────────▼───────────────────────────────┐
│              UnityWebRequest / 其他网络库                 │
│                    (底层实现)                            │
└─────────────────────────────────────────────────────────┘
```

### 核心组件

- **后端层 (IDownloadBackend)**: 定义网络库实现合约，通过工厂方法创建各类请求
- **请求层 (IDownloadRequest)**: 统一的请求生命周期管理，支持轮询驱动
- **参数层 (Args 结构体)**: 配置下载行为（超时、断点续传、看门狗等）

---

## 文件结构

```
DownloadSystem/
├── Interface/                                # 接口定义
│   ├── IDownloadBackend.cs                   # 后端接口（工厂模式）
│   └── IDownloadRequest.cs                   # 请求接口层次结构
│
├── UnityWebBackend/                          # 默认后端实现
│   ├── UnityWebRequestBackend.cs             # UnityWebRequest 后端
│   └── UnityWebRequestCreator.cs             # UnityWebRequest 创建委托
│
├── UnityWebRequest/                          # 默认请求实现
│   ├── UnityWebRequestBase.cs                # 基础下载器（抽象类）
│   ├── UnityWebRequestFile.cs                # 文件下载器
│   ├── UnityWebRequestHead.cs                # HEAD 请求器
│   ├── UnityWebRequestBytes.cs               # 字节下载器
│   ├── UnityWebRequestText.cs                # 文本下载器
│   ├── UnityWebRequestAssetBundle.cs         # AssetBundle 下载器
│   └── SimulateRequestFile.cs                # 模拟下载器（编辑器用）
│
├── EDownloadRequestStatus.cs                 # 下载请求状态枚举
├── DownloadRequestArgs.cs                    # 请求参数结构体定义
├── DownloadSystemTools.cs                    # 工具函数
└── DownloadFailureCounter.cs                 # 请求失败计数器
```

---

## 接口说明

### IDownloadBackend（后端接口）

定义网络库实现的合约，通过工厂方法创建各类下载请求。

```csharp
internal interface IDownloadBackend : IDisposable
{
    /// <summary>
    /// 后端标识名称（用于日志/调试）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 定期驱动更新（部分第三方库需要）
    /// </summary>
    void Update();

    // 工厂方法 - 创建各类请求
    IDownloadHeadRequest CreateHeadRequest(DownloadDataRequestArgs args);
    IDownloadFileRequest CreateFileRequest(DownloadFileRequestArgs args);
    IDownloadBytesRequest CreateBytesRequest(DownloadDataRequestArgs args);
    IDownloadTextRequest CreateTextRequest(DownloadDataRequestArgs args);
    IDownloadAssetBundleRequest CreateAssetBundleRequest(DownloadAssetBundleRequestArgs args);
    IDownloadFileRequest CreateSimulateRequest(DownloadSimulateRequestArgs args);
}
```

### IDownloadRequest（基础请求接口）

所有下载请求的通用接口，定义生命周期和状态管理。

```csharp
internal interface IDownloadRequest : IDisposable
{
    // 元信息
    string URL { get; }

    // 生命周期
    bool IsDone { get; }                      // 访问时自动调用 PollingRequest()
    EDownloadRequestStatus Status { get; }

    // 进度跟踪
    float DownloadProgress { get; }           // 0f - 1f
    long DownloadedBytes { get; }             // 本次请求新增字节数

    // 诊断信息
    long HttpCode { get; }
    string Error { get; }

    // 生命周期方法
    void SendRequest();                       // 发起请求
    void PollingRequest();                    // 轮询状态
    void AbortRequest();                      // 中止请求
}
```

### 专化请求接口

| 接口 | 用途 | 特有能力 |
|------|------|----------|
| `IDownloadHeadRequest` | HEAD 请求，获取响应头 | `ETag`, `LastModified`, `ContentLength`, `ContentType`, `GetResponseHeader(name)` |
| `IDownloadFileRequest` | 文件下载到本地 | `SavePath` |
| `IDownloadBytesRequest` | 下载到内存（字节数组） | `byte[] Result` |
| `IDownloadTextRequest` | 下载文本内容 | `string Result` |
| `IDownloadAssetBundleRequest` | 下载并加载 AssetBundle | `AssetBundle Result` |

---

## 结构体定义

### 请求状态枚举

```csharp
internal enum EDownloadRequestStatus
{
    None,       // 未开始
    Running,    // 进行中
    Succeed,    // 已成功
    Failed,     // 已失败
    Aborted     // 已中止（用户中止或看门狗超时）
}
```

### 请求参数结构体

#### DownloadFileRequestArgs（文件下载参数）

```csharp
internal struct DownloadFileRequestArgs
{
    public readonly string URL;                 // 请求地址
    public readonly int Timeout;                // 响应超时（秒），0=不应用超时
    public readonly int WatchdogTimeout;        // 看门狗超时（秒），0=禁用

    public readonly string SavePath;            // 文件保存路径
    public readonly bool AppendToFile;          // 追加写入（断点续传）
    public readonly bool RemoveFileOnAbort;     // 中止时删除文件
    public readonly long ResumeOffset;          // 断点续传起始位置（>0 时推荐由后端设置 Range 头）

    public Dictionary<string, string> Headers;  // 自定义请求头（可选）

    public DownloadFileRequestArgs(
        string url,
        string savePath,
        int timeout,
        int watchdogTimeout,
        bool appendToFile = false,
        bool removeFileOnAbort = true,
        long resumeOffset = 0);

    /// <summary>
    /// 添加请求头（注意：相同 key 重复添加会抛异常）
    /// </summary>
    public void AddRequestHeader(string name, string value);
}
```

#### DownloadDataRequestArgs（数据下载参数）

```csharp
internal struct DownloadDataRequestArgs
{
    public readonly string URL;                 // 请求地址
    public readonly int Timeout;                // 响应超时（秒），0=不应用超时
    public readonly int WatchdogTimeout;        // 看门狗超时（秒），0=禁用
    public Dictionary<string, string> Headers;  // 自定义请求头（可选）

    public DownloadDataRequestArgs(string url, int timeout, int watchdogTimeout);

    /// <summary>
    /// 添加请求头（注意：相同 key 重复添加会抛异常）
    /// </summary>
    public void AddRequestHeader(string name, string value);
}
```

#### DownloadAssetBundleRequestArgs（AssetBundle 下载参数）

```csharp
internal struct DownloadAssetBundleRequestArgs
{
    public readonly string URL;                 // 请求地址
    public readonly int Timeout;                // 响应超时（秒），0=不应用超时
    public readonly int WatchdogTimeout;        // 看门狗超时（秒），0=禁用

    public readonly bool DisableUnityWebCache;  // 禁用 Unity 缓存（默认 true）
    public readonly string FileHash;            // 文件哈希（缓存启用时需要，且不能为空）
    public readonly uint UnityCRC;              // Unity CRC 校验值

    public Dictionary<string, string> Headers;  // 自定义请求头（可选）

    public DownloadAssetBundleRequestArgs(
        string url,
        int timeout,
        int watchdogTimeout,
        bool disableUnityWebCache = true,
        string fileHash = null,
        uint unityCrc = 0);

    /// <summary>
    /// 添加请求头（注意：相同 key 重复添加会抛异常）
    /// </summary>
    public void AddRequestHeader(string name, string value);
}
```

#### DownloadSimulateRequestArgs（模拟下载参数）

```csharp
internal struct DownloadSimulateRequestArgs
{
    public readonly string URL;          // 标识符
    public readonly long FileSize;       // 模拟文件大小
    public readonly long DownloadSpeed;  // 模拟速度（字节/秒），默认 1MB/s

    public DownloadSimulateRequestArgs(string url, long fileSize, long downloadSpeed = 1024 * 1024);
}
```

---

## 核心类说明

### UnityWebRequestBackend

默认的后端实现，基于 Unity 的 UnityWebRequest API。

**特性：**
- 支持自定义 UnityWebRequest 创建方式（证书验证、代理等）
- 无需手动调用 Update()，UnityWebRequest 自动驱动

```csharp
// 自定义 UnityWebRequest 创建（建议通过 backend 构造函数传入）
UnityWebRequestCreator creator = (url, method) =>
{
    var request = new UnityWebRequest(url, method);
    // 自定义配置...
    return request;
};

IDownloadBackend backend = new UnityWebRequestBackend(creator);
```

### UnityWebRequestBase

抽象基类，封装所有下载器的通用逻辑。

**职责：**
- 管理请求生命周期和状态转换
- 实现看门狗监控机制
- 追踪下载进度和字节数
- 处理超时和错误

**生命周期：**

```
None ──► SendRequest() ──► Running ──► PollingRequest() ──┬──► Succeed
                                                          ├──► Failed
                                                          └──► Aborted
```

### 具体下载器

| 下载器 | 实现接口 | 使用场景 |
|--------|----------|----------|
| `UnityWebRequestFile` | `IDownloadFileRequest` | 大文件下载到本地 |
| `UnityWebRequestHead` | `IDownloadHeadRequest` | 检查资源信息 |
| `UnityWebRequestBytes` | `IDownloadBytesRequest` | 小文件内存加载 |
| `UnityWebRequestText` | `IDownloadTextRequest` | 文本文件下载 |
| `UnityWebRequestAssetBundle` | `IDownloadAssetBundleRequest` | AB 包下载加载 |
| `SimulateRequestFile` | `IDownloadFileRequest` | 编辑器模拟下载 |

---

## 使用示例

### 基础文件下载

```csharp
IDownloadBackend backend = new UnityWebRequestBackend();
IDownloadFileRequest request = null;
try
{
    // 1. 创建请求
    var args = new DownloadFileRequestArgs(
        url: "https://example.com/file.zip",
        savePath: "/path/to/save/file.zip",
        timeout: 30,
        watchdogTimeout: 0);
    request = backend.CreateFileRequest(args);

    // 2. 发起并轮询
    request.SendRequest();
    while (!request.IsDone)
    {
        await Task.Yield();
        float progress = request.DownloadProgress;
    }

    // 3. 检查结果
    if (request.Status == EDownloadRequestStatus.Succeed)
        Debug.Log("下载成功");
    else
        Debug.LogError($"下载失败: {request.Error}");
}
finally
{
    // 4. 清理资源
    request?.Dispose();
    backend.Dispose();
}
```

### 断点续传

```csharp
// 获取已下载的文件大小
long existingFileSize = new FileInfo(savePath).Length;

var args = new DownloadFileRequestArgs(
    url: url,
    savePath: savePath,
    timeout: 30,
    watchdogTimeout: 0,
    appendToFile: true,              // 追加写入
    removeFileOnAbort: false,        // 中止时保留文件
    resumeOffset: existingFileSize); // 断点位置

IDownloadFileRequest request = backend.CreateFileRequest(args);
request.SendRequest();
// ... 轮询等待完成
```

### 看门狗保护

```csharp
var args = new DownloadFileRequestArgs(
    url: url,
    savePath: path,
    timeout: 30,
    watchdogTimeout: 30); // 30秒无数据自动中止

IDownloadFileRequest request = backend.CreateFileRequest(args);
request.SendRequest();

while (!request.IsDone)
{
    await Task.Yield();
}

// 检查是否因看门狗超时而中止
if (request.Status == EDownloadRequestStatus.Aborted)
{
    Debug.LogWarning("下载超时，已自动中止");
}
```

### HEAD 请求获取文件信息

```csharp
var args = new DownloadDataRequestArgs(
    url: "https://example.com/file.zip",
    timeout: 30,
    watchdogTimeout: 0);

IDownloadHeadRequest request = backend.CreateHeadRequest(args);
request.SendRequest();

while (!request.IsDone)
{
    await Task.Yield();
}

if (request.Status == EDownloadRequestStatus.Succeed)
{
    long fileSize = request.ContentLength;
    string etag = request.ETag;
    string lastModified = request.LastModified;

    Debug.Log($"文件大小: {fileSize}, ETag: {etag}");
}
```

### 下载字节数据

```csharp
var args = new DownloadDataRequestArgs(
    url: "https://example.com/data.json",
    timeout: 30,
    watchdogTimeout: 0);

IDownloadBytesRequest request = backend.CreateBytesRequest(args);
request.SendRequest();

while (!request.IsDone)
{
    await Task.Yield();
}

if (request.Status == EDownloadRequestStatus.Succeed)
{
    byte[] data = request.Result;
    // 处理数据...
}
```

---

## 设计模式

### 工厂模式

`IDownloadBackend` 作为工厂接口，创建各类下载请求对象：

```
IDownloadBackend
    ├── CreateHeadRequest()    ──► IDownloadHeadRequest
    ├── CreateFileRequest()    ──► IDownloadFileRequest
    ├── CreateBytesRequest()   ──► IDownloadBytesRequest
    ├── CreateTextRequest()    ──► IDownloadTextRequest
    ├── CreateAssetBundleRequest() ──► IDownloadAssetBundleRequest
    └── CreateSimulateRequest() ──► IDownloadFileRequest
```

### 策略模式

通过实现 `IDownloadBackend` 接口，可以替换底层网络库：

```
IDownloadBackend (接口)
    ├── UnityWebRequestBackend  (默认实现)
    ├── BestHTTPBackend         (可扩展)
    └── CustomBackend           (自定义)
```

### 状态机模式

请求生命周期通过状态机管理：

```
┌──────┐    SendRequest()    ┌─────────┐
│ None │ ──────────────────► │ Running │
└──────┘                     └────┬────┘
                                  │ PollingRequest()
                    ┌─────────────┼─────────────┐
                    ▼             ▼             ▼
              ┌─────────┐  ┌──────────┐  ┌─────────┐
              │ Succeed │  │  Failed  │  │ Aborted │
              └─────────┘  └──────────┘  └─────────┘
```

### 看门狗模式

监控数据接收，防止网络卡顿导致请求无限等待：

```
每帧轮询 PollingRequest()
    │
    ├── 收到新数据 ──► 重置计时器
    │
    └── 未收到数据 ──► 计时器累加
                           │
                           └── 超过 WatchdogTimeout ──► AbortRequest()
```

---

## 类继承关系

```
IDownloadRequest (基础接口)
    │
    ├── IDownloadHeadRequest        (HEAD 请求)
    ├── IDownloadFileRequest        (文件下载)
    ├── IDownloadBytesRequest       (字节下载)
    ├── IDownloadTextRequest        (文本下载)
    └── IDownloadAssetBundleRequest (AssetBundle 下载)

UnityWebRequestBase (抽象基类)
    │
    ├── UnityWebRequestFile          ──► IDownloadFileRequest
    ├── UnityWebRequestHead          ──► IDownloadHeadRequest
    ├── UnityWebRequestBytes         ──► IDownloadBytesRequest
    ├── UnityWebRequestText          ──► IDownloadTextRequest
    └── UnityWebRequestAssetBundle   ──► IDownloadAssetBundleRequest

SimulateRequestFile (独立实现) ──► IDownloadFileRequest
```

---

## 工具类

### DownloadSystemTools

提供跨平台的 URL 转换和判断功能：

| 方法 | 参数 | 返回值 | 说明 |
|------|------|--------|------|
| `ToLocalURL(path)` | `string path` 本地文件路径 | 可用于 UnityWebRequest 的文件协议 URL | 转换本地路径为文件协议 URL（自动处理特殊字符） |
| `IsLocalFileURL(url)` | `string url` 要判断的 URL | `bool` 是否为本地文件 URL | 判断 URL 是否为 `file:` 或 `jar:file:` 协议 |

### DownloadFailureCounter

网络请求失败计数器（诊断用）：

- **线程安全**：内部使用 `Dictionary` 且未加锁，约定只在 Unity 主线程调用；如需在多线程/回调线程调用，请在外层加锁或改为并发容器实现
- **Key 格式**：`$"{packageName}_{eventName}"`
- **统计口径**：**仅统计网络请求失败**（`IDownloadRequest.Status != Succeed` 时记录），不统计内容为空、校验失败、解析失败等业务层失败

| 方法 | 参数 | 返回值 | 说明 |
|------|------|--------|------|
| `RecordFailure(packageName, eventName)` | `string packageName` 资源包名称, `string eventName` 事件名称 | `void` | 记录一次失败 |
| `GetFailureCount(packageName, eventName)` | `string packageName` 资源包名称, `string eventName` 事件名称 | `int` 失败次数（未记录过返回 0） | 获取失败次数 |

```csharp
// 记录失败
DownloadFailureCounter.RecordFailure(packageName, eventName);

// 查询失败次数
int count = DownloadFailureCounter.GetFailureCount(packageName, eventName);
```

---

## 注意事项

1. **资源释放**：使用完毕后务必调用 `Dispose()` 释放资源
   - `AbortRequest()` 仅用于中止请求与切换状态，不等同于释放资源；无论成功/失败/中止都需要 `Dispose()`
   - 第三方 `IDownloadBackend` 可能持有原生资源/线程/连接池等，上层在不再使用时也应调用 `backend.Dispose()`
   - 推荐使用 `try/finally` 确保释放（尤其是上层可能提前中止的场景）
2. **断点续传**：需要服务器支持 `Range` 请求头和 `206 Partial Content` 响应
   - 若服务端不支持 Range 仍返回 200，全量内容可能会被追加写入，导致文件损坏
3. **看门狗超时**：设置为 0 表示禁用，建议根据网络环境设置合理值
4. **内存下载**：`IDownloadBytesRequest` 会将整个响应体加载到内存，不适合大文件
5. **驱动更新**：部分第三方网络库实现的 backend 可能需要每帧调用 `IDownloadBackend.Update()` 进行驱动
6. **中止语义**：`Aborted` 可能来自用户主动 `AbortRequest()` 或看门狗超时；中止场景下 `HttpCode/Error` 可能为默认值（例如 0/空）
7. **线程安全**：所有下载请求的创建和轮询应在主线程进行
8. **模拟下载器**：`SimulateRequestFile` 仅用于模拟进度，不会落盘，且 `SavePath` 始终为 `null`；成功时 `HttpCode` 固定为 `200`
