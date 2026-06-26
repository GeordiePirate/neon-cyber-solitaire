Shader "Neon/GlowCard"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("Card Face Texture", 2D) = "white" {}
        _Color ("Card Tint", Color) = (1,1,1,1)

        [Header(Neon Glow)]
        _GlowColor ("Glow Color", Color) = (0,0.5,1,1)
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.0
        _GlowPulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _GlowWidth ("Glow Width", Range(0, 0.3)) = 0.06

        [Header(Edge Scan)]
        _ScanColor ("Scan Color", Color) = (0.2,1,0.2,1)
        _ScanSpeed ("Scan Speed", Range(0, 5)) = 0.5

        [Header(Data Fragment Effect)]
        _FragmentAlpha ("Fragment Noise", Range(0, 0.3)) = 0.05

        [Header(Blending)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
        [Enum(UnityEngine.Rendering.BlendOp)] _BlendOp ("Blend Op", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "NeonGlow"
            Tags { "LightMode"="UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            BlendOp [_BlendOp]
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldPos : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _GlowColor;
                float _GlowIntensity;
                float _GlowPulseSpeed;
                float _GlowWidth;
                float4 _ScanColor;
                float _ScanSpeed;
                float _FragmentAlpha;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color;
                OUT.worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;

                // ── Sample card face texture ─────────────────
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half4 baseColor = texColor * _Color * IN.color;

                // ── Edge glow detection ──────────────────────
                float2 uvEdge = IN.uv;
                float edgeX = min(uvEdge.x, 1.0 - uvEdge.x);
                float edgeY = min(uvEdge.y, 1.0 - uvEdge.y);
                float edgeDist = min(edgeX, edgeY);

                // Feather the glow
                float glow = 1.0 - smoothstep(0.0, _GlowWidth, edgeDist);

                // Pulsing glow
                float pulse = 0.7 + 0.3 * sin(time * _GlowPulseSpeed);
                glow *= pulse;

                // ── Travelling scan line ─────────────────────
                float scanPos = frac(time * _ScanSpeed * 0.1);
                float scanLine = 1.0 - abs(IN.uv.y - scanPos) * 10.0;
                scanLine = saturate(scanLine * 2.0);
                float scanGlow = scanLine * 0.3 * (0.5 + 0.5 * sin(time * 4.0));

                // ── Data fragment noise ──────────────────────
                float noise = sin(IN.uv.x * 137.0 + IN.uv.y * 73.0 + time * 3.0) * 0.5 + 0.5;
                float fragment = noise < _FragmentAlpha ? 0.2 : 0.0;

                // ── Combine layers ───────────────────────────
                half4 glowLayer = half4(_GlowColor.rgb * _GlowIntensity, glow * _GlowIntensity);
                half4 scanLayer = half4(_ScanColor.rgb, scanGlow);

                half4 finalColor = baseColor;
                // Additive glow on edges
                finalColor.rgb += glowLayer.rgb * glowLayer.a;
                // Scan line highlight
                finalColor.rgb += scanLayer.rgb * scanLayer.a;
                // Data fragment flicker
                finalColor.rgb += half4(1,0.5,1,1).rgb * fragment;

                // ── Final alpha: use main texture alpha or glow ──
                finalColor.a = max(baseColor.a, glowLayer.a * 0.5);

                return finalColor;
            }
            ENDHLSL
        }
    }

    FallBack "Sprites/Default"
}
