Shader "Game/Vision/SoftVisionOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Vision Mask", 2D) = "black" {}

        _FogColor ("Fog Color", Color) = (0, 0, 0, 1)

        _OutsideOpacity ("Outside Darkness", Range(0, 1)) = 0.7
        _InsideOpacity ("Inside Darkness", Range(0, 1)) = 0.05

        _BlurStepPixels ("Softness - Mask Pixels", Range(0, 4)) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "CanUseSpriteAtlas" = "False"
        }

        Pass
        {
            Name "VisionOverlay"

            Cull Off
            ZWrite Off
            ZTest [unity_GUIZTestMode]
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_TexelSize;
                half4 _FogColor;

                float _OutsideOpacity;
                float _InsideOpacity;
                float _BlurStepPixels;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                output.positionCS =
                    TransformObjectToHClip(input.positionOS.xyz);

                output.uv = input.uv;
                output.color = input.color;

                return output;
            }

            half ReadMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D(
                    _MainTex,
                    sampler_MainTex,
                    saturate(uv)).r;
            }

            // 5点の重み: 1, 4, 6, 4, 1
            float GaussianWeight(int offset)
            {
                int distance = abs(offset);

                if (distance == 0)
                    return 6.0;

                if (distance == 1)
                    return 4.0;

                return 1.0;
            }

            half SampleSoftMask(float2 uv)
            {
                if (_BlurStepPixels <= 0.0)
                    return ReadMask(uv);

                float2 sampleStep =
                    abs(_MainTex_TexelSize.xy) * _BlurStepPixels;

                float total = 0.0;

                [unroll]
                for (int y = -2; y <= 2; y++)
                {
                    float weightY = GaussianWeight(y);

                    [unroll]
                    for (int x = -2; x <= 2; x++)
                    {
                        float weight =
                            GaussianWeight(x) * weightY;

                        float2 offset = float2(x, y) * sampleStep;

                        total += ReadMask(uv + offset) * weight;
                    }
                }

                // (1 + 4 + 6 + 4 + 1)^2 = 256
                return (half)saturate(total / 256.0);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half visibility = SampleSoftMask(input.uv);

                half opacity = (half)lerp(
                    _OutsideOpacity,
                    _InsideOpacity,
                    visibility);

                half3 color = _FogColor.rgb * input.color.rgb;

                opacity *= _FogColor.a * input.color.a;

                return half4(color, saturate(opacity));
            }

            ENDHLSL
        }
    }

    Fallback Off
}