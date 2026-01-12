
namespace YooAsset
{
    public class DestroyPackageOperation : AsyncOperationBase
    {
        private enum ESteps
        {
            None,
            CheckInitStatus,
            UnloadAllAssets,
            DestroyPackage,
            Done,
        }

        private readonly ResourcePackage _resourcePackage;
        private readonly UnloadAllAssetsOptions _options;
        private UnloadAllAssetsOperation _unloadAllAssetsOp;
        private ESteps _steps = ESteps.None;


        public DestroyPackageOperation(ResourcePackage resourcePackage, UnloadAllAssetsOptions options)
        {
            _resourcePackage = resourcePackage;
            _options = options;
        }

        internal override void InternalStart()
        {
            _steps = ESteps.CheckInitStatus;
        }
        internal override void InternalUpdate()
        {
            if (_steps == ESteps.None || _steps == ESteps.Done)
                return;

            if (_steps == ESteps.CheckInitStatus)
            {
                switch (_resourcePackage.InitializeStatus)
                {
                    case EOperationStatus.None:
                    case EOperationStatus.Failed:
                    case EOperationStatus.Aborted:
                        _steps = ESteps.DestroyPackage;
                        break;

                    case EOperationStatus.Processing:
                        _steps = ESteps.Done;
                        Status = EOperationStatus.Failed;
                        Error = "The Package is initializing. Please try to destroy the package again later.";
                        break;

                    case EOperationStatus.Succeed:
                        if (_resourcePackage.PackageValid)
                            _steps = ESteps.UnloadAllAssets;
                        else
                            _steps = ESteps.DestroyPackage;
                        break;

                    default:
                        throw new System.NotImplementedException(_resourcePackage.InitializeStatus.ToString());
                }
            }

            if (_steps == ESteps.UnloadAllAssets)
            {
                if (_unloadAllAssetsOp == null)
                {
                    _unloadAllAssetsOp = _resourcePackage.UnloadAllAssetsAsync(_options);
                    _unloadAllAssetsOp.StartOperation();
                    AddChildOperation(_unloadAllAssetsOp);
                }

                _unloadAllAssetsOp.UpdateOperation();
                if (_unloadAllAssetsOp.IsDone == false)
                    return;

                if (_unloadAllAssetsOp.Status == EOperationStatus.Succeed)
                {
                    _steps = ESteps.DestroyPackage;
                }
                else
                {
                    _steps = ESteps.Done;
                    Status = EOperationStatus.Failed;
                    Error = _unloadAllAssetsOp.Error;
                }
            }

            if (_steps == ESteps.DestroyPackage)
            {
                // 销毁包裹
                _resourcePackage.InternalDestroy();

                // 最后清理该包裹的异步任务
                // 注意：对于有线程操作的异步任务，需要保证线程安全释放。
                OperationSystem.ClearPackageOperations(_resourcePackage.PackageName);

                _steps = ESteps.Done;
                Status = EOperationStatus.Succeed;
            }
        }
        internal override string InternalGetDescription()
        {
            return $"PackageVersion : {_resourcePackage.GetPackageVersion()}";
        }
    }
}