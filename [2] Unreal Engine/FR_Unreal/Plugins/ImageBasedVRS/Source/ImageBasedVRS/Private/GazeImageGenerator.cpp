#if PLATFORM_WINDOWS

#include "GazeImageGenerator.h"
#include "SystemTextures.h"
#include "GlobalShader.h"
#include "DataDrivenShaderPlatformInfo.h"
#include "RenderGraphUtils.h"
#include "VariableRateShadingImageManager.h"
#include "SceneTexturesConfig.h"

static TAutoConsoleVariable<int> CVarFoveationLevel(
	TEXT("r.VRS.FoveationLevel"),
	0,
	TEXT("Level of foveated VRS to apply on desktop (when Variable Rate Shading is available)\n")
	TEXT(" 0: Disabled (default)\n")
	TEXT(" 1: Low\n")
	TEXT(" 2: Medium\n")
	TEXT(" 3: High\n"),
	ECVF_RenderThreadSafe);

static TAutoConsoleVariable<int32> CVarFoveationPreview(
	TEXT("r.VRS.FoveationPreview"),
	1,
	TEXT("Show desktop foveated VRS in debug overlay\n")
	TEXT(" 0: Disabled (default)\n 1: Enabled\n"),
	ECVF_RenderThreadSafe);

constexpr int32 kComputeGroupSize = FComputeShaderUtils::kGolden2DGroupSize;

class FDesktopFoveatedShader : public FGlobalShader
{
public:
	DECLARE_GLOBAL_SHADER(FDesktopFoveatedShader);
	SHADER_USE_PARAMETER_STRUCT(FDesktopFoveatedShader, FGlobalShader);

	BEGIN_SHADER_PARAMETER_STRUCT(FParameters, )
		SHADER_PARAMETER_RDG_TEXTURE_UAV(RWTexture2D<uint>, RWOutputTexture)
		SHADER_PARAMETER(FVector2f, GazePixelXY)
		SHADER_PARAMETER(float, ViewDiagonalSquaredInPixels)
		SHADER_PARAMETER(float, FoveationFullRateCutoffSquared)
		SHADER_PARAMETER(float, FoveationHalfRateCutoffSquared)
	END_SHADER_PARAMETER_STRUCT()

	static bool ShouldCompilePermutation(const FGlobalShaderPermutationParameters& Parameters)
	{
		return FDataDrivenShaderPlatformInfo::GetSupportsVariableRateShading(Parameters.Platform);
	}

	static void ModifyCompilationEnvironment(
		const FGlobalShaderPermutationParameters& Parameters,
		FShaderCompilerEnvironment& OutEnvironment)
	{
		FGlobalShader::ModifyCompilationEnvironment(Parameters, OutEnvironment);

		OutEnvironment.SetDefine(TEXT("SHADING_RATE_1x1"), VRSSR_1x1);
		OutEnvironment.SetDefine(TEXT("SHADING_RATE_2x2"), VRSSR_2x2);
		OutEnvironment.SetDefine(TEXT("SHADING_RATE_4x4"), VRSSR_4x4);
		OutEnvironment.SetDefine(TEXT("THREADGROUP_SIZEX"), kComputeGroupSize);
		OutEnvironment.SetDefine(TEXT("THREADGROUP_SIZEY"), kComputeGroupSize);
	}
};

IMPLEMENT_GLOBAL_SHADER(FDesktopFoveatedShader, "/Plugin/ImageBasedVRS/Private/FoveatedVRS.usf", "GenerateShadingRateTexture", SF_Compute);

void GazeImageGenerator::SetEnabled(bool bInEnabled)  { IsETFREnabled.store(bInEnabled); }
void GazeImageGenerator::SetGazeNormalized(FVector2f GazePoint)  { GazeX.store(GazePoint.X); GazeY.store(GazePoint.Y); }

FRDGTextureRef GazeImageGenerator::GetImage(
	FRDGBuilder& GraphBuilder,
	const FViewInfo& ViewInfo, 
	FVariableRateShadingImageManager::EVRSImageType ImageType)
{
	return CachedImage;
}

FRDGTextureDesc GazeImageGenerator::GetSRIDesc()
{
	const FIntPoint Size = FSceneTexturesConfig::Get().Extent;
	const FIntPoint TileSize = FIntPoint(GRHIVariableRateShadingImageTileMinWidth, GRHIVariableRateShadingImageTileMinHeight);
	const FIntPoint SRISize = FMath::DivideAndRoundUp(Size, TileSize);

	return FRDGTextureDesc::Create2D(
		SRISize,
		GRHIVariableRateShadingImageFormat,
		FClearValueBinding::None,
		TexCreate_Foveation | TexCreate_UAV | TexCreate_ShaderResource | TexCreate_DisableDCC);
}

