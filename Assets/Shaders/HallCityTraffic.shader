Shader "NOPE/Hall City Traffic"
{
 Properties {
 [PerRendererData] _MainTex("Vehicle sprite",2D)="white"{}
 _WindowMask("Original window aperture",2D)="white"{}
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 TEXTURE2D(_WindowMask);SAMPLER(sampler_WindowMask);
 float4x4 _WindowToLocal;
 float4 _CanvasMetrics,_CanvasSize;
 struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float2 aperture:TEXCOORD1;};
 V vert(A v){
 V o;float3 world=TransformObjectToWorld(v.position.xyz);
 o.position=TransformWorldToHClip(world);o.uv=v.uv;o.color=v.color;
 float2 local=mul(_WindowToLocal,float4(world,1)).xy;
 o.aperture=(local*_CanvasMetrics.x+_CanvasMetrics.yz)/_CanvasSize.xy;
 return o;
 }
 half4 frag(V v):SV_Target {
 half4 car=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv)*v.color;
 float inside=step(0,v.aperture.x)*step(v.aperture.x,1)*step(0,v.aperture.y)*step(v.aperture.y,1);
 car.a*=SAMPLE_TEXTURE2D(_WindowMask,sampler_WindowMask,v.aperture).a*inside;
 return car;
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
