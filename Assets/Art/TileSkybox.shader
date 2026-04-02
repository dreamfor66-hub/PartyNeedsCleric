Shader "Custom/URP_WorldChecker_XY"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.15, 0.15, 0.15, 1)
        _ColorB ("Color B", Color) = (0.25, 0.25, 0.25, 1)
        _CellSize ("Cell Size (world units)", Float) = 1.0
    }

    SubShader
    {
        Tags{
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
        }

        Pass
        {
            Name "Checker"
            Tags{ "LightMode"="SRPDefaultUnlit" }

            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA;
                float4 _ColorB;
                float  _CellSize;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS.xyz);
                o.worldPos = ws;
                o.positionCS = TransformWorldToHClip(ws);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float cell = max(_CellSize, 0.001);

                // XY 기반 체크보드
                float2 uv = i.worldPos.xy / cell;

                int xi = (int)floor(uv.x);
                int yi = (int)floor(uv.y);

                int parity = (xi ^ yi) & 1;

                return (parity == 0) ? _ColorA : _ColorB;
            }
            ENDHLSL
        }
    }
}
