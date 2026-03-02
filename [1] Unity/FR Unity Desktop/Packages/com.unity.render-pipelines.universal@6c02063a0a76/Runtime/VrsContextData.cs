using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    // Shared VRS context accessible from both URP and project features
    public class VrsContextData : ContextItem
    {
        public TextureHandle shadingRateColorMask;
        public TextureHandle shadingRateImage;

        public override void Reset()
        {
            shadingRateColorMask = TextureHandle.nullHandle;
            shadingRateImage = TextureHandle.nullHandle;
        }
    }
}
