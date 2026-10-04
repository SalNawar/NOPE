Shader "NOPE/Hall Foreground Floor"
{
 Properties { _BaseMap("Terracotta tiles", 2D)="white" {} _PaintedReference("Unlit registered painting",2D)="white"{} _BaseColor("Daylight match", Color)=(1,1,1,1) }
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
   TEXTURE2D(_PaintedReference); SAMPLER(sampler_PaintedReference);
   CBUFFER_START(UnityPerMaterial)
    float4 _BaseMap_ST;
    float4 _BaseColor;
   CBUFFER_END
   float4 _HallFloorEdge;
   float4 _HallFloorShade;
   float4 _HallFloorCanvas;
   float4 _HallFloorRegistration;
   float4x4 _HallFloorCanvasToWorld;
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
    float2 canvas=float2(input.uv.x*_HallFloorCanvas.x,(1-input.uv.y)*_HallFloorCanvas.y);
    float depth=max(canvas.y-_HallFloorCanvas.w,1);
    float seamDepth=_HallFloorCanvas.y-_HallFloorCanvas.w;
    // Reproject each painted grout ray from its exact intersection with the
    // canvas edge. Both sides share the same transform during pan and tilt.
    float sampleX=_HallFloorCanvas.z+(canvas.x-_HallFloorCanvas.z)*seamDepth/depth;
    half3 colour=SAMPLE_TEXTURE2D(_PaintedReference,sampler_PaintedReference,float2(saturate(sampleX/_HallFloorCanvas.x),2/_HallFloorCanvas.y)).rgb*_HallFloorShade.rgb;
    if(_HallFloorHasBackdrop>0.5)
    {
     float3 local=float3((sampleX-_HallFloorRegistration.y)/_HallFloorRegistration.x,(2-_HallFloorRegistration.z)/_HallFloorRegistration.x,0);
     float3 world=mul(_HallFloorCanvasToWorld,float4(local,1)).xyz;
     float4 projected=ComputeScreenPos(TransformWorldToHClip(world));
     float2 uv=projected.xy/projected.w;
     half3 exact=SAMPLE_TEXTURE2D(_HallFloorBackdrop,sampler_HallFloorBackdrop,saturate(uv)).rgb;
     half3 average=0;
     [unroll] for(int j=0;j<9;j++) average+=SAMPLE_TEXTURE2D(_HallFloorBackdrop,sampler_HallFloorBackdrop,float2(.1+j*.1,saturate(uv.y))).rgb;
     colour=lerp(exact,average/9,smoothstep(0,12,canvas.y-_HallFloorCanvas.y));
    }
    // Perspective spacing continues the final painted row (approximately
    // 36 canvas pixels deep). Keep its grout dark and fine, never white.
    float row=(1/seamDepth-1/depth)/.00018;
    float rowDistance=abs(frac(row+.5)-.5);
    float horizontal=1-smoothstep(.008,.022,rowDistance);
    horizontal*=smoothstep(0,12,canvas.y-_HallFloorCanvas.y);
    // Native pixel intersections and tangents traced from the last painted
    // platform row. The hand-painted joints do not share a perfect vanishing
    // point, so extrapolate each actual tangent instead of fitting a new grid.
    const float origins[14]={106,224,359,493,608,735,836,933,1043,1172,1274,1374,1505,1634};
    const float slopes[14]={-1.428,-1.483,-1.287,-1.077,-.87,-.658,-.424,-.148,0,.326,.554,.707,1.028,1.22};
    float distanceToJoint=10000;
    [unroll] for(int k=0;k<14;k++) distanceToJoint=min(distanceToJoint,abs(canvas.x-origins[k]-slopes[k]*(canvas.y-724)));
    float width=1.2*depth/seamDepth;
    float vertical=1-smoothstep(width,width+max(fwidth(canvas.x),.6),distanceToJoint);
    vertical*=smoothstep(0,9,canvas.y-_HallFloorCanvas.y);
    float grey=dot(colour,half3(.2126,.7152,.0722))*1.22;
    colour=lerp(colour,lerp(colour,grey.xxx,.75),max(horizontal,vertical)*.65);
    // Gentle native surface variation, rather than stretching a one-row bitmap
    // or allowing a second texture's grout to produce another competing grid.
    float2 cell=float2(sampleX/26,row*6);
    float2 f=frac(cell);f=f*f*(3-2*f);float2 c=floor(cell);
    float a=frac(sin(dot(c,float2(127.1,311.7)))*43758.5453);
    float b=frac(sin(dot(c+float2(1,0),float2(127.1,311.7)))*43758.5453);
    float d=frac(sin(dot(c+float2(0,1),float2(127.1,311.7)))*43758.5453);
    float e=frac(sin(dot(c+1,float2(127.1,311.7)))*43758.5453);
    colour*=lerp(.975,1.025,lerp(lerp(a,b,f.x),lerp(d,e,f.x),f.y));
    return half4(colour*_BaseColor.rgb,1);
   }
   ENDHLSL
  }
 }
}
