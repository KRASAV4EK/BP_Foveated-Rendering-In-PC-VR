using UnrealBuildTool;
public class ImageBasedVRS : ModuleRules
{
    public ImageBasedVRS(ReadOnlyTargetRules Target) : base(Target)
    {
        PCHUsage = PCHUsageMode.UseExplicitOrSharedPCHs;
        PublicDependencyModuleNames.AddRange(new[] {
            "Core", "CoreUObject", "Engine", "Projects",
            "RHI", "RenderCore", "Renderer"
        });
        PrivateDependencyModuleNames.AddRange(new[] {
            "Core", "CoreUObject", "Engine", "Projects",
            "RHI", "RenderCore", "Renderer"
        });
    }
}
