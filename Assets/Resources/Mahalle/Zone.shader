Shader "MISKETR/Zone"
{
    // HARİTA 2 zemin bölgeleri: yere yatırılmış bir Quad üstünde yumuşak kenarlı daire.
    //   _Kind 0 = kum (açık, tanecikli), 1 = çamur (koyu, ıslak parlama), 2 = çukur (koyu delik + kenar)
    //   _Kind 3 = rögar kapağı (Harita 3: düzgün kenarlı metal disk, halka + ızgara çizgileri)
    //   _Kind 4 = yürüyen bant (Harita 4: koyu lastik disk, +x yönünde kayan sarı oklar; quad bandın yönüne döndürülür)
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
                if (_Kind > 2.5) wobble = 0.0;
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
                else if (_Kind > 3.5)
                {
                    // Yürüyen bant: lastik disk, çerçeve, +x yönünde akan ok (chevron) sırası.
                    float frame = smoothstep(0.82, 0.86, r) * (1.0 - smoothstep(0.93, 0.97, r));
                    float u = d.x * 2.2 - _Time.y * 1.6;
                    // Ok ucu +x (bandın yönü) tarafında: |y| büyüdükçe faz artar → ">" (2026-10-21: "<" çiziliyordu, ters).
                    float chev = frac(u + abs(d.y) * 0.9);
                    float arrow = (1.0 - smoothstep(0.0, 0.08, abs(chev - 0.5) - 0.12)) * step(abs(d.y), 0.62) * step(r, 0.8);
                    float ribs = 0.92 + 0.08 * step(0.5, frac(d.y * 6.0));
                    c.rgb *= ribs;
                    c.rgb = lerp(c.rgb, c.rgb * 2.2, frame);
                    c.rgb = lerp(c.rgb, half3(0.98, 0.72, 0.22), arrow * 0.9);
                }
                else if (_Kind > 2.5)
                {
                    // Rögar kapağı: dış çerçeve açık, içte iki halka ve ızgara çizgileri, ortada parlama.
                    float frame = smoothstep(0.80, 0.84, r) * (1.0 - smoothstep(0.92, 0.96, r));
                    float ring = 1.0 - smoothstep(0.0, 0.025, abs(r - 0.55));
                    float2 g = abs(frac(d * 3.2) - 0.5);
                    float grid = (1.0 - smoothstep(0.0, 0.06, min(g.x, g.y))) * step(r, 0.78) * step(0.6, r);
                    float shine = 1.0 - smoothstep(0.0, 0.7, length(d - float2(-0.35, 0.35)));
                    c.rgb *= 0.85 + shine * 0.35;
                    c.rgb = lerp(c.rgb, c.rgb * 1.6, frame);
                    c.rgb = lerp(c.rgb, c.rgb * 0.55, max(ring, grid));
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
