Shader "NOPE/Hall White Crowd Fade"
{
 Properties {_BaseMap("Authored silhouette alpha",2D)="white"{} _Tint("Palette / opacity",Color)=(1,1,1,.52)}
 SubShader
 {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  HLSLINCLUDE
  #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
  TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
  half4 _Tint;float _FootPixelY;float4x4 _ArtToLocal;float4 _Canvas;
  struct A {float4 p:POSITION;float2 uv:TEXCOORD0;};
  struct V {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 pixel:TEXCOORD1;};
  V vert(A v)
  {
   V o;o.p=TransformObjectToHClip(v.p.xyz);o.uv=v.uv;
   float3 local=mul(_ArtToLocal,float4(TransformObjectToWorld(v.p.xyz),1)).xyz;
   o.pixel=float2(local.x*_Canvas.x+_Canvas.y,_Canvas.z-local.y*_Canvas.x);return o;
  }
  half4 frag(V v):SV_Target
  {
   if(_Canvas.w>.5)
   {
    // Source-registered brass rails cover the legs of the balcony extras.
    if(v.pixel.y>282||(v.pixel.y>245&&v.pixel.y<252)||(v.pixel.y>264&&v.pixel.y<270))clip(-1);
    if(v.pixel.y>245){float posts[7]={1467,1564,1652,1781,1860,1955,2090};for(int n=0;n<7;n++)if(abs(v.pixel.x-posts[n])<4)clip(-1);}
   }
   else if(v.pixel.x<1703 && v.pixel.y>547)
   {
    // Ground crowds remain behind the foreground brass railing.
    if((v.pixel.y>547&&v.pixel.y<561)||(v.pixel.y>636&&v.pixel.y<646))clip(-1);
    float posts[13]={43,175,300,429,553,680,824,943,1088,1216,1364,1496,1643};
    for(int n=0;n<13;n++)if(abs(v.pixel.x-posts[n])<5 && v.pixel.y<695)clip(-1);
   }
   if(_Canvas.w<.5 && _FootPixelY<416)
   {
    // Far crowds are behind the rear portal hoops, rather than painted over their rims.
    float2 left=(v.pixel-float2(963,365))/float2(45,38);
    float2 right=(v.pixel-float2(1215,365))/float2(45,38);
    if(abs(length(left)-1)<.16 || abs(length(right)-1)<.16)clip(-1);
    if(v.pixel.y>397&&v.pixel.y<421&&((v.pixel.x>918&&v.pixel.x<1008)||(v.pixel.x>1170&&v.pixel.x<1260)))clip(-1);
   }
   half a=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,v.uv).a;
   half glass=1;
   if(_Canvas.w<.5 && _FootPixelY<416 && v.pixel.y>367 && v.pixel.y<432 &&
      ((v.pixel.x>866&&v.pixel.x<1049)||(v.pixel.x>1135&&v.pixel.x<1305)))glass=.42;
   return half4(_Tint.rgb,_Tint.a*glass*smoothstep(.48,.54,a));
  }
  ENDHLSL
  Pass {Tags {"LightMode"="Universal2D"} HLSLPROGRAM
  #pragma target 3.0
  #pragma vertex vert
  #pragma fragment frag
  ENDHLSL }
  Pass {Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
  #pragma target 3.0
  #pragma vertex vert
  #pragma fragment frag
  ENDHLSL }
 }
}
