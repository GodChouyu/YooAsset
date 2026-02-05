using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace YooAsset
{
    /// <summary>
    /// 异步操作基类，所有异步操作的抽象基类
    /// 支持协程（IEnumerator）、Task（async/await）、回调等多种异步编程模式
    /// </summary>
    public abstract class AsyncOperationBase : IEnumerator, IComparable<AsyncOperationBase>
    {
        private List<AsyncOperationBase> _children;
        private Action<AsyncOperationBase> _onCompleted;
        private uint _priority;

        /// <summary>
        /// 是否正处于同步等待状态
        /// </summary>
        internal bool IsWaitForCompletion { get; private set; }

        /// <summary>
        /// 标记脏（用于调度器检测并重排）
        /// </summary>
        internal bool IsDirty { get; set; }

        /// <summary>
        /// 任务是否已结束（已触发回调和Task完成）
        /// </summary>
        internal bool IsFinished { get; private set; }

        /// <summary>
        /// 当前帧时间切片是否已用完（同步等待时始终返回false）
        /// </summary>
        internal bool IsBusy
        {
            get
            {
                if (IsWaitForCompletion)
                    return false;
                return AsyncOperationSystem.IsBusy;
            }
        }

        /// <summary>
        /// 任务优先级（值越大越优先执行）
        /// </summary>
        public uint Priority
        {
            set
            {
                if (_priority == value)
                    return;
                _priority = value;
                IsDirty = true;
            }
            get
            {
                return _priority;
            }
        }

        /// <summary>
        /// 任务状态
        /// </summary>
        public EOperationStatus Status { get; protected set; } = EOperationStatus.None;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error { get; protected set; }

        /// <summary>
        /// 处理进度
        /// </summary>
        public float Progress { get; protected set; }

        /// <summary>
        /// 任务逻辑是否完成（Status为Succeeded、Failed或Aborted）
        /// </summary>
        public bool IsDone
        {
            get
            {
                return Status == EOperationStatus.Succeeded || Status == EOperationStatus.Failed || Status == EOperationStatus.Aborted;
            }
        }

        /// <summary>
        /// 完成事件
        /// </summary>
        public event Action<AsyncOperationBase> Completed
        {
            add
            {
                if (value == null)
                    return;

                if (IsDone)
                {
                    try
                    {
                        // 注意：任务已完成，立即调用回调
                        value.Invoke(this);
                    }
                    catch (Exception ex)
                    {
                        YooLogger.Error($"Exception in completion callback: {ex}");
                    }
                }
                else
                {
                    _onCompleted += value;
                }
            }
            remove
            {
                _onCompleted -= value;
            }
        }

        /// <summary>
        /// 用于 async/await 的 Task 对象
        /// </summary>
        public Task Task
        {
            get
            {
                if (_taskCompletionSource == null)
                {
                    _taskCompletionSource = new TaskCompletionSource<object>();
                    if (IsDone)
                        _taskCompletionSource.SetResult(null);
                }
                return _taskCompletionSource.Task;
            }
        }


        /// <summary>
        /// 内部启动方法（子类必须实现）
        /// </summary>
        internal abstract void InternalStart();

        /// <summary>
        /// 内部更新方法（子类必须实现）
        /// </summary>
        internal abstract void InternalUpdate();

        /// <summary>
        /// 内部中止方法（子类可选实现）
        /// </summary>
        internal virtual void InternalAbort()
        {
        }

        /// <summary>
        /// 内部释放方法（子类可选实现）
        /// </summary>
        internal virtual void InternalDispose()
        {
        }

        /// <summary>
        /// 获取操作的描述信息（子类可选实现）
        /// </summary>
        internal virtual string InternalGetDescription()
        {
            return string.Empty;
        }

        /// <summary>
        /// 内部同步等待方法（子类可选实现）
        /// 默认抛出异常，如果异步操作需要支持，子类应重写以支持同步等待
        /// </summary>
        internal virtual void InternalWaitForCompletion()
        {
            throw new YooInternalException($"InternalWaitForCompletion() is not implemented: {this.GetType().Name}");
        }

        /// <summary>
        /// 添加子任务
        /// </summary>
        internal void AddChildOperation(AsyncOperationBase child)
        {
            if (_children == null)
                _children = new List<AsyncOperationBase>(10);

#if UNITY_EDITOR || DEBUG
            if (child == null)
                throw new YooInternalException("Child operation is null.");

            if (ReferenceEquals(child, this))
                throw new YooInternalException("Cannot add operation as its own child.");

            if (_children.Contains(child))
                throw new YooInternalException($"Child operation {child.GetType().Name} already exists.");

            // 禁止形成环依赖
            if (WouldCreateCycle(child))
                throw new YooInternalException($"Adding {child.GetType().Name} would create a circular dependency with {this.GetType().Name}.");
#endif

            _children.Add(child);
        }

        /// <summary>
        /// 移除子任务
        /// </summary>
        internal void RemoveChildOperation(AsyncOperationBase child)
        {
            if (_children == null)
                return;

#if UNITY_EDITOR || DEBUG
            if (child == null)
                throw new YooInternalException("Child operation is null.");

            if (_children.Contains(child) == false)
                throw new YooInternalException($"Child operation {child.GetType().Name} does not exist.");
#endif

            _children.Remove(child);
        }

        /// <summary>
        /// 获取异步操作说明
        /// </summary>
        internal string GetOperationDescription()
        {
            return InternalGetDescription();
        }

        /// <summary>
        /// 开始异步操作
        /// </summary>
        internal void StartOperation()
        {
            if (Status == EOperationStatus.None)
            {
                Status = EOperationStatus.Processing;

                // 开始记录
                DebugBeginRecording();

                // 开始任务
                try
                {
                    InternalStart();
                }
                catch (Exception ex)
                {
                    Status = EOperationStatus.Failed;
                    Error = ex.ToString();
                    YooLogger.Error($"Exception in {this.GetType().Name}.InternalStart: {ex}");
                }
            }
        }

        /// <summary>
        /// 更新异步操作
        /// </summary>
        internal void UpdateOperation()
        {
            if (IsDone == false)
            {
                // 更新记录
                DebugUpdateRecording();

                // 更新任务
                // 注意：兜底隔离机制
                // 说明：检测的异常源包含：I/O（解压/读写权限/磁盘满），平台差异等
                try
                {
                    InternalUpdate();
                }
                catch (Exception ex)
                {
                    Status = EOperationStatus.Failed;
                    Error = ex.ToString();
                    YooLogger.Error($"Exception in {this.GetType().Name}.InternalUpdate: {ex}");
                }
            }

            if (IsDone && IsFinished == false)
            {
                FinishOperation();
            }
        }

        /// <summary>
        /// 终止异步任务（递归中止所有子任务）
        /// </summary>
        internal void AbortOperation()
        {
            if (_children != null)
            {
                foreach (var child in _children)
                {
                    child.AbortOperation();
                }
            }

            if (IsDone == false)
            {
                InternalAbort();
                Status = EOperationStatus.Aborted;
                Error = "Aborted by user";
                YooLogger.Warning($"Async operation {this.GetType().Name} has been aborted.");
            }

            // 注意：强制收尾，确保Task能完成
            FinishOperation();
        }

        /// <summary>
        /// 完成异步任务（触发回调和Task完成）
        /// </summary>
        private void FinishOperation()
        {
            if (IsFinished == false)
            {
                IsFinished = true;
                Progress = 1f;

                // 结束记录
                DebugEndRecording();

                try
                {
                    InternalDispose();
                }
                catch (Exception ex)
                {
                    YooLogger.Error($"Exception in {this.GetType().Name}.InternalDispose: {ex}");
                }

                if (_onCompleted != null)
                {
                    var invocations = _onCompleted.GetInvocationList();
                    InvokeSafely(invocations);
                    _onCompleted = null;
                }

                if (_taskCompletionSource != null)
                    _taskCompletionSource.TrySetResult(null);
            }
        }
        private void InvokeSafely(Delegate[] invocations)
        {
            foreach (var handler in invocations)
            {
                try
                {
                    ((Action<AsyncOperationBase>)handler).Invoke(this);
                }
                catch (Exception ex)
                {
                    YooLogger.Error($"Exception in inoke callback: {ex}");
                }
            }
        }

        /// <summary>
        /// 执行一次更新逻辑
        /// </summary>
        protected void ExecuteOnce()
        {
            if (IsDone)
                return;

            UpdateOperation();
        }

        /// <summary>
        /// 批量执行一定次数的更新逻辑
        /// </summary>
        /// <param name="count">最大执行次数，默认1000次</param>
        /// <remarks>
        /// 用于需要快速完成但又不想完全阻塞主线程的场景。
        /// </remarks>
        protected void ExecuteBatch(int count = 1000)
        {
            if (IsDone)
                return;

            int runCount = count;
            while (true)
            {
                // 执行更新逻辑
                UpdateOperation();
                if (IsDone)
                    break;

                // 当执行次数用完时
                runCount--;
                if (runCount <= 0)
                    break;
            }
        }

        /// <summary>
        /// 无限次数的执行更新逻辑，直到任务完成
        /// 注意：该方法会阻塞主线程
        /// </summary>
        /// <param name="sleepMS">休眠时长</param>
        protected void ExecuteUntilComplete(int sleepMS = 1)
        {
            if (IsDone)
                return;

            while (true)
            {
                UpdateOperation();
                if (IsDone)
                    break;

                // 注意：短暂休眠避免完全占用CPU资源
                System.Threading.Thread.Sleep(sleepMS);
            }
        }

        /// <summary>
        /// 同步等待异步执行完毕（会阻塞当前线程）
        /// </summary>
        public void WaitForCompletion()
        {
            // 注意：防止异步操作被挂起陷入无限死循环
            if (Status == EOperationStatus.None)
            {
                StartOperation();
            }

            if (IsWaitForCompletion == false)
            {
                IsWaitForCompletion = true;

                if (IsDone == false)
                    InternalWaitForCompletion();

                if (IsDone == false)
                {
                    Status = EOperationStatus.Failed;
                    Error = $"Operation {this.GetType().Name} failed to wait for completion.";
                    YooLogger.Error(Error);
                }

                // 注意：强制收尾，确保Task能完成
                FinishOperation();
            }
        }

        #region 调试信息
        /// <summary>
        /// 任务开始的时间（格式：HH:MM:SS，仅DEBUG模式有效）
        /// </summary>
        public string StartTime { get; protected set; }

        /// <summary>
        /// 处理耗时（单位：毫秒）
        /// </summary>
        public long ElapsedMilliseconds { get; protected set; }

        /// <summary>
        /// 任务耗时计时器
        /// </summary>
        private Stopwatch _stopwatch = null;

        [Conditional("DEBUG")]
        private void DebugBeginRecording()
        {
            if (_stopwatch == null)
            {
                StartTime = FormatElapsedTime(TimeUtility.RealtimeSinceStartup);
                _stopwatch = Stopwatch.StartNew();
            }
        }

        [Conditional("DEBUG")]
        private void DebugUpdateRecording()
        {
            if (_stopwatch != null)
            {
                ElapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
            }
        }

        [Conditional("DEBUG")]
        private void DebugEndRecording()
        {
            if (_stopwatch != null)
            {
                ElapsedMilliseconds = _stopwatch.ElapsedMilliseconds;
                _stopwatch = null;
            }
        }

        /// <summary>
        /// 将游戏运行时间格式化为 HH:MM:SS 格式
        /// </summary>
        /// <param name="time">运行时间（秒）</param>
        private string FormatElapsedTime(double time)
        {
            double h = System.Math.Floor(time / 3600);
            double m = System.Math.Floor(time / 60 - h * 60);
            double s = System.Math.Floor(time - m * 60 - h * 3600);
            return h.ToString("00") + ":" + m.ToString("00") + ":" + s.ToString("00");
        }

        /// <summary>
        /// 检测添加子任务是否会形成循环依赖
        /// 使用深度优先搜索（DFS）遍历子任务图
        /// </summary>
        private bool WouldCreateCycle(AsyncOperationBase child)
        {
            const int MaxCycleCheckDepth = 4096; // 循环检测最大深度
            var stack = new Stack<AsyncOperationBase>();
            var visited = new HashSet<AsyncOperationBase>();
            stack.Push(child);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node == null)
                    continue;

                // 防止重复访问
                if (visited.Add(node) == false)
                    continue;

                // 防止无限循环（图过大）
                if (visited.Count > MaxCycleCheckDepth)
                    throw new YooInternalException("Child operation graph is too large, cycle check aborted.");

                // 检测循环：如果遍历到自己，说明形成循环
                if (ReferenceEquals(node, this))
                    return true;

                if (node._children == null)
                    continue;

                // 将子节点加入栈
                for (int i = 0; i < node._children.Count; i++)
                {
                    stack.Push(node._children[i]);
                }
            }

            return false;
        }

        /// <summary>
        /// 获取调试信息
        /// 注意：递归构建子树存在深度风险
        /// </summary>
        internal DiagnosticOperationInfo GetDiagnosticInfo()
        {
            var operationInfo = new DiagnosticOperationInfo();
            operationInfo.OperationName = this.GetType().Name;
            operationInfo.OperationDesc = GetOperationDescription();
            operationInfo.Priority = Priority;
            operationInfo.Progress = Progress;
            operationInfo.StartTime = StartTime;
            operationInfo.ElapsedMilliseconds = ElapsedMilliseconds;
            operationInfo.Status = Status.ToString();

            if (_children == null)
            {
                operationInfo.Children = new List<DiagnosticOperationInfo>();
            }
            else
            {
                operationInfo.Children = new List<DiagnosticOperationInfo>(_children.Count);
                foreach (var child in _children)
                {
                    var childInfo = child.GetDiagnosticInfo();
                    operationInfo.Children.Add(childInfo);
                }
            }

            return operationInfo;
        }
        #endregion

        #region 排序接口实现
        public int CompareTo(AsyncOperationBase other)
        {
            return other.Priority.CompareTo(this.Priority);
        }
        #endregion

        #region 异步编程相关
        /// <summary>
        /// 用于支持 async/await 的任务完成源
        /// </summary>
        private TaskCompletionSource<object> _taskCompletionSource;

        bool IEnumerator.MoveNext()
        {
            return !IsDone;
        }
        void IEnumerator.Reset()
        {
        }
        object IEnumerator.Current => null;
        #endregion
    }
}
