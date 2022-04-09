#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "WaterInput.hlsl"
#include "WaterLighting.hlsl"
#include "GerstnerWaves.hlsl"

///////////////////////////////////////////////////////////////////////////////
//                  				Structs		                             //
///////////////////////////////////////////////////////////////////////////////

struct WaterVertexInput // vert struct
{
    float4	vertex 					: POSITION;		// vertex positions
    float2	texCoord 				: TEXCOORD0;	// local UVs
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct WaterVertexOutput // fragment struct
{
    float4	uv 						: TEXCOORD0;	// Geometric UVs stored in xy, and world(pre-waves) in zw
    float3	posWS					: TEXCOORD1;	// world position of the vertices
    half3 	normal 					: NORMAL;		// vert normals
    float3 	viewDir 				: TEXCOORD2;	// view direction
    float4	additionalData			: TEXCOORD5;	// x = distance to surface, y = distance to surface, z = normalized wave height, w = horizontal movement
    float4	shadowCoord				: TEXCOORD6;	// for ssshadows
    float4	clipPos					: SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float WaterTextureDepth(float3 posWS)
{
	return (1 - SAMPLE_TEXTURE2D_LOD(_WaterDepthMap, sampler_WaterDepthMap_linear_clamp, posWS.xz * 0.002 + 0.5, 1).r) * (_MaxDepth + _VeraslWater_DepthCamParams.x) - _VeraslWater_DepthCamParams.x;
}

float4 AdditionalData(float3 postionWS, WaveStruct wave)
{
	float4 data = float4(0, 0, 0, 0);
	float3 viewPos = TransformWorldToView(postionWS);
	data.x = length(viewPos / viewPos.z);// distance to surface
	data.y = length(GetCameraPositionWS().xyz - postionWS); // local position in camera space
	data.z = wave.position.y / _MaxWaveHeight * 0.5 + 0.5; // encode the normalized wave height into additional data
	data.w = wave.position.x + wave.position.z;
	return data;
}

WaterVertexOutput WaveVertexOperations(WaterVertexOutput input)
{
	float time = _Time.y;

	half4 screenUV = ComputeScreenPos(TransformWorldToHClip(input.posWS));
	screenUV.xyz /= screenUV.w;
	
	//Detail UVs
	input.uv.zw = input.posWS.xz * 0.1h + time * 0.05h;
	input.uv.xy = input.posWS.xz * 0.4h - time.xx * 0.1h;

	//Dynamic displacement
	half4 waterFX = SAMPLE_TEXTURE2D_LOD(_WaterFXMap, sampler_ScreenTextures_linear_clamp, screenUV.xy, 0);
	input.posWS.y += waterFX.w * 2 - 1;
	
	//Gerstner here
	WaveStruct wave;
	SampleWaves(input.posWS, 0.05, wave);
	input.normal = wave.normal;
	input.posWS += wave.position;

	//After waves
	input.clipPos = TransformWorldToHClip(input.posWS);
	input.shadowCoord = ComputeScreenPos(input.clipPos);
	input.viewDir = SafeNormalize(_WorldSpaceCameraPos - input.posWS);

	//Additional data
	input.additionalData = AdditionalData(input.posWS, wave);
	
	return input;
}

///////////////////////////////////////////////////////////////////////////////
//               	   Vertex and Fragment functions                         //
///////////////////////////////////////////////////////////////////////////////

WaterVertexOutput WaterVertex(WaterVertexInput v)
{
    WaterVertexOutput o;
	UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv.xy = v.texCoord; // geo uvs
    o.posWS = TransformObjectToWorld(v.vertex.xyz);

	o = WaveVertexOperations(o);
    return o;
}

float4 WaterFragment(WaterVertexOutput input) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(IN);
	half3 screenUV = input.shadowCoord.xyz / input.shadowCoord.w;

    //Detail waves
	float2 detailBump1 = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, input.uv.zw).xy * 2 - 1;
	float2 detailBump2 = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, input.uv.xy).xy * 2 - 1;
	float2 detailBump = (detailBump1 + detailBump2 * 0.5);

	input.normal += float3(detailBump.x, 0, detailBump.y) * _BumpScale;
	input.normal = normalize(input.normal);

	//Reflection
	float3 reflection = SampleReflections(input.normal, screenUV.xy, 0);

#if defined(_DEBUG_REFLECTION)
	return float4(reflection, 1);
#elif defined(_DEBUG_NORMAL)
	return float4(input.normal.x * 0.5 + 0.5f, 0, input.normal.z * 0.5 + 0.5f, 1);
#else
	return float4(0, 0, 0, 1);
#endif
}
