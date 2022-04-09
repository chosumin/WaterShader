#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float CalculateFresnelTerm(float3 normalWS, float3 viewDirectionWS)
{
    return saturate(pow(1.0 - dot(normalWS, viewDirectionWS), 5));
}

float3 SampleReflections(float3 normalWS, float2 screenUV, float roughness)
{
    float3 reflection = 0;

    //planar reflection
    float2 reflectionUV = screenUV + normalWS.zx * float2(0.02, 0.15);
    reflection += SAMPLE_TEXTURE2D_LOD(_PlanarReflectionTexture, sampler_ScreenTextures_linear_clamp, reflectionUV, 6 * roughness).rgb;

    return reflection;
}