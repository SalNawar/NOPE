Shader "NOPE/Hall City Exterior"
{
 Properties {
 [PerRendererData] _MainTex("Original window aperture",2D)="white"{}
 _PanelRange("Connected panel range",Vector)=(0,1,0,0)
 _MorningCity("Morning city",2D)="white"{}
 _NoonCity("Noon city - morning placeholder until authored",2D)="white"{}
 _EveningCity("Evening city - morning placeholder until authored",2D)="white"{}
 _NightCity("Night city - morning placeholder until authored",2D)="white"{}
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 TEXTURE2D(_MorningCity);SAMPLER(sampler_MorningCity);
 TEXTURE2D(_NoonCity);TEXTURE2D(_EveningCity);TEXTURE2D(_NightCity);
 float4 _StateWeights;float _CityPan;float4 _PanelRange;
 struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A v){V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.color=v.color;return o;}
 half4 frag(V v):SV_Target {
 // Three spatially adjacent images share one seamless source painting.
 // Crop bounds select panels; no window repeats or independently scales a city.
 clip(v.uv.x-_PanelRange.x);clip(_PanelRange.y-v.uv.x);
 float2 cityUV=float2(v.uv.x+_CityPan,v.uv.y+.12);
 half4 city=SAMPLE_TEXTURE2D(_MorningCity,sampler_MorningCity,saturate(cityUV))*_StateWeights.x
 +SAMPLE_TEXTURE2D(_NoonCity,sampler_MorningCity,saturate(cityUV))*_StateWeights.y
 +SAMPLE_TEXTURE2D(_EveningCity,sampler_MorningCity,saturate(cityUV))*_StateWeights.z
 +SAMPLE_TEXTURE2D(_NightCity,sampler_MorningCity,saturate(cityUV))*_StateWeights.w;
 return half4(city.rgb*v.color.rgb,SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv).a*v.color.a);
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
