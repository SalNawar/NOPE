Shader "NOPE/Hall Foreground Floor"
{
 Properties { _BaseMap("Terracotta tiles", 2D)="white" {} _BaseColor("Daylight match", Color)=(1,1,1,1) }
 SubShader {
  Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
  Pass {
   Tags { "LightMode"="UniversalForward" }
   Cull Off
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
   CBUFFER_END
   float4 _HallFloorEdge;
   float4 _HallFloorShade;
   TEXTURE2D(_HallFloorBackdrop); SAMPLER(sampler_HallFloorBackdrop);
   float4 _HallFloorBackdropSize;
   float _HallFloorHasBackdrop;
   struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
   struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float4 screen:TEXCOORD1; };
   Varyings vert(Attributes input) {
    Varyings o;
    o.positionCS=TransformObjectToHClip(input.positionOS.xyz);
    o.screen=ComputeScreenPos(o.positionCS);
    o.uv=TRANSFORM_TEX(input.uv,_BaseMap);
    return o;
   }
   half4 frag(Varyings input):SV_Target {
    // Keep the visual floor proxy below the painted canvas throughout camera movement.
    float4 edge=ComputeScreenPos(TransformWorldToHClip(_HallFloorEdge.xyz));
    clip(edge.y/edge.w+0.0015-input.screen.y/input.screen.w);
    half3 shade=_HallFloorShade.rgb;
    if(_HallFloorHasBackdrop>0.5)
    {
     float2 seam=float2(input.screen.x/input.screen.w,edge.y/edge.w+3*_HallFloorBackdropSize.y);
     // Average neighbouring tiles so a grout line cannot paint a vertical stripe.
     half3 edgeColour=0;
     [unroll] for(int i=-4;i<=4;i++)
      edgeColour+=SAMPLE_TEXTURE2D(_HallFloorBackdrop,sampler_HallFloorBackdrop,saturate(seam+float2(i*0.025,abs(i)*_HallFloorBackdropSize.y))).rgb;
     // Linear-light median of the generated texture's unlit RGB (152,104,86).
     shade=edgeColour/(9*half3(0.314,0.1384,0.0931));
    }
    return half4(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv).rgb*_BaseColor.rgb*shade,1);
   }
   ENDHLSL
  }
 }
}
