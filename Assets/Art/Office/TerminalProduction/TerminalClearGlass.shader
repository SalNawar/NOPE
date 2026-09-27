Shader "NOPE/Terminal Clear Glass"
{
 Properties{_BaseColor("Glass tint",Color)=(.54,.71,.79,.025)}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
 Pass{
 Tags{"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Back
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;
 CBUFFER_END
 struct A{float4 p:POSITION;float3 n:NORMAL;};struct V{float4 p:SV_POSITION;float3 w:TEXCOORD0;half3 n:TEXCOORD1;};
 V Vert(A a){V o;o.w=TransformObjectToWorld(a.p.xyz);o.p=TransformWorldToHClip(o.w);o.n=TransformObjectToWorldNormal(a.n);return o;}
 half4 Frag(V i):SV_Target{half3 n=normalize(i.n),v=GetWorldSpaceNormalizeViewDir(i.w);Light l=GetMainLight();half edge=pow(1-abs(dot(n,v)),5);half glint=pow(saturate(dot(n,normalize(l.direction+v))),96)*.1;return half4(_BaseColor.rgb*(SampleSH(n)+l.color*.15)+l.color*glint,_BaseColor.a+edge*.055);}
 ENDHLSL
 }
 }
}
