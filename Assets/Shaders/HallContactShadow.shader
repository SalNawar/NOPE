Shader "NOPE/Hall Contact Shadow"
{
 Properties {_Strength("Contact shade",Range(0,1))=.22}
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off Blend DstColor Zero
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  float _Strength;
  struct A {float4 position:POSITION;float2 uv:TEXCOORD0;};
  struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
  V vert(A v) {V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;return o;}
  half4 frag(V v):SV_Target
  {float r=length(v.uv*2-1);float shade=(1-smoothstep(.15,1,r))*_Strength;return half4((1-shade).xxx,1);}
  ENDHLSL
  Pass {Tags {"LightMode"="Universal2D"} HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   ENDHLSL
  }
  Pass {Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   ENDHLSL
  }
 }
}

