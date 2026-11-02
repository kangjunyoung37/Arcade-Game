Shader "Game/Vision/GradientMask"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "VisionMask"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Off
            ZWrite Off
            ZTest Always

            // 겹치는 시야는 더하지 않고 하나의 영역으로 합칩니다.
            Blend One One
            BlendOp Max
            ColorMask RGB

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // 시야 내부는 거리에 관계없이 동일한 값입니다.
                // 시야 모양과 장애물 차단은 기존 메시가 결정합니다.
                return half4(1.0, 1.0, 1.0, 1.0);
            }

            ENDHLSL
        }
    }

    Fallback Off
}