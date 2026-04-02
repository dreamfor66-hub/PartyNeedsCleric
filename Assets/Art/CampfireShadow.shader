// UI/ShadowAutoFromCanvasCenter.shader
Shader "UI/ShadowAutoFromCanvasCenter"
{
    Properties
    {
        [PerRendererData]_MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 화면 정규화 좌표(0~1). 기본은 화면/캔버스 정중앙.
        _CanvasCenter01 ("Canvas Center (0..1)", Vector) = (0.5, 0.5, 0, 0)

        // 그림자 아트가 기본적으로 "위(+Y)"로 늘어나는 형태라고 가정.
        // 아트가 다르면 여기로 보정(도 단위)
        _AngleOffsetDeg ("Angle Offset (deg)", Float) = 0

        // (선택) 눕혀진 느낌
        _ShearX ("Shear X", Float) = 0.0

        // UI 기본
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            sampler2D _MainTex;
            fixed4 _Color;

            float4 _CanvasCenter01;
            float _AngleOffsetDeg;
            float _ShearX;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            v2f vert(appdata_t v)
            {
                v2f o;

                // 이 드로우콜(해당 Image)의 "피벗(로컬 0,0)" 위치를 화면좌표로 계산
                float4 pivotClip = UnityObjectToClipPos(float4(0,0,0,1));
                float4 pivotSP   = ComputeScreenPos(pivotClip);
                float2 pivot01   = pivotSP.xy / pivotSP.w;           // 0..1

                // 캔버스 중앙(= 화면 중앙)에서 바깥으로 향하는 방향
                float2 dir = pivot01 - _CanvasCenter01.xy;
                float len = max(length(dir), 1e-6);
                dir /= len;

                // dir을 기준으로 그림자의 "위(+Y)"가 dir을 향하도록 회전
                // atan2 기준이 x축이므로 -90도(PI/2) 보정
                float ang = atan2(dir.y, dir.x) - 1.57079632679;
                ang += radians(_AngleOffsetDeg);

                // 버텍스 변형: UV는 그대로, 쿼드 자체를 회전/쉬어 => 잘림 문제 없음
                float2 p = v.vertex.xy;          // RectTransform 피벗 기준 로컬 좌표
                p.x += _ShearX * p.y;

                float s, c;
                sincos(ang, s, c);
                float2 rp = float2(p.x * c - p.y * s, p.x * s + p.y * c);

                float4 vtx = v.vertex;
                vtx.xy = rp;

                o.worldPos = vtx;
                o.vertex = UnityObjectToClipPos(vtx);

                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
