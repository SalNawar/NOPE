Shader "NOPE/Character Pose"
{
 Properties { [PerRendererData] _MainTex("Character",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline"}
  Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   Tags {"LightMode"="UniversalForward"}
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
   float4 _Color;
   float4 _CharacterPose;
   struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float4 colour:COLOR;};
   struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;float4 colour:COLOR;};
   V vert(A v) {V o;o.position=TransformObjectToHClip(v.position.xyz);o.uv=v.uv;o.colour=v.colour*_Color;return o;}
   float2 turn(float2 p,float2 pivot,float angle)
   {float s=sin(angle),c=cos(angle);p-=pivot;return pivot+float2(c*p.x-s*p.y,s*p.x+c*p.y);}
   float2 posed(float2 p)
   {
    // Shared canvas landmarks keep skin, sleeves, accessories, faces and hats
    // registered. The paper/photo stack keeps its original unposed material.
    float upper=1-smoothstep(760,1120,p.y);
    float head=1-smoothstep(410,490,p.y);
    float armHeight=smoothstep(440,520,p.y)*(1-smoothstep(700,860,p.y));
    float left=(1-smoothstep(325,425,p.x))*armHeight;
    float right=smoothstep(599,699,p.x)*armHeight;
    float2 q=p;
    q+=left*(turn(p,float2(365,510),clamp(_CharacterPose.y,0,.28))-p);
    q+=right*(turn(p,float2(659,510),clamp(_CharacterPose.z,-.28,0))-p);
    q+=head*(turn(p,float2(512,450),_CharacterPose.w)-p);
    q.x+=_CharacterPose.x*upper;
    return q;
   }
   half4 frag(V v):SV_Target
   {
    float2 destination=float2(v.uv.x*1024,(1-v.uv.y)*1536),source=destination;
    // Invert a small, continuous rig deformation. Full-canvas imported quads
    // supply room for the changing silhouette without editing shared sprites.
    // A Jacobian solve avoids the oscillating texture folds produced by simple
    // fixed-point sampling near a sleeve edge. Lower-body garments are outside
    // the arm rig, so broad skirts and robes retain their original silhouette.
    [unroll] for(int i=0;i<6;i++)
    {
     float2 position=posed(source),error=destination-position;
     float2 dx=posed(source+float2(1,0))-position,dy=posed(source+float2(0,1))-position;
     float determinant=dx.x*dy.y-dx.y*dy.x;
     float2 delta=float2(dy.y*error.x-dy.x*error.y,-dx.y*error.x+dx.x*error.y)/max(determinant,.25);
     source+=clamp(delta,-50,50);
    }
    float2 uv=float2(source.x/1024,1-source.y/1536);
    half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv)*v.colour;
    c.a*=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
    return c;
   }
   ENDHLSL
  }
 }
}
