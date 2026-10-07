using System;
using System.IO;
using System.Linq;
using System.Globalization;
using UnityEditor;
using UnityEngine;

// A calibrated virtual floor behind the 2D illustration. Ground points and
// vertical heights are reconstructed from source pixels, then rays intersect
// the actual proxy volumes. Ring holes are preserved. No footprint extrusion.
public static class HallFloorShadowGeometry
{
    public const float Horizon=300, Focal=1000, EyeHeight=3;
    static Vector2 Slope(HallBakedCycle.Weights4 weights){var d=HallBakedLighting.DaylightDirection(weights);return new Vector2(d.x/-d.y,d.z/-d.y);}
    public static readonly Vector2 Morning=Slope(new HallBakedCycle.Weights4(1,0,0,0));
    public static readonly Vector2 Noon=Slope(new HallBakedCycle.Weights4(0,1,0,0));
    public static readonly Vector2 Evening=Slope(new HallBakedCycle.Weights4(0,0,1,0));
    public struct Box {public Vector3 min,max;public float opacity;}
    public struct Ring {public Vector2 ground;public float centreHeight,radiusX,radiusY,thickness,opacity;}
    public static Vector2 Ground(Vector2 pixel)
    {
        float depth=Focal*EyeHeight/(pixel.y-Horizon);
        return new Vector2((pixel.x-1090)*depth/Focal,depth);
    }
    public static Vector2 Pixel(Vector2 ground) => new Vector2(1090+Focal*ground.x/ground.y,Horizon+Focal*EyeHeight/ground.y);
    static float Height(float groundY,float topY)=>EyeHeight*(groundY-topY)/(groundY-Horizon);
    static Box Volume(float x0,float y0,float x1,float y1,float top,float opacity)
    {
        var a=Ground(new Vector2(x0,y0));var b=Ground(new Vector2(x1,y1));
        return new Box{min=new Vector3(Mathf.Min(a.x,b.x),0,Mathf.Min(a.y,b.y)),max=new Vector3(Mathf.Max(a.x,b.x),Height((y0+y1)*.5f,top),Mathf.Max(a.y,b.y)),opacity=opacity};
    }
    static Ring Hoop(float x,float centreY,float rx,float ry,float footY)
    {
        var ground=Ground(new Vector2(x,footY));float scale=ground.y/Focal;
        return new Ring{ground=ground,centreHeight=(footY-centreY)*scale,radiusX=rx*scale,radiusY=ry*scale,thickness=.14f,opacity=.26f};
    }
    public static readonly Ring[] Rings={Hoop(1092,479,76,70,570),Hoop(963,365,43,36,416),Hoop(1215,365,43,36,416)};
    static Box[] MakeBoxes()
    {
        var boxes=new System.Collections.Generic.List<Box>{
            // The pier face runs along constant world X: source foot from (701,481) to (789,449).
            Volume(701,481,789,449,309,.23f),
            Volume(978,561,1200,575,535,.19f),
            Volume(920,409,1002,419,397,.17f),
            Volume(1172,409,1254,419,397,.17f),
            Volume(706,486,728,488,453,.18f)
        };
        // Brass supports cast slender vertical silhouettes rather than broad bay-sized strips.
        foreach(var p in new[]{new Vector2(869,596),new Vector2(941,632),new Vector2(1202,632),new Vector2(1303,596),new Vector2(866,418),new Vector2(901,430),new Vector2(1014,430),new Vector2(1049,418),new Vector2(1135,418),new Vector2(1164,430),new Vector2(1274,430),new Vector2(1305,418)})
            boxes.Add(Volume(p.x-3,p.y,p.x+3,p.y+1,p.y-(p.y>500?104:50),.19f));
        foreach(float x in new float[]{43,175,300,429,553,680,824,943,1088,1216,1364,1496,1643})
            boxes.Add(Volume(x-3,691,x+3,694,550,.20f));
        return boxes.ToArray();
    }
    public static readonly Box[] Boxes=MakeBoxes();
    static bool Slab(float p,float d,float min,float max,ref float enter,ref float leave)
    {
        if(Mathf.Abs(d)<.00001f)return p>=min&&p<=max;
        float a=(min-p)/d,b=(max-p)/d;enter=Mathf.Max(enter,Mathf.Min(a,b));leave=Mathf.Min(leave,Mathf.Max(a,b));return leave>=enter;
    }
    public static float BoxCast(Vector2 p,Vector2 slope,Box box)
    {
        float enter=0,leave=box.max.y;
        float penumbra=.025f*box.max.y;
        if(!Slab(p.x,-slope.x,box.min.x-penumbra,box.max.x+penumbra,ref enter,ref leave)||!Slab(p.y,-slope.y,box.min.z-penumbra,box.max.z+penumbra,ref enter,ref leave))return 0;
        return Mathf.SmoothStep(0,1,Mathf.Clamp01((leave-enter)/(.06f+penumbra)))*box.opacity;
    }
    public static float RingCast(Vector2 p,Vector2 slope,Ring ring)
    {
        if(Mathf.Abs(slope.y)<.0001f)return 0;
        float h=(p.y-ring.ground.y)/slope.y;if(h<=0)return 0;
        float x=p.x-slope.x*h;
        float radius=new Vector2((x-ring.ground.x)/ring.radiusX,(h-ring.centreHeight)/ring.radiusY).magnitude;
        float softness=.025f+.014f*h;
        return (1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(ring.thickness,ring.thickness+softness,Mathf.Abs(radius-1))))*ring.opacity;
    }
    public static float Shade(Vector2 pixel,Vector2 slope)
    {
        if(pixel.y<=Horizon+1)return 0;
        var ground=Ground(pixel);float opacity=0;
        foreach(var box in Boxes)opacity=Mathf.Max(opacity,BoxCast(ground,slope,box));
        foreach(var ring in Rings)opacity=Mathf.Max(opacity,RingCast(ground,slope,ring));
        return opacity;
    }
    static string N(float v)=>v.ToString("0.########",CultureInfo.InvariantCulture);
    static string V4(float a,float b,float c,float d)=>"float4("+N(a)+","+N(b)+","+N(c)+","+N(d)+")";
    public static void WriteShader()
    {
        var s=new System.Text.StringBuilder("// Generated by HallFloorShadowGeometry. Source 2172x724, horizon (1090,300).\n");
        s.Append("static const float4 HallBoxMin["+Boxes.Length+"]={");s.Append(string.Join(",",Boxes.Select(b=>V4(b.min.x,b.min.y,b.min.z,0))));s.Append("};\n");
        s.Append("static const float4 HallBoxMax["+Boxes.Length+"]={");s.Append(string.Join(",",Boxes.Select(b=>V4(b.max.x,b.max.y,b.max.z,b.opacity))));s.Append("};\n");
        s.Append("static const float4 HallRingPosition[3]={");s.Append(string.Join(",",Rings.Select(r=>V4(r.ground.x,r.ground.y,r.centreHeight,r.thickness))));s.Append("};\n");
        s.Append("static const float4 HallRingSize[3]={");s.Append(string.Join(",",Rings.Select(r=>V4(r.radiusX,r.radiusY,r.opacity,0))));s.Append("};\n");
        s.Append(@"
float4 _HallShadowRay;
float HallSlab(float p,float d,float lo,float hi,inout float enter,inout float leave)
{
 if(abs(d)<.00001)return step(lo,p)*step(p,hi);
 float2 t=float2(lo-p,hi-p)/d;enter=max(enter,min(t.x,t.y));leave=min(leave,max(t.x,t.y));return step(enter,leave);
}
float HallGeometricShadow(float2 pixel,float4 weights)
{
 if(pixel.y<=301)return 0;
 float day=weights.x+weights.y+weights.z;if(day<.0001)return 0;
 // Interpolate light direction first: the silhouettes move continuously rather than double-exposing three static masks.
 float2 slope=_HallShadowRay.xy;
 // Avoid a degenerate exactly edge-on ring for a single intermediate frame.
 slope.y=abs(slope.y)<.005?(slope.y<0?-.005:.005):slope.y;
 float depth=3000/(pixel.y-300);float2 p=float2((pixel.x-1090)*depth/1000,depth);float shade=0;
");
        s.Append("[loop] for(int i=0;i<"+Boxes.Length+";i++){\n");
        s.Append(@"
 float4 a=HallBoxMin[i],b=HallBoxMax[i];float enter=0,leave=b.y;float penumbra=.025*b.y;
 float hit=HallSlab(p.x,-slope.x,a.x-penumbra,b.x+penumbra,enter,leave);
 hit*=HallSlab(p.y,-slope.y,a.z-penumbra,b.z+penumbra,enter,leave);
 shade=max(shade,hit*smoothstep(0,.06+penumbra,max(0,leave-enter))*b.w);
 }
 [unroll] for(int j=0;j<3;j++){
 float4 r=HallRingPosition[j],size=HallRingSize[j];float h=(p.y-r.y)/slope.y;
 float radius=length(float2((p.x-slope.x*h-r.x)/size.x,(h-r.z)/size.y));
 float soft=.025+.014*max(0,h);float hit=(1-smoothstep(r.w,r.w+soft,abs(radius-1)))*step(.0001,h);
 shade=max(shade,hit*size.z);
 }
 return shade*day;
}
");
        File.WriteAllText("Assets/Shaders/HallFloorShadowGeometry.hlsl",s.ToString());AssetDatabase.ImportAsset("Assets/Shaders/HallFloorShadowGeometry.hlsl",ImportAssetOptions.ForceSynchronousImport);
        // Check the open ring centre against its rim before receiver occlusion.
        foreach(var ring in Rings)
        {
            Vector2 centre=ring.ground+Morning*ring.centreHeight;
            if(RingCast(centre,Morning,ring)>.001f)throw new InvalidOperationException("Ring hole filled.");
            Vector2 rim=ring.ground+new Vector2(ring.radiusX,0)+Morning*ring.centreHeight;
            if(RingCast(rim,Morning,ring)<.2f)throw new InvalidOperationException("Ring rim missing.");
        }
    }
}
