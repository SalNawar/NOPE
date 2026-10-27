// The hall window's flying traffic (HallCityExterior's lanes): a vehicle sprite clipped to the hall's
// window aperture. Small sprites moving a fraction of a pixel a frame shimmered (HALL-FX, BUG-13 / VIS-9),
// so each pixel takes four rotated-grid samples across its own footprint (a sub-pixel smoothing the
// unmipped art lacks), and the sprite's outer pixel fades out on screen (its margin), so its quad's edge
// never crawls. The sprite's colour carries the hour's light and its lane-end fade (HallCityExterior).
Shader "NOPE/Hall City Traffic"
{
 Properties {
 [PerRendererData] _MainTex("Vehicle sprite",2D)="white"{}
 _WindowMask("Original window aperture",2D)="white"{}
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 TEXTURE2D(_WindowMask);SAMPLER(sampler_WindowMask);
 float4x4 _WindowToLocal;
 float4 _CanvasMetrics,_CanvasSize;
 struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;float2 aperture:TEXCOORD1;};
 V vert(A v){
 V o;float3 world=TransformObjectToWorld(v.position.xyz);
 o.position=TransformWorldToHClip(world);o.uv=v.uv;o.color=v.color;
 float2 local=mul(_WindowToLocal,float4(world,1)).xy;
 o.aperture=(local*_CanvasMetrics.x+_CanvasMetrics.yz)/_CanvasSize.xy;
 return o;
 }
 half4 frag(V v):SV_Target {
 // Four rotated-grid samples over this pixel's footprint in the sprite (a box filter of about a pixel).
 float2 dx=ddx(v.uv),dy=ddy(v.uv);
 half4 car=(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv+.125*dx+.375*dy)+
  SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv-.375*dx+.125*dy)+
  SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv+.375*dx-.125*dy)+
  SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv-.125*dx-.375*dy))*.25*v.color;
 // The margin: the sprite's outermost screen pixel fades to nothing.
 float2 edge=min(v.uv,1-v.uv)/max(fwidth(v.uv),1e-5);
 car.a*=saturate(min(edge.x,edge.y));
 float inside=step(0,v.aperture.x)*step(v.aperture.x,1)*step(0,v.aperture.y)*step(v.aperture.y,1);
 car.a*=SAMPLE_TEXTURE2D(_WindowMask,sampler_WindowMask,v.aperture).a*inside;
 return car;
 }
 ENDHLSL
 Pass {Tags {"LightMode"="Universal2D"} HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 ENDHLSL
 }
 Pass {Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 ENDHLSL
 }
 }
}
