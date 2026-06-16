// Blur + затемнение для оверлея ShopPanel (URP, world-space Canvas).
// Видимость = Image.color.a (скрипт анимирует его).
// Если блюр слабый — увеличь BlurSize на материале (30-60 для VR).
Shader "Custom/ShopBlur"
{
    Properties
    {
        // Шаг между сэмплами в пикселях. В VR нужно 30-60, на мониторе — 10-20.
        _BlurSize ("Blur Step (px)",   Range(1, 80)) = 25
        _Darkness ("Darkness",         Range(0,  1)) = 0.55

        // Unity UI stencil (для корректной работы внутри Mask)
        _StencilComp    ("Stencil Comparison", Float) = 8
        _Stencil        ("Stencil ID",         Float) = 0
        _StencilOp      ("Stencil Operation",  Float) = 0
        _StencilWriteMask("Stencil Write Mask",Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask      ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "RenderType"      = "Transparent"
            "Queue"           = "Transparent"
            "RenderPipeline"  = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        ColorMask [_ColorMask]
        ZWrite Off
        Cull   Off
        ZTest  [unity_GUIZTestMode]
        Blend  SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ShopBlur"

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            CBUFFER_START(UnityPerMaterial)
                float _BlurSize;
                float _Darkness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 screenUV   : TEXCOORD0;
                float  alpha      : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.screenUV   = ComputeScreenPos(pos.positionCS);
                OUT.alpha      = IN.color.a;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 uv    = IN.screenUV.xy / IN.screenUV.w;
                // texel = шаг в UV-пространстве; _BlurSize пикселей на шаг
                float2 texel = _BlurSize / _ScreenParams.xy;

                // 5×5 Gaussian blur (25 сэмплов) — заметен даже в VR
                half3 col         = 0;
                float totalWeight = 0;

                for (int x = -2; x <= 2; x++)
                for (int y = -2; y <= 2; y++)
                {
                    // Gaussian вес: centre = 1, края ≈ 0.08
                    float w = exp(-0.5 * (x * x + y * y));
                    float2 sampleUV = saturate(uv + float2(x, y) * texel);
                    col         += SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture,
                                                      sampler_CameraOpaqueTexture,
                                                      sampleUV).rgb * w;
                    totalWeight += w;
                }
                col /= totalWeight;
                col *= 1.0 - _Darkness;

                return half4(col, IN.alpha);
            }
            ENDHLSL
        }
    }

    FallBack "UI/Default"
}
