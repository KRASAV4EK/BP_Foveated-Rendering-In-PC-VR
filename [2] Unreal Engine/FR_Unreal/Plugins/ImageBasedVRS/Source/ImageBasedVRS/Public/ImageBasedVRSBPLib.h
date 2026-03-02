#pragma once

#include "Kismet/BlueprintFunctionLibrary.h"
#include "ImageBasedVRSBPLib.generated.h"

UCLASS()
class UImageBasedVRSBPLib : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:
	UFUNCTION(BlueprintCallable, Category = "VRS")
	static void VRS_SetETFREnabled(bool IsETFREnabled);

	UFUNCTION(BlueprintCallable, Category = "VRS")
	static void VRS_SetGaze(float X, float Y);
};
