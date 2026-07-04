Shader "GameCore/UI/ScanlinePixel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)

        _PixelBlockCount ("Pixel Block Count", Range(16, 512)) = 96
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.28
        _ScanlineCount ("Scanline Count", Range(20, 400)) = 140
        _ScanBeamWidth ("Scan Beam Width", Range(0.01, 0.3)) = 0.07
        _ScanBeamBrightness ("Scan Beam Brightness", Range(0, 1)) = 0.22
        _ScanSpeed ("Scan Speed", Range(0, 2)) = 0.45
        _VignetteStrength ("Vignette Strength", Range(0, 1)) = 0.35
        _FrameStrength ("Frame Strength", Range(0, 1)) = 0.55

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
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            fixed4 _Color;
            float _PixelBlockCount;
            float _ScanlineIntensity;
            float _ScanlineCount;
            float _ScanBeamWidth;
            float _ScanBeamBrightness;
            float _ScanSpeed;
            float _VignetteStrength;
            float _FrameStrength;

            v2f vert(appdata_t input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.color = input.color * _Color;
                return output;
            }

            float getScanFrameMask(float2 uv)
            {
                float2 edge = min(uv, 1.0 - uv);
                float edgeMin = min(edge.x, edge.y);
                float border = smoothstep(0.0, 0.025, edgeMin);
                float cornerLen = 0.12;
                float cornerThick = 0.012;
                float2 cuv = abs(uv - 0.5);
                float cornerH = step(cuv.y, cornerThick) * step(cuv.x, cornerLen);
                float cornerV = step(cuv.x, cornerThick) * step(cuv.y, cornerLen);
                float corners = saturate(cornerH + cornerV);
                return saturate(border + corners * 0.85);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.texcoord;
                float blockCount = max(_PixelBlockCount, 1.0);
                float2 pixelUv = (floor(uv * blockCount) + 0.5) / blockCount;
                half4 color = (tex2D(_MainTex, pixelUv) + _TextureSampleAdd) * input.color;

                float scanPhase = frac(uv.y * _ScanlineCount);
                float scanDark = 1.0 - _ScanlineIntensity * step(0.5, scanPhase);
                color.rgb *= scanDark;

                float beamPos = frac(_Time.y * _ScanSpeed);
                float beamDist = abs(uv.y - beamPos);
                float beam = smoothstep(_ScanBeamWidth, 0.0, beamDist);
                float3 beamTint = lerp(color.rgb, color.rgb * float3(0.75, 1.15, 0.85) + _ScanBeamBrightness, beam);
                color.rgb = beamTint;

                float2 centered = uv - 0.5;
                float vignette = 1.0 - dot(centered * 1.35, centered * 1.35);
                color.rgb *= lerp(1.0, saturate(vignette), _VignetteStrength);

                float frameMask = getScanFrameMask(uv);
                color.rgb = lerp(color.rgb * (1.0 - _FrameStrength * 0.55), color.rgb, frameMask);

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}
