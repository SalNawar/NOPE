Shader "NOPE/Hall Mounted Fixture"
{
 Properties {_MainTex("Unlit fixture and mount",2D)="white"{} _FixtureLevel("Diffuser emission",Range(0,1))=0}
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 float _FixtureLevel;
 struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A v){V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.color=v.color;return o;}
 half4 frag(V v):SV_Target
 {
  half4 fixture=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv)*v.color;
  // Registered inner glass of GalleryMountedUnlit. Mount/rim never emit.
  float glass=smoothstep(.375,.395,v.uv.x)*(1-smoothstep(.60,.62,v.uv.x))
   *smoothstep(.075,.10,v.uv.y)*(1-smoothstep(.645,.67,v.uv.y));
  fixture.rgb=lerp(fixture.rgb,half3(1,.79,.54),glass*saturate(_FixtureLevel));
  return fixture;
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
