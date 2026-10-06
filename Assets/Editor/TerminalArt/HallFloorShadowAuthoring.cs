using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Source-pixel registration: origin at the top left of HallDeepMorning (2172 x 724).
// These are technical receiver/occlusion maps, never a repaint of the illustration.
public static class HallFloorShadowAuthoring
{
    const int W=2172,H=724;
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/DeepRoom";
    static Vector2[] P(params float[] xy) => Enumerable.Range(0,xy.Length/2).Select(i=>new Vector2(xy[2*i],xy[2*i+1])).ToArray();
    static readonly Vector2[] Floor=P(0,699,550,510,790,350,1340,340,2172,684,2172,724,0,724);
    static readonly Vector2[][] Objects={
        P(568,493,574,373,649,373,683,332,790,315,790,448,703,487,673,510,568,510), // actual stair/pier silhouette
        P(706,455,724,452,728,456,728,483,710,491), // bin beside stair
        P(866,364,900,352,1014,352,1049,365,1049,415,1014,432,900,432,866,415),
        P(1135,365,1164,350,1274,350,1305,365,1305,415,1274,432,1164,432,1135,415),
        P(869,516,940,474,1202,474,1303,517,1303,595,1202,638,940,638,869,595),
        P(1824,521,2172,520,2172,700,2040,700,1824,674) // bench, bin and kiosk
    };
    struct Caster
    {
        public Vector2[] footprint; public float height,opacity;
        public Caster(Vector2[] p,float h,float o){footprint=p;height=h;opacity=o;}
    }
    static readonly Caster[] Casters={
        new Caster(P(701,481,784,445,789,449,706,486),1.1f,.20f),
        new Caster(P(866,408,900,428,1014,428,1049,408,1038,403,877,403),.65f,.22f),
        new Caster(P(1135,408,1164,428,1274,428,1305,408,1294,403,1146,403),.65f,.22f),
        new Caster(P(869,589,940,635,1202,635,1303,589,1290,578,882,578),1f,.24f),
        new Caster(P(1833,667,1950,689,2016,675,1998,665,1850,660),.5f,.18f)
    };
    static bool Inside(Vector2 q,Vector2[] p)
    {
        bool inside=false;
        for(int i=0,j=p.Length-1;i<p.Length;j=i++)
            if((p[i].y>q.y)!=(p[j].y>q.y) && q.x<(p[j].x-p[i].x)*(q.y-p[i].y)/(p[j].y-p[i].y)+p[i].x)inside=!inside;
        return inside;
    }
    static float Distance(Vector2 q,Vector2[] p)
    {
        if(Inside(q,p))return 0;
        float d=float.MaxValue;
        for(int i=0;i<p.Length;i++)
        {
            Vector2 a=p[i],v=p[(i+1)%p.Length]-a;
            d=Mathf.Min(d,(q-a-v*Mathf.Clamp01(Vector2.Dot(q-a,v)/v.sqrMagnitude)).magnitude);
        }
        return d;
    }
    static float Cross(Vector2 a,Vector2 b,Vector2 c) => (b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
    static Vector2[] Sweep(Vector2[] p,Vector2 direction)
    {
        var points=p.Concat(p.Select(v=>v+direction)).OrderBy(v=>v.x).ThenBy(v=>v.y).ToArray();
        var hull=new System.Collections.Generic.List<Vector2>();
        foreach(var v in points){while(hull.Count>=2 && Cross(hull[hull.Count-2],hull[hull.Count-1],v)<=0)hull.RemoveAt(hull.Count-1);hull.Add(v);}
        int lower=hull.Count;
        for(int i=points.Length-2;i>=0;i--){var v=points[i];while(hull.Count>lower && Cross(hull[hull.Count-2],hull[hull.Count-1],v)<=0)hull.RemoveAt(hull.Count-1);hull.Add(v);}
        hull.RemoveAt(hull.Count-1);return hull.ToArray();
    }
    public static void Receiver(Color32[] masks)
    {
        for(int y=0;y<H;y++)for(int x=0;x<W;x++)
        {
            var q=new Vector2(x+.5f,y+.5f);int i=(H-1-y)*W+x;var c=masks[i];
            c.b=(byte)(Inside(q,Floor)&&!Objects.Any(p=>Inside(q,p))?255:0);masks[i]=c;
        }
    }
    public static Color32[] Bake(Color32[] masks,Vector2 direction,float strength)
    {
        var result=new Color32[W*H];var shade=new float[W*H];
        void Cast(Caster c)
        {
            Vector2 delta=direction*c.height;var hull=Sweep(c.footprint,delta);
            int x0=Mathf.Max(0,Mathf.FloorToInt(hull.Min(v=>v.x)-5)),x1=Mathf.Min(W-1,Mathf.CeilToInt(hull.Max(v=>v.x)+5));
            int y0=Mathf.Max(0,Mathf.FloorToInt(hull.Min(v=>v.y)-5)),y1=Mathf.Min(H-1,Mathf.CeilToInt(hull.Max(v=>v.y)+5));
            for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
            {
                int i=(H-1-y)*W+x;if(masks[i].b==0)continue;var q=new Vector2(x+.5f,y+.5f);
                float travelled=Distance(q,c.footprint)/delta.magnitude;
                float feather=Mathf.Lerp(1.3f,5f,Mathf.Clamp01(travelled));
                float edge=1-Mathf.SmoothStep(0,1,Mathf.Clamp01(Distance(q,hull)/feather));
                // Full contact at the foot. Fade only toward the remote end, never at the origin.
                float fade=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1f,travelled));
                shade[i]=Mathf.Max(shade[i],edge*fade*c.opacity*strength);
            }
        }
        foreach(var c in Casters)Cast(c);
        // Individually registered brass feet, not an evenly spaced synthetic row.
        foreach(float x in new float[]{43,175,300,429,553,680,824,943,1088,1216,1364,1496,1643})
            Cast(new Caster(P(x-5,690,x+5,690,x+6,695,x-6,695),.24f,.28f));
        foreach(float x in new float[]{1697,2070})Cast(new Caster(P(x-6,698,x+6,698,x+6,704,x-6,704),.24f,.28f));
        for(int i=0;i<result.Length;i++){byte v=(byte)Mathf.RoundToInt(Mathf.Clamp01(shade[i])*255);result[i]=new Color32(v,v,v,255);}
        return result;
    }
    static Color32[] Read(string path)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false,true);t.LoadImage(File.ReadAllBytes(path));
        if(t.width!=W||t.height!=H)throw new InvalidOperationException("Shadow source registration changed.");
        var data=t.GetPixels32();UnityEngine.Object.DestroyImmediate(t);return data;
    }
    static void Save(string name,Color32[] data)
    {
        var t=new Texture2D(W,H,TextureFormat.RGBA32,false,true);t.SetPixels32(data);t.Apply();
        string path=Folder+"/"+name+".png";File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
    }
    [MenuItem("Tools/Terminal Art/Lighting/Repair Hall Floor Shadows")]
    public static void Repair()
    {
        var masks=Read(Folder+"/DeepRoomMasks.png");var original=(Color32[])masks.Clone();Receiver(masks);
        for(int i=0;i<masks.Length;i++)if(masks[i].r!=original[i].r||masks[i].g!=original[i].g||masks[i].a!=original[i].a)throw new InvalidOperationException("Glazing changed.");
        Save("DeepRoomMasks",masks);
        Save("MorningShadow",Bake(masks,new Vector2(100,42),1));
        Save("NoonShadow",Bake(masks,new Vector2(35,15),.65f));
        Save("EveningShadow",Bake(masks,new Vector2(135,52),1.1f));
        foreach(var obj in Objects){var centre=obj.Aggregate(Vector2.zero,(a,b)=>a+b)/obj.Length;int i=(H-1-(int)centre.y)*W+(int)centre.x;if(masks[i].b!=0)throw new InvalidOperationException("Object receiving floor shadow.");}
        UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
        Directory.CreateDirectory("ArtDeliverables/TimeDesk/HallLayers/FloorShadowRepair");
        File.WriteAllText("ArtDeliverables/TimeDesk/HallLayers/FloorShadowRepair/validation.txt","2172x724 source registration. Glazing R/G/A unchanged pixel-for-pixel. Receiver excludes all traced object silhouettes. Casts sweep traced ground footprints, remain connected at origins, soften and fade at distance. Morning/noon/evening retain existing time blend; night directional share remains zero. No scene/camera/desk/traveller edits.");
        Debug.Log("Rebuilt hall floor shadows from registered footprints; glazing and composition preserved.");
    }
    [MenuItem("Tools/Terminal Art/Lighting/Capture Floor Shadow Review")]
    public static void Review()
    {
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();var settings=rig.Settings;
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        float oldHour=settings.previewHour,oldPan=art.lookLeft;bool oldPreview=settings.previewHourOn;
        try
        {
            foreach(int pan in new[]{0,1})foreach(float hour in new[]{8f,12f,16.5f,22f})
            {
                settings.previewHourOn=true;settings.previewHour=hour;art.SetPan(pan);
                string file="floor-shadow-"+(pan==0?"front":"left")+"-"+hour.ToString("00.0",System.Globalization.CultureInfo.InvariantCulture);
                HallFocusAlignmentAuthoring.Capture(file,false);
                File.Copy("ArtDeliverables/TimeDesk/City/FocusAlignment/"+file+".png","ArtDeliverables/TimeDesk/HallLayers/FloorShadowRepair/"+file+".png",true);
            }
        }
        finally{settings.previewHour=oldHour;settings.previewHourOn=oldPreview;art.SetPan(oldPan);HallFocusAlignmentAuthoring.Capture("floor-shadow-live",false);}
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }
    static void Detail(string input,string output,int x,int y,int width,int height)
    {
        var source=new Texture2D(2,2);source.LoadImage(File.ReadAllBytes(input));
        var crop=new Texture2D(width,height,TextureFormat.RGB24,false);
        crop.SetPixels(source.GetPixels(x,source.height-y-height,width,height));crop.Apply();
        File.WriteAllBytes(output,crop.EncodeToPNG());UnityEngine.Object.DestroyImmediate(source);UnityEngine.Object.DestroyImmediate(crop);
    }
    [MenuItem("Tools/Terminal Art/Lighting/Capture Column Diagnosis")]
    public static void Diagnosis() => ColumnDetail("before");
    [MenuItem("Tools/Terminal Art/Lighting/Capture Corrected Column")]
    public static void CorrectedColumn() => ColumnDetail("after");
    static void ColumnDetail(string revision)
    {
        const string report="ArtDeliverables/TimeDesk/HallLayers/FloorShadowRepair";
        Directory.CreateDirectory(report);
        Detail(Folder+"/HallDeepMorning.png",report+"/painted-column-detail.png",560,310,340,250);
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();var settings=rig.Settings;
        float oldHour=settings.previewHour;bool oldPreview=settings.previewHourOn;
        try
        {
            settings.previewHourOn=true;
            foreach(float hour in revision=="before"?new[]{8f}:new[]{8f,10f,12f,14f,16.5f,19f,22f})
            {
                settings.previewHour=hour;
                string file="column-"+revision+(hour==8?"":"-"+hour.ToString("00.0",System.Globalization.CultureInfo.InvariantCulture));
                HallFocusAlignmentAuthoring.Capture(file,false);
                Detail("ArtDeliverables/TimeDesk/City/FocusAlignment/"+file+".png",report+"/"+file+".png",380,310,430,330);
            }
        }
        finally{settings.previewHour=oldHour;settings.previewHourOn=oldPreview;UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();}
    }
}
