Shader "NOPE/Layered Hall Lighting"
{
 Properties {
  [PerRendererData] _MainTex("Albedo",2D)="white"{}
  _NormalTex("Camera-space surface normals",2D)="bump"{}
  _Color("Tint",Color)=(1,1,1,1)
  [PerRendererData] _RendererColor("Sprite tint",Color)=(1,1,1,1)
  _Mode("0 Hall, 1 City, 2 furnishing",Float)=0
  _Atmosphere("Hall ambient integration",Range(0,1))=0
  _ClipCorner("Atlas exclusion corner",Vector)=(1,1,0,0)
 }
 SubShader {
  Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
  Pass {
   Tags {"LightMode"="UniversalForward"}
   Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
   HLSLPROGRAM
   #pragma vertex Vert
   #pragma fragment Frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
   TEXTURE2D(_NormalTex);SAMPLER(sampler_NormalTex);
   CBUFFER_START(UnityPerMaterial)
   float4 _Color;float4 _RendererColor;float _Mode;float _Atmosphere;float4 _ClipCorner;
   CBUFFER_END
   float _HallEvening,_HallSunShift,_HallClock;
   float4 _HallLightDirection;
   struct A{float4 p:POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
   struct V{float4 p:SV_POSITION;float2 uv:TEXCOORD0;half4 c:COLOR;};
   V Vert(A i){V o;o.p=TransformObjectToHClip(i.p.xyz);o.uv=i.uv;o.c=i.c*_Color*_RendererColor;return o;}
   float Spot(float2 p,float2 c,float2 size){return exp(-dot((p-c)/size,(p-c)/size)*3);}
   half4 Frag(V i):SV_Target{
    if(i.uv.x>_ClipCorner.x && i.uv.y>_ClipCorner.y)discard;
    if((_Mode>2.5 && _Mode<3.5)||(_Mode>4.1 && _Mode<4.4))clip(.5-i.uv.x);
    half4 a=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.c;
    float e=saturate(_HallEvening);float2 p=float2(i.uv.x,1-i.uv.y);
    if(_Mode>4.5){float2 q=i.uv*2-1;if(_Mode>6.5)return half4(a.rgb,a.a*exp(-dot(q,q)*4)*saturate((1-dot(q,q))*5));if(_Mode>5.5){clip(1-dot(q,q));a.rgb*=.8+.2*sin(p.y*18+p.x*12+_HallClock);}return a;}
    if(_Mode>.5 && _Mode<1.5){
     half3 city=a.rgb*lerp(half3(1,1,1),half3(.18,.23,.40),e);
     float windows=step(.71,frac(p.x*781))*step(.82,frac(p.y*647))*step(.35,p.y);
     // City windows retain the painted structure; no screen-space dot grid.
     return half4(city,a.a);
    }
    half3 n=_Mode>1.5?normalize(lerp(half3(0,0,1),half3(0,1,0),_Mode>3.5?1:(_Mode>2.5?smoothstep(.52,.72,p.y):0))):normalize(SAMPLE_TEXTURE2D(_NormalTex,sampler_NormalTex,i.uv).rgb*2-1);
    float d=saturate(dot(n,normalize(_HallLightDirection.xyz)));
    half3 ambient=lerp(half3(.52,.52,.55),half3(.16,.18,.28),e);
    half3 daylight=lerp(half3(.70,.61,.46),half3(.10,.12,.22),e);
    float floorMask=smoothstep(.55,.82,n.y);
    float phase=(p.x+p.y*.56+_HallSunShift*.08)*15;
    float bars=smoothstep(.03,.08,abs(frac(phase)-.5));
    float shadow=lerp(1,lerp(.52,1,bars),floorMask*(1-e)*.65);
    half3 result=a.rgb*(ambient+daylight*(.25+.75*d)*shadow);
    half luminance=dot(result,half3(.2126,.7152,.0722));
    result=lerp(result,lerp(luminance.xxx,half3(.51,.46,.39)*(ambient+daylight*.6),.35),_Atmosphere);
    result+=a.rgb*floorMask*pow(saturate(sin(phase*.45)),20)*.12*(1-e);
    if(_Mode>2.1 && _Mode<2.4){float glass=smoothstep(.60,.83,min(a.r,min(a.g,a.b)));result+=glass*half3(.8,.58,.28)*(.3+e);}
    return half4(result,a.a);
   }
   ENDHLSL
  }
 }
}
