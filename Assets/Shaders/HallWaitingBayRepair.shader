Shader "NOPE/Hall Waiting Bay Repair"
{
 Properties
 {
  [PerRendererData] _MainTex("Sprite",2D)="white"{}
  _WaitingGuide("Waiting bay illustration",2D)="white"{}
  _RemovedMask("Removed dispenser mask",2D)="black"{}
  _RestoreRemovedOnly("Restore removed objects",Float)=1
  _RepairRect("Right wall service region pixels",Vector)=(1920,475,2172,724)
  _PaletteGuide("Selected palette guide",2D)="white"{}
  _PaletteSource("Original registered painting",2D)="white"{}
  _Morning("Morning light",2D)="white"{} _Noon("Noon light",2D)="white"{}
  _Evening("Evening light",2D)="white"{} _Night("Night light",2D)="white"{}
  _MorningGlow("Morning emission",2D)="black"{} _NoonGlow("Noon emission",2D)="black"{}
  _EveningGlow("Evening emission",2D)="black"{} _NightGlow("Night emission",2D)="black"{}
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
  TEXTURE2D(_Morning);SAMPLER(sampler_Morning);
  TEXTURE2D(_Noon);TEXTURE2D(_Evening);TEXTURE2D(_Night);
  TEXTURE2D(_MorningGlow);TEXTURE2D(_NoonGlow);TEXTURE2D(_EveningGlow);TEXTURE2D(_NightGlow);
  float4 _StateWeights;float _LightingAmount;float _CloudMotion;
  TEXTURE2D(_PaletteGuide);SAMPLER(sampler_PaletteGuide);
  TEXTURE2D(_PaletteSource);SAMPLER(sampler_PaletteSource);
  float _PaletteAmount;
  TEXTURE2D(_WaitingGuide);SAMPLER(sampler_WaitingGuide);
  TEXTURE2D(_RemovedMask);SAMPLER(sampler_RemovedMask);
  float _RestoreRemovedOnly;float4 _RepairRect;
  struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
  struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
  V vert(A v){V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.color=v.color;return o;}
  float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
  float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
  half4 frag(V v):SV_Target
  {
   half4 source=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv)*v.color;
   float2 pixel=float2(v.uv.x*2172,(1-v.uv.y)*724);
   float inRect=step(_RepairRect.x,pixel.x)*step(pixel.x,_RepairRect.z)*step(_RepairRect.y,pixel.y)*step(pixel.y,_RepairRect.w);
   source.a=_RestoreRemovedOnly>.5?max(source.a,SAMPLE_TEXTURE2D(_RemovedMask,sampler_RemovedMask,v.uv).a):inRect;
   source.rgb=SAMPLE_TEXTURE2D(_WaitingGuide,sampler_WaitingGuide,v.uv).rgb*v.color.rgb;
   half4 light=SAMPLE_TEXTURE2D(_Morning,sampler_Morning,v.uv)*_StateWeights.x
    +SAMPLE_TEXTURE2D(_Noon,sampler_Morning,v.uv)*_StateWeights.y
    +SAMPLE_TEXTURE2D(_Evening,sampler_Morning,v.uv)*_StateWeights.z
    +SAMPLE_TEXTURE2D(_Night,sampler_Morning,v.uv)*_StateWeights.w;
   half3 glow=SAMPLE_TEXTURE2D(_MorningGlow,sampler_Morning,v.uv).rgb*_StateWeights.x
    +SAMPLE_TEXTURE2D(_NoonGlow,sampler_Morning,v.uv).rgb*_StateWeights.y
    +SAMPLE_TEXTURE2D(_EveningGlow,sampler_Morning,v.uv).rgb*_StateWeights.z
    +SAMPLE_TEXTURE2D(_NightGlow,sampler_Morning,v.uv).rgb*_StateWeights.w;
   float time=_Time.y*.009*_CloudMotion;
   float clouds=smoothstep(.35,.76,noise(v.uv*float2(8,3)+float2(time,time*.3)));
   light.rgb*=1-clouds*light.a*.12;
   source.rgb=lerp(source.rgb,source.rgb*light.rgb+glow*v.color.rgb,saturate(_LightingAmount));
   return source;
  }
  ENDHLSL
  Pass {Tags {"LightMode"="Universal2D"} HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   ENDHLSL}
  Pass {Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   ENDHLSL}
 }
}
