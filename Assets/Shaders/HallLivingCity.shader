Shader "NOPE/Hall Living City"
{
 Properties {
 [PerRendererData] _MainTex("Window registration",2D)="white"{}
 _Masks("Hall apertures",2D)="black"{}
 _Region("Window side",Float)=1
 _MorningClear("Morning clear",2D)="white"{}
 _NoonClear("Noon clear",2D)="white"{}
 _EveningClear("Evening clear",2D)="white"{}
 _NightClear("Night clear",2D)="white"{}
 _MorningRain("Morning rain",2D)="white"{}
 _NoonRain("Noon rain",2D)="white"{}
 _EveningRain("Evening rain",2D)="white"{}
 _NightRain("Night rain",2D)="white"{}
 _CityDepth("Registered parallax depth",2D)="black"{}
 _Atmosphere("Cloud smoke ship bus atlas",2D)="black"{}
 _AtmospherePace("Clouds, airship, bus and headlights pace (1: the hall window's)",Float)=1
 _HeadlightSize("Headlight size (1: the hall window's)",Float)=1
 _CityView("Full-screen city view (0: the hall window)",Float)=0
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_Masks);SAMPLER(sampler_Masks);
 TEXTURE2D(_MorningClear);SAMPLER(sampler_MorningClear);
 TEXTURE2D(_NoonClear);TEXTURE2D(_EveningClear);TEXTURE2D(_NightClear);
 TEXTURE2D(_MorningRain);TEXTURE2D(_NoonRain);TEXTURE2D(_EveningRain);TEXTURE2D(_NightRain);
 TEXTURE2D(_CityDepth);TEXTURE2D(_Atmosphere);
 float4 _StateWeights;
 float _Region,_CityPan,_CityRain,_CityDepthStrength,_CitySeconds,_CityMotion,_AtmospherePace,_HeadlightSize,_CityView;
 struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A v){V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.color=v.color;return o;}
 half Depth(float2 uv){return SAMPLE_TEXTURE2D(_CityDepth,sampler_MorningClear,saturate(uv)).r;}
 half4 Fx(float2 uv,float2 center,float2 size,float2 cell)
 {
  float2 q=(uv-center)/size+.5;
  half inside=step(0,q.x)*step(q.x,1)*step(0,q.y)*step(q.y,1);
  half4 c=SAMPLE_TEXTURE2D(_Atmosphere,sampler_MorningClear,(clamp(q,.002,.998)+cell)*.5);
  c.a*=inside;return c;
 }
 // Where a flyer's centre is across the painting at frac f of its pass: the hall window keeps f (its
 // apertures hide the wrap); the city view (CityView, _CityView 1) runs it from wholly off one edge
 // to wholly off the other (halfWidth past each edge), so it never pops in or out in view.
 float Fly(float f,float halfWidth){return lerp(f,f*(1+2*halfWidth+.004)-halfWidth-.002,_CityView);}
 half3 Over(half3 baseColor,half4 layer,half opacity,half3 light)
 {return lerp(baseColor,layer.rgb*light,saturate(layer.a*opacity));}
 half4 frag(V v):SV_Target
 {
  half4 mask=SAMPLE_TEXTURE2D(_Masks,sampler_Masks,v.uv);
  half aperture=_Region<1.5?mask.r:mask.g;
  clip(aperture-.001);
  float shift=_CityPan*(_Region<1.5?1:.4);
  float2 baseUV=saturate(v.uv+float2(shift*.3,0)),uv=baseUV;
  // Conservative depth reprojection: sky barely shifts, close roofs shift most.
  // Same depth coordinates across all eight states prevents blend swimming.
  [unroll] for(int i=0;i<4;i++)uv=saturate(baseUV+float2(shift*_CityDepthStrength*Depth(uv),0));
  half4 w=_StateWeights/max(dot(_StateWeights,half4(1,1,1,1)),.001);
  half3 clear=SAMPLE_TEXTURE2D(_MorningClear,sampler_MorningClear,uv).rgb*w.x+
   SAMPLE_TEXTURE2D(_NoonClear,sampler_MorningClear,uv).rgb*w.y+
   SAMPLE_TEXTURE2D(_EveningClear,sampler_MorningClear,uv).rgb*w.z+
   SAMPLE_TEXTURE2D(_NightClear,sampler_MorningClear,uv).rgb*w.w;
  half3 rain=SAMPLE_TEXTURE2D(_MorningRain,sampler_MorningClear,uv).rgb*w.x+
   SAMPLE_TEXTURE2D(_NoonRain,sampler_MorningClear,uv).rgb*w.y+
   SAMPLE_TEXTURE2D(_EveningRain,sampler_MorningClear,uv).rgb*w.z+
   SAMPLE_TEXTURE2D(_NightRain,sampler_MorningClear,uv).rgb*w.w;
  half wet=saturate(_CityRain);half3 rgb=lerp(clear,rain,wet);
  float t=_CitySeconds*_CityMotion;
  // The city view (CityView) draws the panorama full screen: its sky and traffic move at a pace
  // that reads there, and its headlights are drawn larger; the hall windows keep 1 and 1.
  float tf=t*_AtmospherePace;
  float glow=4000000/max(_HeadlightSize*_HeadlightSize,.0001);
  half3 light=half3(.95,.97,1)*w.x+half3(1,1,1)*w.y+half3(.75,.55,.55)*w.z+half3(.22,.29,.43)*w.w;
  half sky=1-smoothstep(.015,.06,Depth(uv));
  rgb=Over(rgb,Fx(uv,float2(Fly(frac(.16+tf*.0011),.175),.91),float2(.35,.22),float2(0,1)),.5*sky*(1-wet*.5),light);
  // The second cloud is the first's picture again: full screen it reads as a ghost copy, so the city view draws one.
  rgb=Over(rgb,Fx(uv,float2(frac(.68+tf*.0007),.86),float2(.26,.18),float2(0,1)),.3*sky*(1-_CityView),light);
  // Plumes remain attached to the existing industrial stacks.
  [unroll] for(int s=0;s<3;s++)
  {
   float2 origin=s==0?float2(.697,.708):s==1?float2(.731,.70):float2(.763,.69);
   float height=.075+s*.013;
   float2 smokeUV=uv;smokeUV.x+=sin(t*.35+s)*.0015*saturate((uv.y-origin.y)/height);
   rgb=Over(rgb,Fx(smokeUV,origin+float2(.009,height*.5),float2(.055,height),float2(1,1)),.23+.04*sin(t*.4+s),light);
  }
  rgb=Over(rgb,Fx(uv,float2(Fly(frac(.32+tf*.0005),.022),.81),float2(.044,.095),float2(0,0)),.85,light);
  rgb=Over(rgb,Fx(uv,float2(1-Fly(frac(.68+tf*.0013),.0115),.61),float2(.023,.055),float2(1,0)),.9,light);
  // Sparse warm building lights gently vary rather than flashing the whole image.
  half warm=step(rgb.b*1.5,rgb.r)*step(.22,rgb.r)*step(.1,Depth(uv));
  rgb+=warm*(w.z+w.w)*.018*(.5+.5*sin(floor(uv.x*390)+floor(uv.y*210)+t*.6));
  // Small moving headlight pairs follow three authored road segments.
  [unroll] for(int lane=0;lane<3;lane++)
  {
   float2 a=lane==0?float2(.035,.42):lane==1?float2(.12,.16):float2(.39,.375);
   float2 b=lane==0?float2(.295,.54):lane==1?float2(.29,.01):float2(.68,.45);
   // Full screen the cars keep to the painted roads, from the frame's edge or from behind a
   // building to behind a building or off the frame (the upper-left highway in from the left
   // edge to the tower; the lower highway from the tower to the bottom edge; the middle
   // viaduct between its two towers).
   a=lerp(a,lane==0?float2(-.01,.537):lane==1?float2(.152,.17):float2(.414,.395),_CityView);
   b=lerp(b,lane==0?float2(.239,.406):lane==1?float2(.33,-.02):float2(.594,.435),_CityView);
   [unroll] for(int car=0;car<5;car++)
   {
    float travel=frac(tf*(.007+lane*.002)+car*.19+lane*.27);
    float2 head=lerp(a,b,travel);float2 d=(uv-head)*float2(3,1);
    // ...and come out from behind the building and go behind the next one (a short fade at the ends).
    half emerge=lerp(1,smoothstep(0,.06,travel)*(1-smoothstep(.94,1,travel)),_CityView);
    rgb+=half3(1,.74,.38)*exp(-dot(d,d)*glow)*(.25+.55*w.w)*emerge;
   }
  }
  // A fine moving rain layer stays inside the exterior aperture.
  float2 ruv=uv*float2(360,60);ruv.x+=uv.y*12;
  float streak=step(.978,frac(ruv.x))*pow(saturate(1-frac(ruv.y+t*5+sin(floor(ruv.x))*7)),8);
  rgb=lerp(rgb,half3(.55,.64,.72),streak*wet*.2);
  return half4(rgb*v.color.rgb,aperture*v.color.a);
 }
 ENDHLSL
 Pass {Tags {"LightMode"="Universal2D"} HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 ENDHLSL}
 Pass {Tags {"LightMode"="UniversalForward"} HLSLPROGRAM
 #pragma target 3.5
 #pragma vertex vert
 #pragma fragment frag
 ENDHLSL}
 }
}
