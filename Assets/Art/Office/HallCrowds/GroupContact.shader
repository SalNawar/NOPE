// NOPE/Hall Crowd Contact: the faint ground contact under each crowd group (art side).
// A soft, slightly broken ellipse over the quad's UVs in _Tint, alpha-blended
// without depth writes, so the flat groups sit on the floor with no 3D shadow.
// Unlit, one forward pass. Used by HallCrowds/Materials/Contact_Morning and
// Contact_Evening (tinted by OfficeHallCrowdPalette like the silhouettes).
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
