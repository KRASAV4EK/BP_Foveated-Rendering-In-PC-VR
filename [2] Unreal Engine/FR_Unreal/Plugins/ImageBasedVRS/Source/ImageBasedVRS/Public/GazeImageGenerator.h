#pragma once

#if PLATFORM_WINDOWS

#include "CoreMinimal.h"
#include "RHI.h"
#include "RendererInterface.h"
#include "RenderGraphDefinitions.h"
#include "VariableRateShadingImageManager.h"
#include "Engine/Engine.h"

class GazeImageGenerator : public IVariableRateShadingImageGenerator
{
public:
	void SetEnabled(bool bInEnabled);
	void SetGazeNormalized(FVector2f GazePoint);

	virtual ~GazeImageGenerator() override {};
	virtual FRDGTextureRef GetImage(FRDGBuilder& GraphBuilder, const FViewInfo& ViewInfo, FVariableRateShadingImageManager::EVRSImageType ImageType) override;
	virtual void PrepareImages(FRDGBuilder& GraphBuilder, const FSceneViewFamily& ViewFamily, const FMinimalSceneTextures& SceneTextures) override;
	virtual bool IsEnabledForView(const FSceneView& View) const override;
	virtual FVariableRateShadingImageManager::EVRSSourceType GetType() const override;
	virtual FRDGTextureRef GetDebugImage(FRDGBuilder& GraphBuilder, const FViewInfo& ViewInfo, FVariableRateShadingImageManager::EVRSImageType ImageType) override;
private:
	FRDGTextureRef CachedImage = nullptr;
	struct FDynamicVRSData
	{
		float	VRSAmount = 1.0f;
		double	SumBusyTime = 0.0;
		int		NumFramesStored = 0;
		uint32	LastUpdateFrame = 0;
	} DynamicVRSData;
	float UpdateDynamicVRSAmount();
	bool IsGazeTrackingEnabled() const;
	FRDGTextureDesc GetSRIDesc();

	std::atomic<bool>   IsETFREnabled{false};
	std::atomic<float>  GazeX{ 0.5f }, GazeY{ 0.5f };
};

#endif // PLATFORM_WINDOWS
