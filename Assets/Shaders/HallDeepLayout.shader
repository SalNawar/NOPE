Shader "NOPE/Hall Deep Layout"
{
 Properties {
 [PerRendererData] _MainTex("Approved deeper room",2D)="white"{}
 _Masks("Left/front windows and floor receiver",2D)="black"{}
 _FixtureReference("Original fixture luminance",2D)="black"{}
 _UseFixtureReference("Use original fixture luminance",Float)=0
 _CityLeft("Complete left panorama",2D)="white"{}
 _CityFront("Connected front extension",2D)="white"{}
 _Region("0 architecture, 1 left city, 2 front city",Float)=0
 _CanvasSize("Canvas pixels",Vector)=(2172,724,0,0)
 _CeilingCutoff("Ceiling emission region",Float)=180
 _MorningShadow("Morning shadow",2D)="black"{}
 _NoonShadow("Noon shadow",2D)="black"{}
 _EveningShadow("Evening shadow",2D)="black"{}
 _NightTint("Night tint (HallLightingSO.hallNightTint)",Color)=(.24,.32,.49,1)
 _PoolColour("Fixture pool colour (HallLightingSO.fixtureColour)",Color)=(1,.79,.54,1)
 _PoolStrength("Fixture pool strength (HallLightingSO.hallPoolStrength)",Float)=0
 _EmissionGain("Diffuser emission gain (HallLightingSO.hallEmissionGain)",Float)=1
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 HLSLINCLUDE
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "HallFloorShadowGeometry.hlsl"
 TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
 TEXTURE2D(_Masks);SAMPLER(sampler_Masks);
 TEXTURE2D(_FixtureReference);SAMPLER(sampler_FixtureReference);
 float _UseFixtureReference;
 TEXTURE2D(_CityLeft);SAMPLER(sampler_CityLeft);
 TEXTURE2D(_CityFront);SAMPLER(sampler_CityFront);
 TEXTURE2D(_MorningShadow);SAMPLER(sampler_MorningShadow);
 TEXTURE2D(_NoonShadow);TEXTURE2D(_EveningShadow);
 float4 _StateWeights,_CanvasSize;float _LightingAmount,_CloudMotion,_Region,_CityPan,_CeilingCutoff;
 // Per fixture (HallBakedLighting): its lit share; its centre (xy, uv) and wall glow radii (zw, uv); its floor pool (xy centre, zw radii, uv).
 float _FixtureLevels[16];float4 _FixtureCenters[16];float4 _FixturePools[16];int _ScheduledFixtureCount;
 float4 _NightTint,_PoolColour;float _PoolStrength,_EmissionGain;
 // A lamp's soft ellipse (the four-state bake's falloff): 1 at its centre, 0 at its rim.
 float Ellipse(float2 uv,float4 shape){float2 d=(uv-shape.xy)/max(shape.zw,1e-4);float r=dot(d,d);return r<1?exp(-r*3)*(1-r)*(1-r):0;}
 struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
 V vert(A v){V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.color=v.color;return o;}
 half4 frag(V v):SV_Target {
  half4 mask=SAMPLE_TEXTURE2D(_Masks,sampler_Masks,v.uv);
  float2 p=float2(v.uv.x*_CanvasSize.x,(1-v.uv.y)*_CanvasSize.y);
  if(_Region>.5) {
   float left=_Region<1.5?1:0;
   // Clean exterior artwork moves beneath stationary hall apertures.
   float shift=_CityPan*(left>.5?1:.4);
   float2 cityUV=saturate(v.uv+float2(shift,0));
   float2 side=cityUV,front=cityUV;
   half3 rgb=left>.5?SAMPLE_TEXTURE2D(_CityLeft,sampler_CityLeft,saturate(side)).rgb:
    SAMPLE_TEXTURE2D(_CityFront,sampler_CityFront,saturate(front)).rgb;
      // Morning is the sole authored exterior. Temporary cycle tint keeps the
   // day/night slider coherent while matching compositions are still pending.
   half3 cityTint=half3(1,.94,.86)*_StateWeights.x+half3(1,1,1)*_StateWeights.y+
    half3(.81,.59,.46)*_StateWeights.z+half3(.24,.32,.49)*_StateWeights.w;
   rgb*=lerp(half3(1,1,1),cityTint,saturate(_LightingAmount));
   return half4(rgb*v.color.rgb,(left>.5?mask.r:mask.g)*v.color.a);
  }
  half4 source=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,v.uv)*v.color;
  source.a*=1-max(mask.r,mask.g);
  half3 tint=half3(1,.94,.86)*_StateWeights.x+half3(1,1,1)*_StateWeights.y+
   half3(.81,.59,.46)*_StateWeights.z+_NightTint.rgb*_StateWeights.w;
  half shadow=mask.b>.001?HallGeometricShadow(p,_StateWeights):0;
  // Only bright neutral ceiling diffusers emit; no detached glow columns.
  // Pale stone must not become a new emitter. Use the registered original
  // painting to retain only the pre-existing bright ceiling fixtures.
  half3 fixture=lerp(source.rgb,SAMPLE_TEXTURE2D(_FixtureReference,sampler_FixtureReference,v.uv).rgb,saturate(_UseFixtureReference));
  half neutral=min(fixture.r,min(fixture.g,fixture.b));
  // Every ceiling diffuser across the whole canvas (BUG-25d: the old x < 36% cut left the left pier's lamp dark).
  half emission=smoothstep(.76,.94,neutral)*step(p.y,_CeilingCutoff)*(1-max(mask.r,mask.g));
  // Ceiling diffusers use the nearest registered fixture's live schedule,
  // including the lighting switch, stagger, custom sunset and strike flicker.
  // Each lit fixture also throws real light (BUG-25a): a glow on the wall
  // around it (not on the window glass) and a pool on the floor receivers below.
  float level=0,nearest=1e20;half pools=0;half open=1-max(mask.r,mask.g);
  [loop] for(int i=0;i<_ScheduledFixtureCount;i++)
  {
   float2 d=(v.uv-_FixtureCenters[i].xy)*_CanvasSize.xy;float distance=dot(d,d);
   if(distance<nearest){nearest=distance;level=_FixtureLevels[i];}
   pools+=(Ellipse(v.uv,_FixtureCenters[i])*open+Ellipse(v.uv,_FixturePools[i])*mask.b)*_FixtureLevels[i];
  }
  // Unlit glass stays visible; bright emission is separate from the background.
  source.rgb*=lerp(1,.38,emission*(1-level));
  half3 illumination=tint*(1-shadow*mask.b)+_PoolColour.rgb*(pools*_PoolStrength);
  half3 lit=source.rgb*illumination+emission*half3(1,.78,.5)*(.45*level*_EmissionGain);
  source.rgb=lerp(source.rgb,lit,saturate(_LightingAmount));
  return source;
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
