Shader "MISKETR/SwirledGlass"
{
 Properties { _BaseColor("Glass tint", Color) = (.1,.7,.7,1) _SwirlColor("Inner ribbon", Color) = (1,.85,.3,1) _Matcap("Glass matcap", 2D) = "white" {} }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass
  {
   Tags { "LightMode"="UniversalForward" }
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   TEXTURE2D(_Matcap); SAMPLER(sampler_Matcap);
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor; half4 _SwirlColor;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; float2 uv:TEXCOORD2; float3 normalOS:TEXCOORD3; };
   Varyings vert(Attributes v)
   { Varyings o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.uv=v.uv; o.normalOS=v.normalOS; return o; }
   half4 frag(Varyings i):SV_Target
   {
    float3 n=normalize(i.normalWS);
    float3 tint=_BaseColor.rgb;

    // MATCAP: camin malzemesi gercek bir cam kure goruntusunden okunuyor.
    float3 nVS=mul((float3x3)UNITY_MATRIX_V,n);
    float2 mcUV=normalize(nVS).xy*.5+.5;
    float cam=SAMPLE_TEXTURE2D(_Matcap,sampler_Matcap,mcUV).r;

    // KURDELE (kedi gozu). Object space: misket yuvarlandikca kurdele de doner.
    float3 q=normalize(i.normalOS);
    // Kurdele kurenin ekvatorunda bir kusak. Kamera tepeden baktigi icin
    // kaldirir; kutup da ekranin ortasina denk geldigi icin kurdele ortaya gelir.
    // 0 = kenarda (eski hal), .9 = tam ortada, 1'e yaklastikca kucuk bir leke.
    float egri=q.y - .46*sin(q.x*2.6) - .14*q.z;
    float kurdele=smoothstep(.36,.05,abs(egri));
    kurdele*=smoothstep(.05,.45,abs(q.z)+.35);

    float3 govde=tint*(.22+cam*1.20);
    float3 serit=lerp(_SwirlColor.rgb,float3(1,1,1),.32);
    govde=lerp(govde, serit*(.30+cam*1.10), kurdele*.84);

    float parlak=smoothstep(.60,.97,cam);
    float3 col=lerp(govde, float3(1,1,1), parlak*.80);

    return half4(col,1);
   }
   ENDHLSL
  }
 }
}
