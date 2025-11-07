Shader "UI/LiquidFill"
{
    Properties
    {
        _Color          ("Tint", Color) = (1,1,1,1)
        _Fill           ("Fill (0-1)", Range(0,1)) = 1
        _Amplitude      ("Wave Amplitude", Range(0,0.2)) = 0.05
        _Frequency      ("Wave Frequency", Range(0,20)) = 8
        _Speed          ("Wave Speed", Range(-10,10)) = 1.5
        _EdgeSoftness   ("Edge Softness", Range(0,0.2)) = 0.02
        _NoiseTex       ("Noise (optional)", 2D) = "gray" {}
        _NoiseStrength  ("Noise Strength", Range(0,0.1)) = 0.02

        // Unity UI standard props (do not remove)
        _MainTex        ("Sprite", 2D) = "white" {}
        _StencilComp    ("Stencil Comparison", Float) = 8
        _Stencil        ("Stencil ID", Float) = 0
        _StencilOp      ("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255
        _ColorMask      ("Color Mask", Float) = 15
        _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        _ClipRect       ("Clip Rect", Vector) = ( -32767, -32767, 32767, 32767 )
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
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
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _NoiseTex;
            float4 _ClipRect;

            fixed4 _Color;
            float _Fill;
            float _Amplitude;
            float _Frequency;
            float _Speed;
            float _EdgeSoftness;
            float _NoiseStrength;
            float _UseUIAlphaClip;

            struct appdata
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0; // local UV (0..1)
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 pos      : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                fixed4 color    : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.worldPos = v.vertex;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // base UV in 0..1 of the Image rect
                float2 uv = i.uv;

                // time-based horizontal wave
                float t = _Time.y * _Speed;
                float sine = sin((uv.x + t) * _Frequency) * _Amplitude;

                // subtle noise to break uniformity (optional)
                float n = tex2D(_NoiseTex, uv * 4.0 + float2(t*0.2, 0)).r; // 4x tiling
                float noise = (n - 0.5) * 2.0 * _NoiseStrength;

                // total surface height
                float surface = saturate(_Fill + sine + noise);

                // alpha = 1 below surface, 0 above (soft edge)
                float a = smoothstep(surface, surface - _EdgeSoftness, uv.y);

                fixed4 col = _Color;
                col.a *= a;

                // Unity UI clipping/masking
                #ifdef UNITY_UI_CLIP_RECT
                    col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip (col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
