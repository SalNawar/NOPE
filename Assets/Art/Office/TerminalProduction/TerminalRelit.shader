Shader "NOPE/Terminal Relit"
{
 Properties {
  _BaseMap("Neutral albedo / alpha",2D)="white"{}
  _BaseColor("Surface colour",Color)=(1,1,1,1)
  _Cutoff("Alpha cutoff",Range(0,1))=.45
  _Smoothness("Broad highlight",Range(0,1))=.1
  _Wear("Subtle surface variation",Range(0,1))=.035
  _TextureStrength("Albedo detail strength",Range(0,1))=1
  _ReflectionStrength("Live floor reflection",Range(0,1))=0
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
  Cull Off
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
  TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
  TEXTURE2D(_TerminalFloorReflection);SAMPLER(sampler_TerminalFloorReflection);float4x4 _TerminalFloorVP;
  CBUFFER_START(UnityPerMaterial)
   float4 _BaseMap_ST; half4 _BaseColor; half _Cutoff,_Smoothness,_Wear,_TextureStrength,_ReflectionStrength;
  CBUFFER_END
  ENDHLSL
  Pass {
   Name "ForwardLit"
   Tags {"LightMode"="UniversalForward"}
   ZWrite On
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
   #pragma multi_compile _ _ADDITIONAL_LIGHTS
   #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
   #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
   #pragma multi_compile_fog
   #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
   struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
   V Vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);o.fog=ComputeFogFactorZ0ToFar(max(0,-TransformWorldToView(o.w).z));return o;}
   half3 Shade(Light l,half3 n,half3 v){half d=saturate(dot(n,l.direction));half band=lerp(.06,1,smoothstep(.12,.72,d));half spec=pow(saturate(dot(n,normalize(l.direction+v))),24)*_Smoothness*d;return l.color*l.distanceAttenuation*l.shadowAttenuation*(band+spec);}
   half4 Frag(V i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target {
    half4 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv);half4 a=half4(lerp(half3(1,1,1),tex.rgb,_TextureStrength),tex.a)*_BaseColor;clip(a.a-_Cutoff);
    half3 n=normalize(i.n)*IS_FRONT_VFACE(face,1,-1);half3 v=GetWorldSpaceNormalizeViewDir(i.w);
    Light sun=GetMainLight(TransformWorldToShadowCoord(i.w));half3 lighting=SampleSH(n)+Shade(sun,n,v);
    #ifdef _ADDITIONAL_LIGHTS
    uint count=GetAdditionalLightsCount();for(uint k=0;k<count;k++){Light l=GetAdditionalLight(k,i.w,half4(1,1,1,1));lighting+=Shade(l,n,v);}
    #endif
    #ifdef _SCREEN_SPACE_OCCLUSION
    AmbientOcclusionFactor ao=GetScreenSpaceAmbientOcclusion(GetNormalizedScreenSpaceUV(i.p));lighting*=ao.indirectAmbientOcclusion;
    #endif
    half variation=1-_Wear*(.5+.5*sin(i.w.x*3.1+i.w.z*1.7)*sin(i.w.y*4.3+i.w.z*.7));
    half3 result=a.rgb*lighting*variation;
    if(_ReflectionStrength>0 && n.y>.7){
        float4 rp=mul(_TerminalFloorVP,float4(i.w,1));float2 ruv=rp.xy/rp.w*.5+.5;
        half3 reflection=SAMPLE_TEXTURE2D_LOD(_TerminalFloorReflection,sampler_TerminalFloorReflection,ruv,2.8).rgb;
        half fresnel=.35+.65*pow(1-saturate(dot(n,v)),3);result=lerp(result,reflection,_ReflectionStrength*fresnel);
    }
    return half4(MixFog(result,i.fog),1);
   }
   ENDHLSL
  }
  Pass {
   Name "ShadowCaster" Tags {"LightMode"="ShadowCaster"}
   ZWrite On ZTest LEqual ColorMask 0
   HLSLPROGRAM
   #pragma vertex ShadowVert
   #pragma fragment ShadowFrag
   #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
   float3 _LightDirection;float3 _LightPosition;
   struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;};
   struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   V ShadowVert(A a){V o;float3 w=TransformObjectToWorld(a.p.xyz);float3 n=TransformObjectToWorldNormal(a.n);
   #if _CASTING_PUNCTUAL_LIGHT_SHADOW
    float3 direction=normalize(_LightPosition-w);
   #else
    float3 direction=_LightDirection;
   #endif
    o.p=TransformWorldToHClip(ApplyShadowBias(w,n,direction));
   #if UNITY_REVERSED_Z
    o.p.z=min(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
   #else
    o.p.z=max(o.p.z,UNITY_NEAR_CLIP_VALUE*o.p.w);
   #endif
    o.uv=TRANSFORM_TEX(a.uv,_BaseMap);return o;}
   half4 ShadowFrag(V i):SV_Target {clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a*_BaseColor.a-_Cutoff);return 0;}
   ENDHLSL
  }
  Pass {
   Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
   ZWrite On ColorMask R
   HLSLPROGRAM
   #pragma vertex DepthVert
   #pragma fragment DepthFrag
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
   V DepthVert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);return o;}
   half4 DepthFrag(V i):SV_Target{clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).a*_BaseColor.a-_Cutoff);return i.p.z;}
   ENDHLSL
  }
 }
}
