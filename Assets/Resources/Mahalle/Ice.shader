Shader "MISKETR/Ice"
{
    // BUZLU MİSKET kabuğu: yarı saydam açık mavi, kenarları daha parlak (fresnel).
    // Build'e girsin diye Resources içinde; URP anahtar kelimelerine ihtiyaç duymaz.
    Properties
    {
        _BaseColor ("Renk", Color) = (0.72, 0.9, 1.0, 0.32)
        _RimColor ("Kenar", Color) = (0.93, 0.98, 1.0, 0.85)
        _Crack ("Catlak", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half4 _RimColor;
            half _Crack;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 viewWS : TEXCOORD1; float3 posOS : TEXCOORD2; };

            Varyings vert(Attributes input)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(ws);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.viewWS = GetWorldSpaceViewDir(ws);
                o.posOS = input.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                half rim = pow(1.0 - saturate(dot(n, v)), 2.2);
                // Üstten vuran küçük parlama: buz gibi dursun, cam misketle karışmasın.
                half glint = pow(saturate(dot(n, normalize(float3(-0.35, 0.9, 0.25)))), 24.0) * 0.8;
                half4 c = lerp(_BaseColor, _RimColor, rim);
                c.rgb += glint;
                c.a = saturate(c.a + glint * 0.5);
                // Kırılmaya yakın çatlak çizgileri (zayıf darbede kısa süre görünür).
                half line1 = step(0.965, abs(sin(i.posOS.x * 21.0 + i.posOS.y * 13.0)));
                half line2 = step(0.97, abs(sin(i.posOS.z * 17.0 - i.posOS.y * 11.0)));
                half crack = saturate(line1 + line2) * _Crack;
                c.rgb = lerp(c.rgb, half3(1, 1, 1), crack);
                c.a = saturate(c.a + crack * 0.6);
                return c;
            }
            ENDHLSL
        }
    }
}
