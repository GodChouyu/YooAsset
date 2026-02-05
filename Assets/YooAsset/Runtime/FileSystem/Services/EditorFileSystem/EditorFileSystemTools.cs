
namespace YooAsset
{
    internal static class EditorFileSystemTools
    {
        public static string GetEditorFilePath(PackageBundle bundle)
        {
            if (bundle.IncludeMainAssets.Count == 0)
                return string.Empty;

            var pacakgeAsset = bundle.IncludeMainAssets[0];
            return pacakgeAsset.AssetPath;
        }
    }
}