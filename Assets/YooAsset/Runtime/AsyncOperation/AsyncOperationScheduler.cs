using System;
using System.Collections.Generic;

namespace YooAsset
{
    /// <summary>
    /// 异步操作调度器
    /// </summary>
    internal class AsyncOperationScheduler : IComparable<AsyncOperationScheduler>
    {
        private readonly List<AsyncOperationBase> _runningOperations = new List<AsyncOperationBase>(100);
        private readonly List<AsyncOperationBase> _pendingOperations = new List<AsyncOperationBase>(100);
        private uint _priority;

        /// <summary>
        /// 所属包裹名称
        /// </summary>
        public string PackageName { get; private set; }

        /// <summary>
        /// 调度器优先级（值越大越优先）
        /// </summary>
        public uint Priority
        {
            get { return _priority; }
            set
            {
                if (_priority != value)
                {
                    _priority = value;
                    IsDirty = true;
                }
            }
        }

        /// <summary>
        /// 优先级是否已变更（需要重新排序）
        /// </summary>
        public bool IsDirty { get; set; }

        /// <summary>
        /// 创建顺序（用于同优先级稳定排序）
        /// </summary>
        public int CreationOrder { get; private set; }


        public AsyncOperationScheduler(string packageName, int creationOrder)
        {
            PackageName = packageName;
            CreationOrder = creationOrder;
        }

        /// <summary>
        /// 开始处理异步操作
        /// </summary>
        /// <remarks>
        /// 操作会立即启动，但会先添加到待处理队列。
        /// 在下一次Update时才会合并到执行队列并参与调度更新
        /// </remarks>
        public void StartOperation(AsyncOperationBase operation)
        {
            _pendingOperations.Add(operation);
            operation.StartOperation();
        }

        /// <summary>
        /// 更新调度器
        /// </summary>
        public void Update()
        {
            // 移除已经完成的异步操作
            for (int i = _runningOperations.Count - 1; i >= 0; i--)
            {
                var operation = _runningOperations[i];
                if (operation.IsFinished)
                {
                    _runningOperations.RemoveAt(i);
                }
            }

            // 添加新增的异步操作
            if (_pendingOperations.Count > 0)
            {
                _runningOperations.AddRange(_pendingOperations);
                _pendingOperations.Clear();
            }

            // 检测是否需要执行排序
            bool isDirty = false;
            foreach (var operation in _runningOperations)
            {
                if (operation.IsDirty)
                {
                    operation.IsDirty = false;
                    isDirty = true;
                }
            }
            if (isDirty)
            {
                _runningOperations.Sort();
            }

            // 更新进行中的异步操作
            for (int i = 0; i < _runningOperations.Count; i++)
            {
                // 检查全局时间切片预算
                if (AsyncOperationSystem.IsBusy)
                    break;

                var operation = _runningOperations[i];
                if (operation.IsFinished)
                    continue;

                operation.UpdateOperation();
            }
        }

        /// <summary>
        /// 中止所有任务
        /// </summary>
        public void AbortAll()
        {
            // 终止临时队列里的任务
            foreach (var operation in _pendingOperations)
            {
                operation.AbortOperation();
            }
            _pendingOperations.Clear();

            // 终止正在进行的任务
            foreach (var operation in _runningOperations)
            {
                operation.AbortOperation();
            }
            _runningOperations.Clear();
        }

        /// <summary>
        /// 获取调试信息
        /// </summary>
        public List<DiagnosticOperationInfo> GetDiagnosticInfos()
        {
            int totalCount = _runningOperations.Count + _pendingOperations.Count;
            List<DiagnosticOperationInfo> result = new List<DiagnosticOperationInfo>(totalCount);

            // 包含正在执行的任务
            foreach (var operation in _runningOperations)
            {
                var operationInfo = operation.GetDiagnosticInfo();
                result.Add(operationInfo);
            }

            // 包含待处理的新任务
            foreach (var operation in _pendingOperations)
            {
                var operationInfo = operation.GetDiagnosticInfo();
                result.Add(operationInfo);
            }

            return result;
        }

        #region 排序接口实现
        public int CompareTo(AsyncOperationScheduler other)
        {
            // 优先级高的排前面
            int result = other.Priority.CompareTo(this.Priority);
            if (result == 0)
            {
                // 优先级相同，按创建顺序
                result = this.CreationOrder.CompareTo(other.CreationOrder);
            }
            return result;
        }
        #endregion
    }
}
