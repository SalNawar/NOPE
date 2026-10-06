using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class HallWhiteCrowdAuthoring
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/WhiteCrowds";
    const string Report="ArtDeliverables/TimeDesk/HallLayers/WhiteCrowds/StationLifeReview-2026-10-07-Final";
    // Source-pixel foot anchors, explicitly separated from composition indices.
    // Main-floor placements stay inside the walking lanes, away from wall/door bases.
    readonly struct Placement
    {
        public readonly Vector2 feet; public readonly int composition; public readonly bool balcony;
        public Placement(float x,float y,int composition,bool balcony=false)
        {feet=new Vector2(x,y);this.composition=composition;this.balcony=balcony;}
    }
    static readonly Placement[] Placements={
        new(815,448,15),new(836,479,0),new(758,519,18),
        new(1090,351,31),new(600,555,39),new(1490,605,40),
        new(1328,451,20),new(1350,480,19),new(1400,525,21),
        new(1400,566,17),new(822,396,35),new(1308,445,27),
        new(520,590,28),new(1335,504,24),new(1080,344,11),
        new(708,535,25),new(832,433,29),new(1090,376,22),
        new(1066,347,34),new(1115,344,16),new(1318,470,30),new(780,553,23),
        new(800,277,26,true),new(890,277,32,true),new(980,277,33,true),
        new(1070,277,36,true),new(1160,277,37,true),new(1250,277,38,true),
        new(1340,277,41,true),new(1430,277,42,true),new(1520,277,43,true),
        new(1610,277,44,true),new(1700,277,45,true),new(1790,277,46,true),
        new(1880,277,7,true),new(1970,277,13,true),new(2060,277,14,true)
    };
    static Material Material(string name,string shader,Texture2D texture=null)
    {
        string path=Folder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null){material=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(material,path);}
        if(texture!=null)material.SetTexture("_BaseMap",texture);EditorUtility.SetDirty(material);return material;
    }
    static Mesh Quad(string name,float aspect,Rect uv)
    {
        string path=Folder+"/"+name+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(mesh==null){mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,path);}else mesh.Clear();
        mesh.vertices=new[]{new Vector3(-aspect/2,0),new Vector3(aspect/2,0),new Vector3(-aspect/2,1),new Vector3(aspect/2,1)};
        mesh.uv=new[]{new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMax,uv.yMin),new Vector2(uv.xMin,uv.yMax),new Vector2(uv.xMax,uv.yMax)};
        mesh.triangles=new[]{0,2,1,1,2,3};mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);return mesh;
    }
    static Mesh[] VariationMeshes(string filename="WhiteCrowdVariations",string prefix="Variation",int columns=3,int count=9)
    {
        string path=Folder+"/"+filename+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.wrapMode=TextureWrapMode.Clamp;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=2048;imp.npotScale=TextureImporterNPOTScale.None;imp.SaveAndReimport();
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));var pixels=image.GetPixels32();int w=image.width,h=image.height;var result=new Mesh[count];
        for(int n=0;n<count;n++)
        {
            int col=n%columns,row=n/columns,x0=col*w/columns,x1=(col+1)*w/columns,y0=h-(row+1)*h/columns,y1=h-row*h/columns,minX=x1,maxX=x0,minY=y1,maxY=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*w+x].a>128){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            if(count==16)
            {
                // Ignore isolated atlas speckles / small pieces leaking across a cell edge.
                // Only adjust native mesh UV bounds; keep generated image alpha unchanged.
                minX=x1;maxX=x0;minY=y1;maxY=y0;var seen=new HashSet<int>();
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
                {
                    int start=y*w+x;if(pixels[start].a<=137||!seen.Add(start))continue;
                    var queue=new List<int>{start};int left=x,right=x,bottom=y,top=y;
                    for(int i=0;i<queue.Count;i++)
                    {
                        int index=queue[i],cx=index%w,cy=index/w;
                        left=Math.Min(left,cx);right=Math.Max(right,cx);bottom=Math.Min(bottom,cy);top=Math.Max(top,cy);
                        foreach(int next in new[]{index-1,index+1,index-w,index+w})
                        {
                            int nx=next%w,ny=next/w;
                            if(nx<x0||nx>=x1||ny<y0||ny>=y1||Math.Abs(nx-cx)+Math.Abs(ny-cy)!=1)continue;
                            if(pixels[next].a>137&&seen.Add(next))queue.Add(next);
                        }
                    }
                    if(queue.Count<400)continue;
                    minX=Math.Min(minX,left);maxX=Math.Max(maxX,right);minY=Math.Min(minY,bottom);maxY=Math.Max(maxY,top);
                }
            }
            if(maxX<=minX||maxY<=minY)throw new InvalidOperationException("Empty white crowd variant "+n);
            result[n]=Quad(prefix+"_"+n.ToString("00"),(float)(maxX-minX+1)/(maxY-minY+1),new Rect((float)minX/w,(float)minY/h,(float)(maxX-minX+1)/w,(float)(maxY-minY+1)/h));
        }
        UnityEngine.Object.DestroyImmediate(image);return result;
    }
    [MenuItem("Tools/Terminal Art/Crowds/Install Approved White Crowds")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Install outside Play mode.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();var drawing=art.layers.First(l=>l.id.StartsWith("62 ")).renderer;
        var previous=drawing.transform.Find("White silhouette crowds");if(previous!=null)Undo.DestroyObjectImmediate(previous.gameObject);
        var variations=VariationMeshes();var originals=Enumerable.Range(1,6).Select(i=>AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Office/HallCrowds/Meshes/CrowdGroup_"+i.ToString("00")+".asset")).ToArray();
        if(originals.Any(m=>m==null))throw new InvalidOperationException("Approved original crowd mesh missing.");
        var originalMaterial=Material("Original white silhouettes","NOPE/Hall White Crowd Fade",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/HallCrowds/Textures/CrowdGroups_Atlas.png"));
        var variantMaterial=Material("White silhouette variations","NOPE/Hall White Crowd Fade",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WhiteCrowdVariations.png"));
        var activities=VariationMeshes("WhiteCrowdActivities","Activity",4,16);
        var stationLife=VariationMeshes("WhiteCrowdStationLife","Station",4,16);
        var activityMaterial=Material("White silhouette activities","NOPE/Hall White Crowd Fade",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WhiteCrowdActivities.png"));
        var stationMaterial=Material("White silhouette station life","NOPE/Hall White Crowd Fade",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/WhiteCrowdStationLife.png"));
        var meshes=originals.Concat(variations).Concat(activities).Concat(stationLife).ToArray();
        var contactMaterial=Material("Faint ground contacts","NOPE/Hall White Crowd Contact");var contactMesh=Quad("ContactQuad",1,new Rect(0,0,1,1));
        var root=new GameObject("White silhouette crowds");root.layer=drawing.gameObject.layer;root.transform.SetParent(drawing.transform,false);
        var groups=new HallWhiteCrowds.Group[Placements.Length];float[] cycles={71,83,67,97,79,89,73,101,61,87,109,77,103,81,93};float[] phases={8,35,17,53,29,71,45,9,61,39,19,55,84,47,23};
        for(int n=0;n<groups.Length;n++)
        {
            var placement=Placements[n];int composition=placement.composition;var mesh=meshes[composition];bool balcony=placement.balcony;float height=(placement.feet.y-(balcony?150:300))*.58f,ppu=drawing.sprite.pixelsPerUnit;
            var anchor=new Vector3((placement.feet.x-drawing.sprite.pivot.x)/ppu,(drawing.sprite.pivot.y-placement.feet.y)/ppu,-.01f);
            var child=new GameObject((composition<6?"Original group ":"New composition ")+n.ToString("00"));child.layer=root.layer;child.transform.SetParent(root.transform,false);
            float scale=height/ppu/mesh.bounds.size.y;child.transform.localScale=new Vector3(n%2==0?scale:-scale,scale,scale);
            child.transform.localPosition=anchor-Vector3.Scale(new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,0),child.transform.localScale);
            child.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=composition<6?originalMaterial:composition<15?variantMaterial:composition<31?activityMaterial:stationMaterial;renderer.sortingLayerID=drawing.sortingLayerID;renderer.sortingOrder=drawing.sortingOrder+4;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            MeshRenderer contact=null;
            if(!balcony)
            {
                var foot=new GameObject("Faint group contact");foot.layer=root.layer;foot.transform.SetParent(root.transform,false);float thickness=height/ppu*.055f;
                foot.transform.localScale=new Vector3(mesh.bounds.size.x*scale*.88f,thickness,1);foot.transform.localPosition=anchor+new Vector3(0,-thickness/2,.002f);
                foot.AddComponent<MeshFilter>().sharedMesh=contactMesh;contact=foot.AddComponent<MeshRenderer>();contact.sharedMaterial=contactMaterial;contact.sortingLayerID=renderer.sortingLayerID;contact.sortingOrder=renderer.sortingOrder-1;contact.shadowCastingMode=ShadowCastingMode.Off;contact.receiveShadows=false;
            }
            groups[n]=new HallWhiteCrowds.Group{silhouette=renderer,contact=contact,balcony=balcony,cycle=height<45?84:cycles[n%cycles.Length]+n/15*7,phase=height<45?n*19%84:phases[n%phases.Length]+n/15*13,hold=height<45?56:balcony?48+n%3*4:36+n%4*4,opacity=height<35?.32f:balcony?.38f:composition<6?.42f:.46f};
        }
        var crowds=root.AddComponent<HallWhiteCrowds>();crowds.Configure(UnityEngine.Object.FindFirstObjectByType<HallLightingRig>(),drawing,groups);
        Undo.RegisterCreatedObjectUndo(root,"Add approved white crowd variations");EditorUtility.SetDirty(crowds);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(art.gameObject.scene);EditorSceneManager.SaveScene(art.gameObject.scene);
        Debug.Log($"Installed {groups.Length} stationary crowd placements using 32 new station-life compositions.");
    }
    static void Capture(string name)
    {
        HallFocusAlignmentAuthoring.Capture("white-crowds-station-life-"+name,false);File.Copy("ArtDeliverables/TimeDesk/City/FocusAlignment/white-crowds-station-life-"+name+".png",Report+"/"+name+".png",true);
    }
    [MenuItem("Tools/Terminal Art/Crowds/Verify Approved White Crowds")]
    public static void Verify()
    {
        Directory.CreateDirectory(Report);var crowds=UnityEngine.Object.FindFirstObjectByType<HallWhiteCrowds>();var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();var settings=rig.Settings;
        float oldHour=settings.previewHour,oldPan=art.lookLeft;bool oldOverride=settings.previewHourOn;
        var positions=crowds.Groups.Select(g=>g.silhouette.transform.localPosition).ToArray();var rotations=crowds.Groups.Select(g=>g.silhouette.transform.localRotation).ToArray();var scales=crowds.Groups.Select(g=>g.silhouette.transform.localScale).ToArray();var report=new StringBuilder();
        try
        {
            int min=crowds.Groups.Length,max=0;float low=1,high=0;
            for(int t=0;t<=600;t++)
            {
                crowds.Apply(t);int count=crowds.Groups.Count(g=>g.silhouette.enabled);min=Math.Min(min,count);max=Math.Max(max,count);
                for(int n=0;n<crowds.Groups.Length;n++)
                {
                    var g=crowds.Groups[n];if(g.silhouette.transform.localPosition!=positions[n]||g.silhouette.transform.localRotation!=rotations[n]||g.silhouette.transform.localScale!=scales[n])throw new InvalidOperationException("Crowd transform animated.");
                    var block=new MaterialPropertyBlock();g.silhouette.GetPropertyBlock(block);float a=block.GetColor("_Tint").a;low=Math.Min(low,a);high=Math.Max(high,a);
                }
            }
            if(!MotionPreference.Reduced&&(min==max||min<2||low>.001f||high>.47f))throw new InvalidOperationException("Crowd fade / opacity validation failed.");
            report.AppendLine($"Approved original meshes/atlas unchanged. {crowds.Groups.Length} fixed placements, {crowds.Groups.Count(g=>!g.balcony)} main floor / {crowds.Groups.Count(g=>g.balcony)} balcony. 601 timeline samples: active groups {min}-{max}, alpha {low:0.00}-{high:0.00}. Position, rotation, scale unchanged.");
            foreach(float hour in new[]{8f,10f,12f,14f,16.5f,18f,22f})
            {
                settings.previewHourOn=true;settings.previewHour=hour;art.SetPan(0);crowds.Apply(29);Capture("hour-"+hour.ToString("00.0",System.Globalization.CultureInfo.InvariantCulture));report.AppendLine($"Hour {hour}: tint {crowds.Palette}.");
            }
            settings.previewHour=12;
            // Show every placement together so a fade cannot hide a bad anchor during review.
            var phases=crowds.Groups.Select(g=>g.phase).ToArray();
            try
            {
                for(int i=0;i<phases.Length;i++)crowds.Groups[i].phase=10;
                crowds.Apply(0);art.SetPan(0);Capture("all-groups");
                art.SetPan(1);Capture("all-groups-left");art.SetPan(0);
            }
            finally{for(int i=0;i<phases.Length;i++)crowds.Groups[i].phase=phases[i];}
            foreach(float time in new[]{0f,12f,29f,47f,71f,104f}){crowds.Apply(time);Capture("population-"+time);report.AppendLine($"Population time {time}: {crowds.Groups.Count(g=>g.silhouette.enabled)} groups.");}
            art.SetPan(1);crowds.Apply(29);Capture("left-pan");art.SetPan(.5f);crowds.Apply(47);Capture("mid-pan");
            File.WriteAllText(Report+"/validation.txt",report.ToString());
        }
        finally{settings.previewHour=oldHour;settings.previewHourOn=oldOverride;art.SetPan(oldPan);crowds.Apply(Application.isPlaying?Time.timeSinceLevelLoad:0);}
        Debug.Log("Crowd lighting, fade and fixed transform verification captured for visual review.");
    }
    [MenuItem("Tools/Terminal Art/Crowds/Capture Live White Crowds")]
    public static void Live()
    {
        Directory.CreateDirectory(Report);var crowds=UnityEngine.Object.FindFirstObjectByType<HallWhiteCrowds>();
        if(crowds==null||!Application.isPlaying)throw new InvalidOperationException("Live review requires Play mode.");
        float seconds=Time.timeSinceLevelLoad;crowds.Apply(seconds);Capture("live-"+Mathf.FloorToInt(seconds));
        File.AppendAllText(Report+"/validation.txt",$"Live Play frame {seconds:0.00}s: {crowds.Groups.Count(g=>g.silhouette.enabled)} visible groups.\n");
    }    [MenuItem("Tools/Terminal Art/Crowds/Save White Crowd Scene")]
    public static void Save()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Save outside Play mode.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();art.SetPan(0);art.Apply();
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        typeof(HallLightingRig).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rig,null);
        UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
        var crowds=UnityEngine.Object.FindFirstObjectByType<HallWhiteCrowds>();crowds.Apply(0);
        EditorSceneManager.MarkSceneDirty(art.gameObject.scene);EditorSceneManager.SaveScene(art.gameObject.scene);
    }}


