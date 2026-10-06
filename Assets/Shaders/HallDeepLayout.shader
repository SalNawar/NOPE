Shader "NOPE/Hall Deep Layout"
{
 Properties {
 [PerRendererData] _MainTex("Approved deeper room",2D)="white"{}
 _Masks("Left/front windows and floor receiver",2D)="black"{}
 _CityLeft("Complete left panorama",2D)="white"{}
 _CityFront("Connected front extension",2D)="white"{}
 _Region("0 architecture, 1 left city, 2 front city",Float)=0
 _CanvasSize("Canvas pixels",Vector)=(2172,724,0,0)
 _CeilingCutoff("Ceiling emission region",Float)=180
 _MorningShadow("Morning shadow",2D)="black"{}
 _NoonShadow("Noon shadow",2D)="black"{}
 _EveningShadow("Evening shadow",2D)="black"{}
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "HallFloorShadowGeometry.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 TEXTURE2D(_Masks);SAMPLER(sampler_Masks);
 TEXTURE2D(_CityLeft);SAMPLER(sampler_CityLeft);
 TEXTURE2D(_CityFront);SAMPLER(sampler_CityFront);
 TEXTURE2D(_MorningShadow);SAMPLER(sampler_MorningShadow);
 TEXTURE2D(_NoonShadow);TEXTURE2D(_EveningShadow);
 float4 _StateWeights,_CanvasSize;float _LightingAmount,_CloudMotion,_Region,_CityPan,_CeilingCutoff;
 struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A v){V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.color=v.color;return o;}
 half4 frag(V v):SV_Target {
  half4 mask=SAMPLE_TEXTURE2D(_Masks,sampler_Masks,v.uv);
  float2 p=float2(v.uv.x*_CanvasSize.x,(1-v.uv.y)*_CanvasSize.y);
  if(_Region>.5) {
   float left=_Region<1.5?1:0;
   // Clean exterior artwork moves beneath stationary hall apertures.
   float shift=_CityPan*(left>.5?1:.4);
   float2 cityUV=saturate(v.uv+float2(shift,0));
   float2 side=cityUV,front=cityUV;
   half3 rgb=left>.5?SAMPLE_TEXTURE2D(_CityLeft,sampler_CityLeft,saturate(side)).rgb:
    SAMPLE_TEXTURE2D(_CityFront,sampler_CityFront,saturate(front)).rgb;
      // Morning is the sole authored exterior. Temporary cycle tint keeps the
   // day/night slider coherent while matching compositions are still pending.
   half3 cityTint=half3(1,.94,.86)*_StateWeights.x+half3(1,1,1)*_StateWeights.y+
    half3(.81,.59,.46)*_StateWeights.z+half3(.24,.32,.49)*_StateWeights.w;
   rgb*=lerp(half3(1,1,1),cityTint,saturate(_LightingAmount));
   return half4(rgb*v.color.rgb,(left>.5?mask.r:mask.g)*v.color.a);
  }
  half4 source=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv)*v.color;
  source.a*=1-max(mask.r,mask.g);
  half3 tint=half3(1,.94,.86)*_StateWeights.x+half3(1,1,1)*_StateWeights.y+
   half3(.81,.59,.46)*_StateWeights.z+half3(.24,.32,.49)*_StateWeights.w;
  half shadow=mask.b>.001?HallGeometricShadow(p,_StateWeights):0;
  // Only bright neutral ceiling diffusers emit; no detached glow columns.
  half neutral=min(source.r,min(source.g,source.b));
  half emission=smoothstep(.76,.94,neutral)*step(p.y,_CeilingCutoff)*step(_CanvasSize.x*.36,p.x)*(1-max(mask.r,mask.g));
  half3 lit=source.rgb*tint*(1-shadow*mask.b)+emission*half3(1,.78,.5)*(.18*_StateWeights.z+.45*_StateWeights.w);
  source.rgb=lerp(source.rgb,lit,saturate(_LightingAmount));
  return source;
 }
 ENDHLSL
 Pass {Tags {"LightMode"="Universal2D"} HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 ENDHLSL}
 Pass {Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 ENDHLSL}
 }
}
