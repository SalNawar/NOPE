Shader "NOPE/Hall Ground Shadow"
{
 Properties { _Ground("Concourse receiver",2D)="black"{} _Platform("Platform receiver",2D)="black"{} _Gallery("Gallery receiver",2D)="black"{} _Strength("Shadow",Range(0,1))=.42 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  TEXTURE2D(_Ground);SAMPLER(sampler_Ground);
  TEXTURE2D(_Platform);SAMPLER(sampler_Platform);
  TEXTURE2D(_Gallery);SAMPLER(sampler_Gallery);
  float _Strength;
  struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float2 receiver:TEXCOORD1;float4 color:COLOR;};
  struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float2 receiver:TEXCOORD1;float4 color:COLOR;};
  V vert(A v){V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.receiver=v.receiver;o.color=v.color;return o;}
  half4 frag(V v):SV_Target
  {
   float alpha=max(SAMPLE_TEXTURE2D(_Ground,sampler_Ground,v.receiver).a,max(SAMPLE_TEXTURE2D(_Platform,sampler_Platform,v.receiver).a,SAMPLE_TEXTURE2D(_Gallery,sampler_Gallery,v.receiver).a));
   float edge=smoothstep(0,.08,v.uv.x)*(1-smoothstep(.92,1,v.uv.x))*(1-smoothstep(.86,1,v.uv.y));
   return half4(.10,.12,.17,alpha*edge*v.color.a*_Strength);
  }
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
