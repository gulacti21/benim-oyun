Shader "MISKETR/Zone"
{
    // HARİTA 2 zemin bölgeleri: yere yatırılmış bir Quad üstünde yumuşak kenarlı daire.
    //   _Kind 0 = kum (açık, tanecikli), 1 = çamur (koyu, ıslak parlama), 2 = çukur (koyu delik + kenar)
    Properties
    {
        _Color ("Renk", Color) = (0.86, 0.76, 0.55, 0.85)
        _Kind ("Tür", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-10" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _Kind;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = input.uv;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }

            half4 frag(Varyings i) : SV_Target
            {
                float2 d = (i.uv - 0.5) * 2.0;
                float r = length(d);
                // Hafif dalgalı kenar: elle dökülmüş kum/çamur gibi dursun.
                float wobble = sin(atan2(d.y, d.x) * 7.0) * 0.025 + sin(atan2(d.y, d.x) * 13.0) * 0.015;
                float edge = 1.0 - smoothstep(0.86 + wobble, 1.0 + wobble, r);
                half4 c = _Color;
                if (_Kind < 0.5)
                {
                    float grain = hash(floor(i.uv * 90.0));
                    c.rgb *= 0.9 + grain * 0.2;
                }
                else if (_Kind < 1.5)
                {
                    // Islak parlama: sol üstte yumuşak bir leke.
                    float wet = 1.0 - smoothstep(0.0, 0.55, length(d - float2(-0.3, 0.3)));
                    c.rgb += wet * 0.12;
                }
                else
                {
                    // Çukur: ortası koyu, kenarda açık toprak halkası.
                    float hole = smoothstep(0.55, 0.78, r);
                    c.rgb = lerp(c.rgb * 0.25, c.rgb * 1.25, hole);
                    c.a = lerp(0.95, c.a, hole);
                }
                c.a *= edge;
                return c;
            }
            ENDHLSL
        }
    }
}
