// The living city behind the hall's windows and in the full-screen city view (CityView): the four
// painted states blended by the hour, depth parallax with the hall's pan, clouds, an airship, a bus and
// smoke, the windows switching on as the hall darkens, and headlights on three roads. Nothing toggles
// (HALL-FX, BUG-13): every window fades in once as the lit share (_HallCityLights.x, from the shift
// clock's hour) passes its moment, over the fade band HallCityExterior sizes to ~0.4 s at the clock's
// pace (_CityWindowFade; HallCity.WindowLevel mirrors it), and holds still while the clock does; the
// cell hash is never 0 (HallCity.WindowHash); headlights are at least ~2.7 px across (widened by the
// pixel's size), spread along their lane over 2.5 frames' travel (a 30 fps frame's motion blur, so a fast light
// in the city view glides over a pixel instead of blinking it on and off), and fade at their lanes' ends, and the
// flyers cross from off one edge to off the other.
// The rain path is gone (it was always dry: BUG-73d).
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
 TEXTURE2D(_CityDepth);TEXTURE2D(_Atmosphere);
 float4 _StateWeights;
 // The city's windows lighting as the hall darkens (HallLightingRig, a global: x 0 by day to 1 when every window is lit, y their brightness).
 float4 _HallCityLights;
 // The windows' fade band (a share of _HallCityLights.x; a global HallCityExterior sets: HallCity.NextFadeWidth).
 float _CityWindowFade;
 // A window cell's moment (HallCity.WindowHash, bit for bit): an integer (Wang) hash, never under 1/256, so no
 // window waits on a lit share of 0 and no crowd of cells shares one moment.
 float CityHash(float2 c)
 {
  uint2 u=(uint2)c;uint h=(u.x*73856093u)^(u.y*19349663u);
  h=(h^61u)^(h>>16);h*=9u;h^=h>>4;h*=0x27d4eb2du;h^=h>>15;
  return 1.0/256.0+(1.0-1.0/256.0)*((float)(h&0xFFFFFFu)/16777216.0);
 }
 float _Region,_CityPan,_CityDepthStrength,_CitySeconds,_CityMotion,_AtmospherePace,_HeadlightSize,_CityView;
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
 // A small flyer (the airship, the bus) drawn from the unmipped atlas many times smaller than its cell: one
 // sample a pixel sparkles as it moves (its windows and outline hop texels; at 1280 px wide the bus failed the
 // flicker check, integration), so it is the average of a 4 x 4 grid over the pixel's footprint (premultiplied).
 half4 FxFine(float2 uv,float2 center,float2 size,float2 cell)
 {
  float2 q=(uv-center)/size+.5;
  float2 dx=ddx(q),dy=ddy(q);
  half4 sum=0;
  [unroll] for(int i=0;i<4;i++)
  [unroll] for(int j=0;j<4;j++)
  {
   float2 o=(float2(i,j)+.5)*.25-.5;
   float2 p=q+o.x*dx+o.y*dy;
   half inside=step(0,p.x)*step(p.x,1)*step(0,p.y)*step(p.y,1);
   half4 c=SAMPLE_TEXTURE2D_LOD(_Atmosphere,sampler_MorningClear,(clamp(p,.002,.998)+cell)*.5,0);
   c.a*=inside;sum+=half4(c.rgb*c.a,c.a);
  }
  return half4(sum.rgb/max(sum.a,1e-4),sum.a*.0625);
 }
 // Where a flyer's centre is across the painting at frac f of its pass: from wholly off one edge to
 // wholly off the other (halfWidth past each edge), so it never pops in or out in view (BUG-13: the
 // hall's apertures did not hide every wrap).
 float Fly(float f,float halfWidth){return f*(1+2*halfWidth+.004)-halfWidth-.002;}
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
  half3 rgb=SAMPLE_TEXTURE2D(_MorningClear,sampler_MorningClear,uv).rgb*w.x+
   SAMPLE_TEXTURE2D(_NoonClear,sampler_MorningClear,uv).rgb*w.y+
   SAMPLE_TEXTURE2D(_EveningClear,sampler_MorningClear,uv).rgb*w.z+
   SAMPLE_TEXTURE2D(_NightClear,sampler_MorningClear,uv).rgb*w.w;
  float t=_CitySeconds*_CityMotion;
  // The city view (CityView) draws the panorama full screen: its sky and traffic move at a pace
  // that reads there, and its headlights are drawn larger; the hall windows keep 1 and 1.
  float tf=t*_AtmospherePace;
  // A headlight's spread in the lane's stretched space (d = (uv - head) * (3, 1)): its painted size, widened to
  // at least 1.35 screen px a side (a sub-pixel light twinkles as it crosses pixel centres, BUG-13; 0.9 still
  // toggled a few pixels by day under the flicker check, integration), dimmed
  // a little as it widens so a widened light keeps about its weight.
  float sigma0=sqrt(.5/(4000000/max(_HeadlightSize*_HeadlightSize,.0001)));
  float2 pixel=fwidth(uv);
  float2 sigma=max(float2(sigma0,sigma0),1.35*float2(3*pixel.x,pixel.y));
  half spreadDim=pow(saturate(sigma0*sigma0/(sigma.x*sigma.y)),.35);
  // The hour's light (HallCity.HourLight mirrors it).
  half3 light=half3(.95,.97,1)*w.x+half3(1,1,1)*w.y+half3(.75,.55,.55)*w.z+half3(.22,.29,.43)*w.w;
  half sky=1-smoothstep(.015,.06,Depth(uv));
  rgb=Over(rgb,Fx(uv,float2(Fly(frac(.16+tf*.0011),.175),.91),float2(.35,.22),float2(0,1)),.5*sky,light);
  // The second cloud is the first's picture again: full screen it reads as a ghost copy, so the city view draws one.
  rgb=Over(rgb,Fx(uv,float2(Fly(frac(.68+tf*.0007),.13),.86),float2(.26,.18),float2(0,1)),.3*sky*(1-_CityView),light);
  // Plumes remain attached to the existing industrial stacks.
  [unroll] for(int s=0;s<3;s++)
  {
   float2 origin=s==0?float2(.697,.708):s==1?float2(.731,.70):float2(.763,.69);
   float height=.075+s*.013;
   float2 smokeUV=uv;smokeUV.x+=sin(t*.35+s)*.0015*saturate((uv.y-origin.y)/height);
   rgb=Over(rgb,Fx(smokeUV,origin+float2(.009,height*.5),float2(.055,height),float2(1,1)),.23+.04*sin(t*.4+s),light);
  }
  rgb=Over(rgb,FxFine(uv,float2(Fly(frac(.32+tf*.0005),.022),.81),float2(.044,.095),float2(0,0)),.85,light);
  rgb=Over(rgb,FxFine(uv,float2(1-Fly(frac(.68+tf*.0013),.0115),.61),float2(.023,.055),float2(1,0)),.9,light);
  // Sparse warm building lights gently vary rather than flashing the whole image.
  half warm=step(rgb.b*1.5,rgb.r)*step(.22,rgb.r)*step(.1,Depth(uv));
  rgb+=warm*(w.z+w.w)*.018*(.5+.5*sin(floor(uv.x*390)+floor(uv.y*210)+t*.6));
  // The windows switch on one by one as the hall darkens (Saleh's demo 1018a: "need more lights to activate as it gets
  // darker"): the night painting's lit windows (warm, bright pixels), in cells each with its own moment along
  // _HallCityLights.x, each fading in once as the lit share passes it (HallCity.WindowLevel); fainter where the
  // night painting already shows them. Off while the share is 0 (by day).
  half3 lit=SAMPLE_TEXTURE2D(_NightClear,sampler_MorningClear,uv).rgb;
  half window=step(lit.b*1.25,lit.r)*smoothstep(.38,.62,lit.r)*step(.1,Depth(uv));
  float2 wcell=floor(uv*float2(240,120));float wh=CityHash(wcell);
  half on=step(1e-5,_HallCityLights.x)*saturate((_HallCityLights.x-wh*.97)/max(_CityWindowFade,.0005));
  rgb+=window*on*_HallCityLights.y*half3(1,.8,.48)*lit*(1-.7*w.w);
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
   // Motion spread (integration, 2026-10-21: in the city view, at 8 times the window's pace, a light crossed a
   // capture pixel in about 5 frames and the flicker check read its rise and fall as a toggle, up to 3 a pixel in
   // 10 s at H5): the light's spread along its lane covers 2.5 frames' travel at 30 fps (its covariance gains that
   // length along the lane), dimmed by the same ratio so it keeps its weight. A step of the light between two
   // frames is then under half its spread; the hall window's slow cars are barely touched, and a still city
   // (Reduced Motion, _CityMotion 0) not at all.
   float2 lane2=(b-a)*float2(3,1);
   float laneLength=max(length(lane2),1e-5);
   float2 along=lane2/laneLength;
   float blur=2.5*laneLength*(.007+lane*.002)*_AtmospherePace*_CityMotion/30.0;
   float2 s2=sigma*sigma;
   float sxx=s2.x+blur*blur*along.x*along.x,syy=s2.y+blur*blur*along.y*along.y,sxy=blur*blur*along.x*along.y;
   float det=max(sxx*syy-sxy*sxy,1e-20);
   half keep=sqrt(s2.x*s2.y/det);
   [unroll] for(int car=0;car<5;car++)
   {
    float travel=frac(tf*(.007+lane*.002)+car*.19+lane*.27);
    float2 head=lerp(a,b,travel);float2 d=(uv-head)*float2(3,1);
    // ...and come out from behind the building and go behind the next one (a short fade at the ends,
    // HallCity.LaneEndFade), in the hall window as in the city view: a lane's wrap never pops a light.
    half emerge=smoothstep(0,.06,travel)*(1-smoothstep(.94,1,travel));
    float q=(syy*d.x*d.x-2*sxy*d.x*d.y+sxx*d.y*d.y)/det;
    rgb+=half3(1,.74,.38)*exp(-.5*q)*spreadDim*keep*(.25+.55*w.w)*emerge;
   }
  }
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
