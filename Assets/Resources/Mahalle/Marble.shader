Shader "MISKETR/SwirledGlass"
{
 Properties { _BaseColor("Glass tint", Color) = (.1,.7,.7,1) _SwirlColor("Inner ribbon", Color) = (1,.85,.3,1) }
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
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor; half4 _SwirlColor;
   CBUFFER_END
   struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float3 positionWS:TEXCOORD1; float2 uv:TEXCOORD2; };
   Varyings vert(Attributes v)
   { Varyings o; o.positionWS=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.positionWS); o.normalWS=TransformObjectToWorldNormal(v.normalOS); o.uv=v.uv; return o; }
   half4 frag(Varyings i):SV_Target
   {
    float3 n=normalize(i.normalWS), v=normalize(GetWorldSpaceViewDir(i.positionWS));
    Light l=GetMainLight(); float3 h=normalize(l.direction+v);
    float ndv=saturate(dot(n,v)); float fres=pow(1-ndv,3);
    float wave=sin(i.uv.x*19 + sin(i.uv.y*8)*2.6 + i.uv.y*10);
    float ribbon=smoothstep(.4,.85,wave) * sin(i.uv.y*3.14159);
    float3 col=lerp(_BaseColor.rgb*.65, _SwirlColor.rgb, ribbon*.68);
    col*=.48+saturate(dot(n,l.direction))*.55;
    col+=pow(saturate(dot(n,h)),95)*1.5*l.color;
    col+=pow(saturate(dot(n,normalize(float3(-.6,.7,-.8)))) ,22)*.18;
    col=lerp(col,_BaseColor.rgb*.6+float3(.2,.28,.3),fres*.7);
    col+=pow(ndv,4)*_BaseColor.rgb*.12;
    return half4(col,1);
   }
   ENDHLSL
  }
 }
}
