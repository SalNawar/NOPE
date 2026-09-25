Shader "NOPE/Hall Crowd Contact"
{
    Properties {[MainColor] _Tint("Ground contact",Color)=(.08,.11,.14,.16)}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent-10" "RenderType"="Transparent"}
        Pass
        {
            Tags {"LightMode"="UniversalForwardOnly"}
            ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial) half4 _Tint; CBUFFER_END
            struct A{float4 p:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;};
            V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;return o;}
            half4 Frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                half soft=1-smoothstep(.38,1,length(p));
                half broken=.72+.28*cos(p.x*12+p.y*2);
                return half4(_Tint.rgb,_Tint.a*soft*broken);
            }
            ENDHLSL
        }
    }
}
