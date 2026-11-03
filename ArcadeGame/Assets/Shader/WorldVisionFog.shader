Shader "Hidden/Game/Vision/WorldFog"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        ZWrite Off
        ZTest Always
        Blend Off

        Pass
        {
            Name "WorldVisionFog"

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_VisionMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _WorldToMaskUV;
                half4 _FogColor;
                float _OutsideDarkness;
                float _InsideDarkness;
            CBUFFER_END

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 sceneColor = SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_LinearClamp,
                    input.texcoord);

                float2 screenUV =
                    input.positionCS.xy / _ScaledScreenParams.xy;

                float rawDepth = SampleSceneDepth(screenUV);

                #if UNITY_REVERSED_Z
                    if (rawDepth <= 0.00001)
                        return sceneColor;

                    float deviceDepth = rawDepth;
                #else
                    if (rawDepth >= 0.99999)
                        return sceneColor;

                    float deviceDepth = lerp(
                        UNITY_NEAR_CLIP_VALUE,
                        1.0,
                        rawDepth);
                #endif

                float3 worldPosition = ComputeWorldSpacePosition(
                    screenUV,
                    deviceDepth,
                    UNITY_MATRIX_I_VP);

                float2 maskUV =
                    worldPosition.xz * _WorldToMaskUV.xy
                    + _WorldToMaskUV.zw;

                float insideTexture =
                    step(0.0, maskUV.x) *
                    step(maskUV.x, 1.0) *
                    step(0.0, maskUV.y) *
                    step(maskUV.y, 1.0);

                half visibility = SAMPLE_TEXTURE2D(
                    _VisionMask,
                    sampler_LinearClamp,
                    saturate(maskUV)).r;

                visibility *= (half)insideTexture;

                half darkness = (half)lerp(
                    _OutsideDarkness,
                    _InsideDarkness,
                    saturate(visibility));

                darkness = saturate(darkness * _FogColor.a);

                sceneColor.rgb = lerp(
                    sceneColor.rgb,
                    _FogColor.rgb,
                    darkness);

                return sceneColor;
            }

            ENDHLSL
        }
    }

    Fallback Off
}