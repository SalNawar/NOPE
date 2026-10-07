Shader "NOPE/Hall White Crowd Contact"
{
 Properties {_Tint("Contact",Color)=(.03,.045,.06,.09)}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  half4 _Tint;
  struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};
  struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
  V vert(A v){V o;o.p=TransformObjectToHClip(v.p.xyz);o.uv=v.uv;return o;}
  half4 frag(V v):SV_Target{return half4(_Tint.rgb,_Tint.a*(1-smoothstep(.1,1,length((v.uv-.5)*2))));}
  ENDHLSL
  Pass {Tags {"LightMode"="Universal2D"} HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  ENDHLSL }
  Pass {Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  ENDHLSL }
 }
}
