Shader "Water/Water"
{
    Properties
    {
        _BumpScale("Detail Wave Amount", Range(0, 2)) = 0.2
        [KeywordEnum(Off, Reflection, Normal)] _Debug ("Debug mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-100" "RenderPipeline" = "UniversalPipeline" }
        ZWrite On

        Pass
        {
            Name "WaterShading"
            Tags {"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM

            //GLES 사용
            #pragma prefer_hlslcc gles

            //Debug modes
            #pragma shader_feature _DEBUG_OFF _DEBUG_REFLECTION _DEBUG_NORMAL

            //Light Kewords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            
            #pragma multi_compile_instancing

            #include "WaterCommon.hlsl"

			//non-tess
			#pragma vertex WaterVertex
			#pragma fragment WaterFragment
            
            ENDHLSL
        }
    }
    FallBack "Hidden/InternalErrorShader"
}
