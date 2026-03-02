using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class FoveatedVrsFeature : ScriptableRendererFeature
{
    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    [Header("Foveated Rendering")]
    public int startFRLevel = 3;
    public bool ETFREnabled = true;
    
    [Header("Mask")]
    public bool maskEnabled = true;

    [Header("Gaze")]
    public bool gazeEnabled = true;
    public static Vector2 currentGazeUV = new Vector2(0.5f, 0.5f);

    private readonly float[] FullRateCutoffs = { 5.0f, 0.30f, 0.25f, 0.20f };
    private readonly float[] HalfRateCutoffs = { 5.0f, 0.35f, 0.30f, 0.25f };
    private readonly int minLevel = 0;
    private readonly int maxLevel = 3;

    private VRSGenerationPass m_ScriptablePass;
    private VRSDebugPass m_DebugPass;

    public override void Create()
    {
        GazeMarker.gazeEnabled = gazeEnabled;
        GazeMarker.etfrEnabled = ETFREnabled;

        m_ScriptablePass = new VRSGenerationPass();
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        m_ScriptablePass.SetupFoveated(ETFREnabled, startFRLevel, FullRateCutoffs, HalfRateCutoffs);

        if (maskEnabled)
        {
            m_DebugPass = new VRSDebugPass();
            m_DebugPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Do not run VRS passes for Scene view camera
        if (renderingData.cameraData.isSceneViewCamera) return;

        renderer.EnqueuePass(m_ScriptablePass);
        if (maskEnabled) renderer.EnqueuePass(m_DebugPass);
    }

    public void ToggleMethod()
    {
        m_ScriptablePass.etfrEnabled = !m_ScriptablePass.etfrEnabled;
        if (debugLog) Debug.Log("ETFR Enabled: " + m_ScriptablePass.etfrEnabled);
    }
    public void IncreaseLevel()
    {
        if (m_ScriptablePass.currentLevel >= maxLevel) return;
        m_ScriptablePass.currentLevel++;
        if (debugLog) Debug.Log("FR Level: " + m_ScriptablePass.currentLevel);
    }
    public void DecreaseLevel()
    {
        if (m_ScriptablePass.currentLevel <= minLevel) return;
        m_ScriptablePass.currentLevel--;
        if (debugLog) Debug.Log("FR Level: " + m_ScriptablePass.currentLevel);
    }
    public void ToggleMask()
    {
        if (maskEnabled = !maskEnabled)
        {
            m_DebugPass = new VRSDebugPass();
            m_DebugPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        }
        else
        {
            m_DebugPass = null;
        }

        if (debugLog) Debug.Log("HUD: " + maskEnabled);
    }

    private class VRSGenerationPass : ScriptableRenderPass
    {
        private TextureHandle m_SRIColorMask;
        private TextureHandle m_SRI;
        private Material m_Material;

        public bool etfrEnabled;
        public int currentLevel;
        private float[] m_FullRateCutoffs;
        private float[] m_HalfRateCutoffs;

        public void SetupFoveated(bool ETFREnabled, int CurrentLevel, float[] FullRateCutoffs, float[] HalfRateCutoffs)
        {
            currentLevel = CurrentLevel;
            etfrEnabled = ETFREnabled;
            m_FullRateCutoffs = FullRateCutoffs;
            m_HalfRateCutoffs = HalfRateCutoffs;
        }

        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        private class PassData
        {
            public Material m_Mat;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "VRS Generation";

            if (!ShadingRateInfo.supportsPerImageTile)
            {
                Debug.Log("VRS is not supported!");
                return;
            }

            VrsLut lut = new VrsLut();
            var vrsPipelineResources = GraphicsSettings.GetRenderPipelineSettings<VrsRenderPipelineRuntimeResources>();
            lut = vrsPipelineResources.conversionLookupTable;

            if (m_Material == null)
            {
                m_Material = new Material(Resources.Load<Shader>("Shaders/GenerateVRS"));
                m_Material.SetColor("_ShadingRateColor1x1", lut[ShadingRateFragmentSize.FragmentSize1x1]);
                m_Material.SetColor("_ShadingRateColor2x2", lut[ShadingRateFragmentSize.FragmentSize2x2]);
                m_Material.SetColor("_ShadingRateColor4x4", lut[ShadingRateFragmentSize.FragmentSize4x4]);
            }

            // Update fovea parameters every frame
            if (etfrEnabled)
            {
                m_Material.EnableKeyword("FOVEATED_ON");
                m_Material.SetVector("_FoveaUV", FoveatedVrsFeature.currentGazeUV);
                m_Material.SetFloat("_InnerRadius", m_FullRateCutoffs[currentLevel]);
                m_Material.SetFloat("_MiddleRadius", m_HalfRateCutoffs[currentLevel]);
            }
            else
            {
                m_Material.DisableKeyword("FOVEATED_ON");
                m_Material.SetVector("_FoveaUV", new Vector2(0.5f, 0.5f));
                m_Material.SetFloat("_InnerRadius", m_FullRateCutoffs[currentLevel]);
                m_Material.SetFloat("_MiddleRadius", m_HalfRateCutoffs[currentLevel]);
            }

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var vrsData = frameData.Create<VrsContextData>();
            var tileSize = ShadingRateImage.GetAllocTileSize(
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                builder.AllowPassCulling(false);

                RenderTextureDescriptor textureProperties =
                    new RenderTextureDescriptor(tileSize.x, tileSize.y, RenderTextureFormat.Default, 0);
                m_SRIColorMask = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, textureProperties, "_ShadingRateColor", false);

                builder.SetRenderAttachment(m_SRIColorMask, 0, AccessFlags.Write);
                vrsData.shadingRateColorMask = m_SRIColorMask;

                passData.m_Mat = m_Material;

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    RasterCommandBuffer cmd = context.cmd;
                    Blitter.BlitTexture(cmd, new Vector4(1, 1, 0, 0), data.m_Mat, 0);
                });

                RenderTextureDescriptor sriDesc =
                    new RenderTextureDescriptor(tileSize.x, tileSize.y, ShadingRateInfo.graphicsFormat, GraphicsFormat.None);
                sriDesc.enableRandomWrite = true;
                sriDesc.enableShadingRate = true;
                sriDesc.autoGenerateMips = false;

                m_SRI = UniversalRenderer.CreateRenderGraphTexture(renderGraph, sriDesc, "_SRI", false);
            }

            Vrs.ColorMaskTextureToShadingRateImage(renderGraph, m_SRI, m_SRIColorMask, TextureDimension.Tex2D, true);
            vrsData.shadingRateImage = m_SRI;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }
    }

    private class VRSDebugPass : ScriptableRenderPass
    {
        private Material m_Material;
        private RenderPassEvent m_Event;
        private TextureHandle m_SRIColorMask;

        // This class stores the data needed by the RenderGraph pass.
        // It is passed as a parameter to the delegate function that executes the RenderGraph pass.
        private class PassData
        {
            public Material m_Mat;
            public TextureHandle m_Tex;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "VRS Debugging";

            if (!ShadingRateInfo.supportsPerImageTile) return;

            if (m_Material == null)
                m_Material = new Material(Resources.Load<Shader>("Shaders/DebugVRS"));

            var vrsData = frameData.Get<VrsContextData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData))
            {
                builder.AllowPassCulling(false);
                passData.m_Tex = vrsData.shadingRateColorMask;

                builder.UseTexture(passData.m_Tex, AccessFlags.Read);
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                passData.m_Mat = m_Material;

                //Blit
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    RasterCommandBuffer cmd = context.cmd;
                    Blitter.BlitTexture(cmd, passData.m_Tex, new Vector4(1, 1, 0, 0), data.m_Mat, 0);
                });
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
        }
    }

}