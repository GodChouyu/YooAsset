
namespace YooAsset
{
    internal class WRFSInitializeOperation : FSInitializeOperation
    {
        private readonly WebRemoteFileSystem _fileSystem;

        public WRFSInitializeOperation(WebRemoteFileSystem fileSystem)
        {
            _fileSystem = fileSystem;
        }
        internal override void InternalStart()
        {
            Status = EOperationStatus.Succeed;
        }
        internal override void InternalUpdate()
        {
        }
    }
}