void GazeImageGenerator::PrepareImages(
	FRDGBuilder& GraphBuilder,
	const FSceneViewFamily& ViewFamily, 
	const FMinimalSceneTextures& SceneTextures)
{
	const int VRSLevel = FMath::Clamp(CVarFoveationLevel.GetValueOnAnyThread(), 0, 3);
	if (VRSLevel <= 0 || VRSLevel > 3)
	{
		CachedImage = nullptr;
		return;
	}

	// Default to fixed center point
	float FoveationCenterX = 0.5f;
	float FoveationCenterY = 0.5f;

	// FFR mask settings
	static TArray<float> FullRateCutoffs = { 1.0f, 0.6f, 0.5f, 0.4f };
	static TArray<float> HalfRateCutoffs = { 1.0f, 0.7f, 0.6f, 0.5f };

	// If gaze data is available and gaze-tracking is enabled, adjust foveation center point
	if (IsGazeTrackingEnabled())
	{
		// Update mask center according to gaze data
		FoveationCenterX = GazeImageGenerator::GazeX;
		FoveationCenterY = GazeImageGenerator::GazeY;

		// ETFR mask settings
		FullRateCutoffs = { 1.0f,  0.6f,  0.4f, 0.20f };
		HalfRateCutoffs = { 1.0f, 0.65f, 0.45f, 0.25f };
	}

	const float FoveationFullRateCutoff = FullRateCutoffs[VRSLevel];
	const float FoveationHalfRateCutoff = HalfRateCutoffs[VRSLevel];

	// Sanity check VRS tile size
	check(GRHIVariableRateShadingImageTileMinWidth >= 8 && GRHIVariableRateShadingImageTileMinWidth <= 64 && GRHIVariableRateShadingImageTileMinHeight >= 8 && GRHIVariableRateShadingImageTileMaxHeight <= 64);

	// Create texture to hold shading rate image
	FRDGTextureDesc Desc = GazeImageGenerator::GetSRIDesc();
	FRDGTextureRef ShadingRateTexture = GraphBuilder.CreateTexture(Desc, TEXT("DesktopFoveatedVRSImage"));
	
	// Setup shader parameters and flags
	FDesktopFoveatedShader::FParameters* PassParameters =
		GraphBuilder.AllocParameters<FDesktopFoveatedShader::FParameters>();

	PassParameters->RWOutputTexture = GraphBuilder.CreateUAV(ShadingRateTexture);
	PassParameters->FoveationFullRateCutoffSquared = FoveationFullRateCutoff * FoveationFullRateCutoff;
	PassParameters->FoveationHalfRateCutoffSquared = FoveationHalfRateCutoff * FoveationHalfRateCutoff;

	// Center of the screen
	PassParameters->GazePixelXY = FVector2f(Desc.Extent.X * FoveationCenterX, Desc.Extent.Y * FoveationCenterY);

	const FVector2f ViewCenterPoint = FVector2f(Desc.Extent.X / ViewFamily.Views.Num() * 0.5f, Desc.Extent.Y * 0.5f);
	PassParameters->ViewDiagonalSquaredInPixels = FVector2f::DotProduct(ViewCenterPoint, ViewCenterPoint);

	TShaderMapRef<FDesktopFoveatedShader> ComputeShader(GetGlobalShaderMap(GMaxRHIFeatureLevel));
	FIntVector GroupCount = FComputeShaderUtils::GetGroupCount(Desc.Extent, kComputeGroupSize);

	GraphBuilder.AddPass(
		RDG_EVENT_NAME("GenerateDesktopFoveatedVRSImage"),
		PassParameters,
		ERDGPassFlags::Compute,
		[PassParameters, ComputeShader, GroupCount](FRHIComputeCommandList& RHICmdList)
		{
			FComputeShaderUtils::Dispatch(RHICmdList, ComputeShader, *PassParameters, GroupCount);
		});

	CachedImage = ShadingRateTexture;
}

bool GazeImageGenerator::IsEnabledForView(const FSceneView& View) const
{
	return CVarFoveationLevel.GetValueOnRenderThread() > 0;
}

FVariableRateShadingImageManager::EVRSSourceType GazeImageGenerator::GetType() const
{
	return IsGazeTrackingEnabled() ? FVariableRateShadingImageManager::EVRSSourceType::FixedFoveation : FVariableRateShadingImageManager::EVRSSourceType::EyeTrackedFoveation;
}

FRDGTextureRef GazeImageGenerator::GetDebugImage(
	FRDGBuilder& GraphBuilder,
	const FViewInfo& ViewInfo, 
	FVariableRateShadingImageManager::EVRSImageType ImageType)
{
	if (CVarFoveationPreview.GetValueOnRenderThread() && ImageType != FVariableRateShadingImageManager::EVRSImageType::Disabled)
	{
		return CachedImage;
	}
	return nullptr;
}

float GazeImageGenerator::UpdateDynamicVRSAmount()
{
	// We dont need dunamic update for desktop, return constant value
	return 1.0f;
}

bool GazeImageGenerator::IsGazeTrackingEnabled() const
{
	return GazeImageGenerator::IsETFREnabled;
}

#endif // PLATFORM_WINDOWS
