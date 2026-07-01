// Full-screen fade overlay. Works in both URP and BIRP.
// CameraFader.cs drives _Color.a (0=transparent, 1=opaque black).
// Migrated from the package's built-in-only Fader.shader; see VR2Gather issue #333.
Shader "VRT/Fader"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0,1)
        // Unused by the shader body, but required so Unity's UI system (Image/CanvasRenderer)
        // has a texture property to bind to; without it every fade logs "doesn't have a texture
        // property '_MainTex'" in built players. See VRTApp-Trolley#80.
        _MainTex ("Texture", 2D) = "white" {}
    }

    // ── URP ──────────────────────────────────────────────────────────────────
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings   { float4 positionHCS : SV_POSITION; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return half4(_Color); }
            ENDHLSL
        }
    }

    // ── BIRP (fallback) ───────────────────────────────────────────────────────
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color;

            struct appdata { float4 vertex : POSITION; };
            struct v2f     { float4 vertex : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target { return _Color; }
            ENDCG
        }
    }
}
