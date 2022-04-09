CBUFFER_START(UnityPerMaterial)
half _BumpScale;
CBUFFER_END

half _MaxDepth;
half _MaxWaveHeight;
half4 _VeraslWater_DepthCamParams;

// Screen Effects textures
SAMPLER(sampler_ScreenTextures_linear_clamp);

TEXTURE2D(_PlanarReflectionTexture);

TEXTURE2D(_WaterFXMap);
TEXTURE2D(_CameraDepthTexture);

TEXTURE2D(_WaterDepthMap); SAMPLER(sampler_WaterDepthMap_linear_clamp);

TEXTURE2D(_SurfaceMap); SAMPLER(sampler_SurfaceMap);