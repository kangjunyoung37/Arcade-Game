Shader "Hidden/Game/Vision/GaussianBlur"
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

        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        CBUFFER_START(UnityPerMaterial)
            // xy: 원본 텍스처의 픽셀 크기
            // zw: 중간 텍스처의 픽셀 크기
            float4 _PixelSize;

            float _CenterWeight;
            int _PairCount;

            // x: 샘플 위치, y: 합쳐진 가중치
            float4 _Pairs[24];
        CBUFFER_END

        half ReadMask(float2 uv)
        {
            return SAMPLE_TEXTURE2D_X(
                _BlitTexture,
                sampler_LinearClamp,
                uv).r;
        }

        half4 Blur(float2 uv, float2 axis)
        {
            float result = ReadMask(uv) * _CenterWeight;

            [loop]
            for (int i = 0; i < _PairCount; i++)
            {
                float2 offset = axis * _Pairs[i].x;
                float weight = _Pairs[i].y;

                result += ReadMask(uv + offset) * weight;
                result += ReadMask(uv - offset) * weight;
            }

            half mask = (half)saturate(result);

            return half4(mask, mask, mask, 1.0);
        }

        half4 FragHorizontal(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            return Blur(
                input.texcoord,
                float2(_PixelSize.x, 0.0));
        }

        half4 FragVertical(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            return Blur(
                input.texcoord,
                float2(0.0, _PixelSize.w));
        }

        ENDHLSL

        Pass
        {
            Name "Horizontal"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragHorizontal
            ENDHLSL
        }

        Pass
        {
            Name "Vertical"

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment FragVertical
            ENDHLSL
        }
    }

    Fallback Off
}