// The hover outline on office objects (HoverHighlighter): the object's mesh
// drawn again, pushed out along its normals, back faces only, in one flat
// colour, so only a rim around the silhouette shows. Drawn in the
// transparent queue through Graphics.RenderMesh, after the opaque office.
// With _WidthPx over 0 the rim is that many screen px at 1080p (scaled by the
// screen's height) for every object, near or far, big or small (the review of
// the desk v3 capture: 3-4 px on BABEL, 1 px on the intercom); else _Width
// metres. _Pull draws it that far toward the camera, so the felt or the tray
// an object rests on never cuts its rim into dashes; its colour's alpha
// fades it in and out (HoverHighlighter: it never pops).
Shader "TimeDesk/HoverHull"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 1, 1, 1)
        _Width ("Width (metres)", Float) = 0.006
        _WidthPx ("Width (screen px at 1080p; over 0 it replaces the metres)", Float) = 0
        _Pull ("Pull toward the camera (metres)", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent+50" }

        Pass
        {
            Name "Hull"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Width, _WidthPx, _Pull;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                positionWS += normalize(GetCameraPositionWS() - positionWS) * _Pull;
                if (_WidthPx <= 0.0)
                {
                    output.positionCS = TransformWorldToHClip(positionWS + normalWS * _Width);
                    return output;
                }
                float4 positionCS = TransformWorldToHClip(positionWS);
                float2 normalCS = mul((float3x3)GetWorldToHClipMatrix(), normalWS).xy;
                float length2 = dot(normalCS, normalCS);
                float2 toward = length2 > 1e-10 ? normalCS * rsqrt(length2) : float2(0.0, 0.0);
                float2 ndc = toward * (_WidthPx * 2.0 / 1080.0);
                ndc.x *= _ScreenParams.y / max(_ScreenParams.x, 1.0);
                positionCS.xy += ndc * positionCS.w;
                output.positionCS = positionCS;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}
