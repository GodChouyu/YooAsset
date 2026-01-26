
namespace YooAsset
{
    internal class EFSInitializeOperation : FSInitializeOperation
    {
        private readonly EditorFileSystem _fileSytem;

        internal EFSInitializeOperation(EditorFileSystem fileSystem)
        {
            _fileSytem = fileSystem;
        }
        internal override void InternalStart()
        {
            Status = EOperationStatus.Succeeded;
        }
        internal override void InternalUpdate()
        {
        }
    }
}