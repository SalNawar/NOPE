Shader "NOPE/Office Painted Surface"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo atlas", 2D) = "white" {}
        [MainColor] _BaseColor("Paint tint", Color) = (1,1,1,1)
        _Saturation("Texture saturation", Range(0,1.5)) = 0.85
        _Contrast("Texture contrast", Range(0.4,1.4)) = 0.9
        _MipBias("Fine texture suppression", Range(0,3)) = 0.5
        _AccentRemap("Replace red factory paint", Range(0,1)) = 0
        _AccentColor("Replacement paint", Color) = (0.16,0.32,0.3,1)
        _Smoothness("Smoothness", Range(0,1)) = 0.22
        _Metallic("Metallic", Range(0,1)) = 0
        [Normal] _BumpMap("Normal map", 2D) = "bump" {}
        _BumpScale("Normal strength", Range(0,1)) = 0.2
        _EmissionMap("Emission mask", 2D) = "black" {}
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,0)
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Cutout", Float) = 0
        _Cutoff("Cutout threshold", Range(0,1)) = 0.5
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
        [HideInInspector] _Surface("Surface", Float) = 0
        [HideInInspector] _ZWrite("Z write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "OfficeForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.5
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor, _AccentColor, _EmissionColor;
                half _Saturation, _Contrast, _MipBias, _AccentRemap;
                half _Smoothness, _Metallic, _BumpScale, _Cutoff;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 tangentOS:TANGENT; float2 uv:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; half3 normalWS:TEXCOORD1; half4 tangentWS:TEXCOORD2; float2 uv:TEXCOORD3; half fog:TEXCOORD4; UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO };
            Varyings Vert(Attributes v)
            {
                Varyings o=(Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v,o); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs n=GetVertexNormalInputs(v.normalOS,v.tangentOS);
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=n.normalWS;
                o.tangentWS=half4(n.tangentWS,v.tangentOS.w*GetOddNegativeScale());
                o.uv=TRANSFORM_TEX(v.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i); UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 tex=SAMPLE_TEXTURE2D_BIAS(_BaseMap,sampler_BaseMap,i.uv,_MipBias);
                #if defined(_ALPHATEST_ON)
                    clip(tex.a*_BaseColor.a-_Cutoff);
                #endif
                half redMask=smoothstep(0.05,0.24,tex.r-max(tex.g,tex.b))*_AccentRemap;
                half luma=dot(tex.rgb,half3(.2126,.7152,.0722));
                half3 albedo=lerp(luma.xxx,tex.rgb,_Saturation);
                albedo=lerp(albedo,_AccentColor.rgb*lerp(.6,1.4,luma),redMask);
                albedo=saturate((albedo-.18h)*_Contrast+.18h)*_BaseColor.rgb;
                half3 normalTS=UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,i.uv),_BumpScale);
                half3 bitangent=cross(i.normalWS,i.tangentWS.xyz)*i.tangentWS.w;
                InputData d=(InputData)0;
                d.positionWS=i.positionWS;
                d.normalWS=NormalizeNormalPerPixel(TransformTangentToWorld(normalTS,half3x3(i.tangentWS.xyz,bitangent,i.normalWS)));
                d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                d.bakedGI=SampleSH(d.normalWS);
                d.vertexLighting=VertexLighting(i.positionWS,d.normalWS);
                d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                d.shadowMask=half4(1,1,1,1);
                SurfaceData s=(SurfaceData)0;
                s.albedo=albedo;s.alpha=1;s.normalTS=normalTS;s.metallic=_Metallic;s.specular=half3(.04,.04,.04);
                s.smoothness=_Smoothness;s.occlusion=1;
                s.emission=SAMPLE_TEXTURE2D(_EmissionMap,sampler_EmissionMap,i.uv).rgb*_EmissionColor.rgb;
                half4 color=UniversalFragmentPBR(d,s);color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
