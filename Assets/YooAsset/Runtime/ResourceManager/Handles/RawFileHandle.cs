
namespace YooAsset
{
    public class RawFileHandle : HandleBase
    {
        private System.Action<RawFileHandle> _callback;

        internal RawFileHandle(ProviderOperation provider) : base(provider)
        {
        }
        internal override void InvokeCallback()
        {
            _callback?.Invoke(this);
        }

        /// <summary>
        /// 完成委托
        /// </summary>
        public event System.Action<RawFileHandle> Completed
        {
            add
            {
                if (IsValidWithWarning == false)
                    throw new YooHandleException($"{nameof(RawFileHandle)} is invalid. It may have been released or the provider was destroyed.");
                if (Provider.IsDone)
                    value.Invoke(this);
                else
                    _callback += value;
            }
            remove
            {
                if (IsValidWithWarning == false)
                    throw new YooHandleException($"{nameof(RawFileHandle)} is invalid. It may have been released or the provider was destroyed.");
                _callback -= value;
            }
        }

        /// <summary>
        /// 等待异步执行完毕
        /// </summary>
        public void WaitForAsyncComplete()
        {
            if (IsValidWithWarning == false)
                return;
            Provider.WaitForCompletion();
        }

        /// <summary>
        /// 获取原生文件的路径
        /// </summary>
        public string GetRawFilePath()
        {
            if (IsValidWithWarning == false)
                return string.Empty;
            return Provider.LoadedBundleResult.GetBundleFilePath();
        }
    }
}