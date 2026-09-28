Shader "NOPE/Terminal Energy"
{
 Properties{_BaseColor("Energy colour",Color)=(.12,.42,.7,1)}
 SubShader{
 Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
 Pass{
 Tags{"LightMode"="UniversalForward"}
 Cull Off
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor;
 CBUFFER_END
 struct A{float4 p:POSITION;};struct V{float4 p:SV_POSITION;float2 local:TEXCOORD0;};
 V Vert(A a){V o;o.p=TransformObjectToHClip(a.p.xyz);o.local=a.p.xz*2;return o;}
 half4 Frag(V i):SV_Target{float r=length(i.local);float angle=atan2(i.local.y,i.local.x);float wave=.5+.5*sin(r*12-angle*2-_Time.y*.4);float edge=smoothstep(.72,1,r);return half4(_BaseColor.rgb*(.7+wave*.35)+half3(.15,.45,.65)*edge,1);}
 ENDHLSL
 }
 }
}
