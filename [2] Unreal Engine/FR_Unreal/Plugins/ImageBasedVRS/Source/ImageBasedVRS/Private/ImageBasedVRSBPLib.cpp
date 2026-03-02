#include "ImageBasedVRSBPLib.h"
#include "GazeImageGenerator.h"
#include "Misc/ScopeLock.h"
#include "HAL/PlatformCrt.h"
#include "HAL/IConsoleManager.h"

extern TSharedPtr<GazeImageGenerator, ESPMode::ThreadSafe> GGen;

void UImageBasedVRSBPLib::VRS_SetETFREnabled(bool IsETFREnabled)
{
#if PLATFORM_WINDOWS
	//GGen.Get()->SetEnabled(IsETFREnabled);
    if (GGen.IsValid())
    {
        ENQUEUE_RENDER_COMMAND(VRS_SetEnabledCmd)(
            [Enabled = IsETFREnabled](FRHICommandListImmediate&)
            {
                if (GGen.IsValid())
                    GGen->SetEnabled(Enabled);
            }
            );
    }
#else
	(void)IsETFREnabled;
#endif
}

void UImageBasedVRSBPLib::VRS_SetGaze(float X, float Y)
{
#if PLATFORM_WINDOWS
	//GGen.Get()->SetGazeNormalized(FVector2f(X, Y));
    if (GGen.IsValid())
    {
        const FVector2f Gaze(X, Y);
        ENQUEUE_RENDER_COMMAND(VRS_SetGazeCmd)(
            [Gaze](FRHICommandListImmediate&)
            {
                if (GGen.IsValid())
                    GGen->SetGazeNormalized(Gaze);
            }
            );
    }
#else
	(void)X; (void)Y;
#endif
}
