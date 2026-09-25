Shader "NOPE/Hall Crowd Silhouette"
{
    Properties
    {
        [MainTexture] _BaseMap("Authored group alpha",2D)="white"{}
        [MainColor] _Tint("Silhouette colour",Color)=(.6,.67,.7,1)
        _Cutoff("Clean silhouette edge",Range(0,1))=.50
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            half4 _Tint;half _Cutoff;
        CBUFFER_END
        struct A {float4 p:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
        struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;half fog:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
        V Vert(A i)
        {
            V o=(V)0;UNITY_SETUP_INSTANCE_ID(i);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.fog=ComputeFogFactor(o.p.z);return o;
        }
        void Silhouette(float2 uv){clip(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).a-_Cutoff);}
        ENDHLSL
        Pass
        {
            Name "Flat drawn group" Tags {"LightMode"="UniversalForwardOnly"}
            ZWrite On Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            half4 Frag(V i):SV_Target
            {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);Silhouette(i.uv);return half4(MixFog(_Tint.rgb,i.fog),1);}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly" Tags {"LightMode"="DepthOnly"}
            ZWrite On Cull Off ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            half4 Frag(V i):SV_Target{Silhouette(i.uv);return i.p.z;}
            ENDHLSL
        }
        // These are painted extras, so there is deliberately no 3D shadow caster.
        // Separate low-opacity ground contacts anchor each complete group.
    }
}
