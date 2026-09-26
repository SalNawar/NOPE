// NOPE/Desk Anime: the illustrated desk shader (art side). Replaces URP's PBR lighting
// with three tone bands (shadow, midtone, light) over a flat colour or illustration,
// with a cool shadow and warm light multiplier, small painted highlights and a soft
// grazing-angle contour. Receives main and additional light shadows, SSAO and fog;
// optional alpha cutout (2D paper props).
// Passes: DeskAnimeForward (UniversalForwardOnly), ShadowCaster, DepthOnly and
// DepthNormalsOnly, all on one UnityPerMaterial buffer (SRP Batcher compatible).
// Used by every desk prop and booth finish (the DebtRelief Desk_*/Booth_* materials)
// and the Rebuilt CRT (CRT2_*). The presets and visual rules are in
// ArtDeliverables/TimeDesk/ImportedOffice/DeskClean/ANIME_SHADER.md; the Debt Relief
// pass and Apply Desk Anime Shading set them. No FallBack (triage B15).
Shader "NOPE/Desk Anime"
{
    Properties
    {
        [MainTexture] _BaseMap("Clean colour / illustration",2D)="white"{}
        [MainColor] _BaseColor("Object colour",Color)=(1,1,1,1)
        _ShadowTint("Cool shadow multiplier",Vector)=(.72,.81,.94,0)
        _LightTint("Warm light multiplier",Vector)=(1.06,1.015,.92,0)
        _ShadowValue("Shadow value",Range(.1,.9))=.48
        _ShadowReception("Cast shadow strength",Range(0,1))=1
        _MidValue("Midtone value",Range(.3,1))=.76
        _BandSoftness("Band transition",Range(.005,.3))=.065
        _ShadowThreshold("Shadow boundary",Range(-.5,.5))=.02
        _LightThreshold("Light boundary",Range(.1,.95))=.58
        _HighlightStrength("Painted highlight",Range(0,.5))=.06
        _HighlightSize("Highlight size",Range(.01,.35))=.08
        _EdgeStrength("Subtle contour",Range(0,.35))=.10
        _Smoothness("Smoothness compatibility",Range(0,1))=.25
        _Metallic("Metal highlight",Range(0,1))=0
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Illustration cutout",Float)=0
        _Cutoff("Cutout threshold",Range(0,1))=.4
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2
        [HideInInspector] _Surface("Surface",Float)=0
        [HideInInspector] _ZWrite("Z write",Float)=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor,_ShadowTint,_LightTint;
            half _ShadowValue,_ShadowReception,_MidValue,_BandSoftness,_ShadowThreshold,_LightThreshold;
            half _HighlightStrength,_HighlightSize,_EdgeStrength,_Smoothness,_Metallic,_Cutoff;
        CBUFFER_END
        struct Attributes {float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
        struct Varyings {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;UNITY_VERTEX_INPUT_INSTANCE_ID UNITY_VERTEX_OUTPUT_STEREO};
        Varyings Vert(Attributes v)
        {
            Varyings o=(Varyings)0;
            UNITY_SETUP_INSTANCE_ID(v);UNITY_TRANSFER_INSTANCE_ID(v,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);
            o.positionCS=p.positionCS;o.positionWS=p.positionWS;
            o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.uv=TRANSFORM_TEX(v.uv,_BaseMap);
            o.fog=ComputeFogFactor(p.positionCS.z);return o;
        }
        half4 Albedo(float2 uv)
        {
            half4 a=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv)*_BaseColor;
            #if defined(_ALPHATEST_ON)
                clip(a.a-_Cutoff);
            #endif
            return a;
        }
        ENDHLSL
        Pass
        {
            Name "DeskAnimeForward"
            Tags {"LightMode"="UniversalForwardOnly"}
            Cull [_Cull]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            half3 BandedFill(Light light,half3 normal,half3 albedo)
            {
                half band=smoothstep(.05h,.18h,dot(normal,light.direction));
                return albedo*light.color*min(light.distanceAttenuation,.65)*light.shadowAttenuation*band*.18h;
            }
            half4 Frag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half3 a=Albedo(i.uv).rgb;
                half3 n=normalize(i.normalWS),v=GetWorldSpaceNormalizeViewDir(i.positionWS);
                float4 shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    shadowCoord=ComputeScreenPos(TransformWorldToHClip(i.positionWS));
                #endif
                Light key=GetMainLight(shadowCoord,i.positionWS,half4(1,1,1,1));
                half nl=dot(n,key.direction);
                // Derivative widening keeps narrow curved silhouettes from shimmering.
                half width=max(_BandSoftness,fwidth(nl)*1.25h);
                half mid=smoothstep(_ShadowThreshold-width,_ShadowThreshold+width,nl);
                half lit=smoothstep(_LightThreshold-width,_LightThreshold+width,nl);
                half cast=lerp(1,smoothstep(.14h,.78h,key.shadowAttenuation),_ShadowReception);
                half3 shade=_ShadowTint.rgb*_ShadowValue;
                half3 tone=lerp(shade,lerp(_MidValue.xxx,_LightTint.rgb,lit),mid*cast);
                half3 lightHue=lerp(half3(1,1,1),key.color/max(max(key.color.r,key.color.g),max(key.color.b,.001h)),.25h);
                // Cap bright sunlight to preserve the painted palette, but follow
                // diminishing light intensity instead of glowing when the key goes off.
                half keyEnergy=saturate(dot(key.color,half3(.2126,.7152,.0722))*key.distanceAttenuation);
                half3 color=a*tone*lightHue*keyEnergy;
                // A small environment contribution grounds the bands without washing them out.
                color+=a*min(SampleSH(n),half3(.24,.24,.24))*.30h;
                half nh=saturate(dot(n,normalize(key.direction+v)));
                half highlight=smoothstep(1-_HighlightSize,1-_HighlightSize+.025h,nh)*lit*cast;
                color+=lerp(half3(1,.95,.82),a,_Metallic)*highlight*_HighlightStrength*keyEnergy;
                InputData inputData=(InputData)0;inputData.positionWS=i.positionWS;
                inputData.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                #if defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHTS_VERTEX)
                    #if USE_CLUSTER_LIGHT_LOOP
                        [loop] for(uint lightIndex=0;lightIndex<min(URP_FP_DIRECTIONAL_LIGHTS_COUNT,MAX_VISIBLE_LIGHTS);lightIndex++)
                        {
                            CLUSTER_LIGHT_LOOP_SUBTRACTIVE_LIGHT_CHECK
                            color+=BandedFill(GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1)),n,a);
                        }
                    #endif
                    uint count=GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light fill=GetAdditionalLight(lightIndex,i.positionWS,half4(1,1,1,1));
                        color+=BandedFill(fill,n,a);
                    LIGHT_LOOP_END
                #endif
                half edge=1-smoothstep(.035h,.18h,saturate(dot(n,v)));
                color*=1-edge*_EdgeStrength;
                #if defined(_SCREEN_SPACE_OCCLUSION)
                    AmbientOcclusionFactor ao=GetScreenSpaceAmbientOcclusion(inputData.normalizedScreenSpaceUV);
                    color*=lerp(.55h,1,ao.indirectAmbientOcclusion);
                #endif
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection,_LightPosition;
            Varyings ShadowVert(Attributes v)
            {
                Varyings o=Vert(v);float3 direction=_LightDirection;
                #if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
                    direction=normalize(_LightPosition-o.positionWS);
                #endif
                o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,direction));
                o.positionCS=ApplyShadowClamping(o.positionCS);
                return o;
            }
            half4 DepthFrag(Varyings i):SV_Target {UNITY_SETUP_INSTANCE_ID(i);Albedo(i.uv);return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            half4 DepthFrag(Varyings i):SV_Target {UNITY_SETUP_INSTANCE_ID(i);Albedo(i.uv);return i.positionCS.z;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags {"LightMode"="DepthNormalsOnly"}
            ZWrite On Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment NormalsFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"
            half4 NormalsFrag(Varyings i):SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);Albedo(i.uv);half3 n=normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(n)*.5+.5)),0);
                #else
                    return half4(n,0);
                #endif
            }
            ENDHLSL
        }
    }
}
