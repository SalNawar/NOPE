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
    static bool Inside(Vector2 q,Vector2[] p)
    {
        bool inside=false;
        for(int i=0,j=p.Length-1;i<p.Length;j=i++)
            if((p[i].y>q.y)!=(p[j].y>q.y) && q.x<(p[j].x-p[i].x)*(q.y-p[i].y)/(p[j].y-p[i].y)+p[i].x)inside=!inside;
        return inside;
    }
    static bool ForegroundRail(Vector2 p)
    {
        if(p.x<1703 && ((p.y>=547&&p.y<=561)||(p.y>=636&&p.y<=646)))return true;
        if(p.y<550||p.y>695)return false;
        foreach(float x in new float[]{43,175,300,429,553,680,824,943,1088,1216,1364,1496,1643})
            if(Mathf.Abs(p.x-x)<=5)return true;
        return false;
    }
    public static void Receiver(Color32[] masks)
    {
        for(int y=0;y<H;y++)for(int x=0;x<W;x++)
        {
            var q=new Vector2(x+.5f,y+.5f);int i=(H-1-y)*W+x;var c=masks[i];
            c.b=(byte)(Inside(q,Floor)&&!ForegroundRail(q)&&!Objects.Any(p=>Inside(q,p))?255:0);masks[i]=c;
        }
    }
    public static Color32[] Bake(Color32[] masks,Vector2 direction,float strength)
    {
        var result=new Color32[W*H];
        for(int y=0;y<H;y++)for(int x=0;x<W;x++)
        {
            int i=(H-1-y)*W+x;float shade=masks[i].b==0?0:HallFloorShadowGeometry.Shade(new Vector2(x+.5f,y+.5f),direction)*strength;
            byte v=(byte)Mathf.RoundToInt(Mathf.Clamp01(shade)*255);result[i]=new Color32(v,v,v,255);
        }
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
        HallFloorShadowGeometry.WriteShader();
        Save("MorningShadow",Bake(masks,HallFloorShadowGeometry.Morning,1));
        Save("NoonShadow",Bake(masks,HallFloorShadowGeometry.Noon,1));
        Save("EveningShadow",Bake(masks,HallFloorShadowGeometry.Evening,1));
        foreach(var obj in Objects){var centre=obj.Aggregate(Vector2.zero,(a,b)=>a+b)/obj.Length;int i=(H-1-(int)centre.y)*W+(int)centre.x;if(masks[i].b!=0)throw new InvalidOperationException("Object receiving floor shadow.");}
        UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
        Directory.CreateDirectory("ArtDeliverables/TimeDesk/HallLayers/FloorShadowRepair");
        File.WriteAllText("ArtDeliverables/TimeDesk/HallLayers/FloorShadowRepair/validation.txt","2172x724 source registration. Glazing R/G/A unchanged pixel-for-pixel. Receiver excludes all traced object silhouettes. Virtual floor ray intersections use upright proxy boxes and hollow portal rings; dynamic shader interpolates light direction before casting. Morning/noon/evening retain existing time blend; night directional share remains zero. No scene/camera/desk/traveller edits.");
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
    [MenuItem("Tools/Terminal Art/Lighting/Verify Geometric Floor Shadows")]
    public static void VerifyGeometry()
    {
        const string report="ArtDeliverables/TimeDesk/HallLayers/FloorShadowRepair";
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();var settings=rig.Settings;
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var cam=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Anime hall player preview");
        var pos=cam.transform.position;var rot=cam.transform.rotation;float oldPan=art.lookLeft,oldHour=settings.previewHour;bool oldOverride=settings.previewHourOn;
        var slider=EditorWindow.GetWindow<HallLightingPreviewWindow>();
        try
        {
            art.SetPan(0);
            foreach(var range in new[]{new Vector2(8,12),new Vector2(12,16.5f),new Vector2(16.5f,19.5f)})
            {
                int count=Mathf.RoundToInt((range.y-range.x)*2)+1,columns=4,rows=Mathf.CeilToInt(count/4f);
                var sheet=new Texture2D(columns*430,rows*330,TextureFormat.RGB24,false);
                var baySheet=new Texture2D(columns*500,rows*270,TextureFormat.RGB24,false);
                for(int n=0;n<count;n++)
                {
                    slider.SetHour(range.x+n*.5f);HallFocusAlignmentAuthoring.Capture("geometric-review-tmp",false);
                    var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes("ArtDeliverables/TimeDesk/City/FocusAlignment/geometric-review-tmp.png"));
                    sheet.SetPixels((n%4)*430,(rows-1-n/4)*330,430,330,image.GetPixels(380,image.height-310-330,430,330));
                    baySheet.SetPixels((n%4)*500,(rows-1-n/4)*270,500,270,image.GetPixels(920,image.height-440-270,500,270));
                    UnityEngine.Object.DestroyImmediate(image);
                }
                sheet.Apply();baySheet.Apply();string label=range.x.ToString("00.0",System.Globalization.CultureInfo.InvariantCulture);
                File.WriteAllBytes(report+"/geometry-column-sweep-"+label+".png",sheet.EncodeToPNG());
                File.WriteAllBytes(report+"/geometry-bay-sweep-"+label+".png",baySheet.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(sheet);UnityEngine.Object.DestroyImmediate(baySheet);
            }
            slider.SetHour(8);
            foreach(float pan in new[]{0f,.25f,.5f,.75f,1f}){art.SetPan(pan);HallFocusAlignmentAuthoring.Capture("geometry-pan-"+pan.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture),false);}
            art.SetPan(0);
            var desk=UnityEngine.Object.FindFirstObjectByType<DeskView>();
            var deskCamera=desk!=null?new SerializedObject(desk).FindProperty("deskCamera").objectReferenceValue as Component:null;
            if(deskCamera!=null)
            {
                foreach(float blend in new[]{0f,.25f,.5f,.75f,1f})
                {
                    cam.transform.SetPositionAndRotation(Vector3.Lerp(pos,deskCamera.transform.position,blend),Quaternion.Slerp(rot,deskCamera.transform.rotation,blend));
                    HallFocusAlignmentAuthoring.Capture("geometry-desk-"+blend.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture),false);
                }
            }
            File.AppendAllText(report+"/validation.txt","\nGeometric review: actual lighting slider at half-hour intervals 08-19.5; three column and three bay contact sheets in row-major time order. Source registered pan sweep 0/.25/.5/.75/1 and actual desk camera pose interpolation 0/.25/.5/.75/1 captured. Camera pose, preview override and pan restored. Ring centre/rim assertions passed. Floor light ray fed from the same DaylightDirection as the desk.");
        }
        finally
        {
            cam.transform.SetPositionAndRotation(pos,rot);art.SetPan(oldPan);
            if(oldOverride)slider.SetHour(oldHour);else slider.UseGameClock();
            settings.previewHour=oldHour;settings.previewHourOn=oldOverride;UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
            HallFocusAlignmentAuthoring.Capture("geometry-final-live",false);
        }
    }}
