
namespace YooAsset
{
    public class EditorSimulateBuildInvoker
    {
        public static PackageInvokeBuildResult Build(string packageName)
        {
            var buildParam = new PackageInvokeBuildParam(packageName);
            buildParam.BuildPipelineName = "EditorSimulateBuildPipeline";
            buildParam.InvokeAssmeblyName = "YooAsset.Editor";
            buildParam.InvokeClassFullName = "YooAsset.Editor.AssetBundleSimulateBuilder";
            buildParam.InvokeMethodName = "SimulateBuild";
            return PackageInvokeBuilder.InvokeBuilder(buildParam);
        }
    }
}