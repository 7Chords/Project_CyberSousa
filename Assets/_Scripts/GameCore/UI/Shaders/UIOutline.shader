Shader "GameCore/UI/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth ("Outline Width", Range(0, 8)) = 0

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
            fixed4 _OutlineColor;
            float _OutlineWidth;

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

            fixed sampleSilhouetteAlpha(float2 uv)
            {
                return (tex2D(_MainTex, uv) + _TextureSampleAdd).a;
            }

            #define SILHOUETTE_ALPHA_THRESHOLD 0.01

            fixed getDistanceToSilhouetteEdge(float2 uv, float maxWidth, float2 texel)
            {
                fixed edgeDist = maxWidth + 1.0;

                [unroll]
                for (int step = 1; step <= 8; step++)
                {
                    float s = (float)step;
                    if (s > maxWidth)
                        continue;

                    float2 offset = texel * s;
                    if (sampleSilhouetteAlpha(uv + float2(offset.x, 0)) < SILHOUETTE_ALPHA_THRESHOLD) edgeDist = min(edgeDist, s);
                    if (sampleSilhouetteAlpha(uv + float2(-offset.x, 0)) < SILHOUETTE_ALPHA_THRESHOLD) edgeDist = min(edgeDist, s);
                    if (sampleSilhouetteAlpha(uv + float2(0, offset.y)) < SILHOUETTE_ALPHA_THRESHOLD) edgeDist = min(edgeDist, s);
                    if (sampleSilhouetteAlpha(uv + float2(0, -offset.y)) < SILHOUETTE_ALPHA_THRESHOLD) edgeDist = min(edgeDist, s);
                    if (sampleSilhouetteAlpha(uv + float2(offset.x, offset.y)) < SILHOUETTE_ALPHA_THRESHOLD) edgeDist = min(edgeDist, s);
                    if (sampleSilhouetteAlpha(uv + float2(-offset.x, offset.y)) < SILHOUETTE_ALPHA_THRESHOLD) edgeDist = min(edgeDist, s);
                    if (sampleSilhouetteAlpha(uv + float2(offset.x, -offset.y)) < SILHOUETTE_ALPHA_THRESHOLD) edgeDist = min(edgeDist, s);
                    if (sampleSilhouetteAlpha(uv + float2(-offset.x, -offset.y)) < SILHOUETTE_ALPHA_THRESHOLD) edgeDist = min(edgeDist, s);
                }

                return edgeDist;
            }

            fixed getInnerOutlineBlend(float2 uv, fixed silhouetteAlpha, float maxWidth)
            {
                if (maxWidth <= 0.001 || silhouetteAlpha < SILHOUETTE_ALPHA_THRESHOLD)
                    return 0;

                float2 texel = _MainTex_TexelSize.xy;
                fixed edgeDist = getDistanceToSilhouetteEdge(uv, maxWidth, texel);
                if (edgeDist > maxWidth)
                    return 0;

                return saturate((maxWidth - edgeDist + 1.0) / maxWidth);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, input.texcoord) + _TextureSampleAdd;
                half4 color = texColor * input.color;
                fixed silhouetteAlpha = texColor.a;

                half4 result = color;
                if (_OutlineWidth > 0.001)
                {
                    fixed outlineBlend = getInnerOutlineBlend(input.texcoord, silhouetteAlpha, _OutlineWidth);
                    if (outlineBlend > 0.001)
                        result.rgb = lerp(color.rgb, _OutlineColor.rgb, outlineBlend);
                }

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
