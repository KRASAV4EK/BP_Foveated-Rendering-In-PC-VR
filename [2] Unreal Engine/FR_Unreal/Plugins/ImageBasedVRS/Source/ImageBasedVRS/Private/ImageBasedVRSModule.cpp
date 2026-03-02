#if PLATFORM_WINDOWS

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "Interfaces/IPluginManager.h"
#include "Misc/Paths.h"
#include "ShaderCore.h"
#include "GazeImageGenerator.h"

RENDERER_API extern TGlobalResource<FVariableRateShadingImageManager> GVRSImageManager;
TSharedPtr<GazeImageGenerator, ESPMode::ThreadSafe> GGen;

class FImageBasedVRSModule : public IModuleInterface
{
public:
    virtual void StartupModule() override
    {
        IPlugin* Plugin = IPluginManager::Get().FindPlugin(TEXT("ImageBasedVRS")).Get();
        const FString ShaderDir = FPaths::Combine(Plugin->GetBaseDir(), TEXT("Shaders"));
        AddShaderSourceDirectoryMapping(TEXT("/Plugin/ImageBasedVRS"), ShaderDir);
        GGen = MakeShared<GazeImageGenerator, ESPMode::ThreadSafe>();
        GVRSImageManager.RegisterExternalImageGenerator(GGen.Get());
    }
    virtual void ShutdownModule() override
    {
        if (GGen.IsValid())
        {
            GVRSImageManager.UnregisterExternalImageGenerator(GGen.Get());
            GGen.Reset();
        }
    }
};
IMPLEMENT_MODULE(FImageBasedVRSModule, ImageBasedVRS)

#endif // PLATFORM_WINDOWS
