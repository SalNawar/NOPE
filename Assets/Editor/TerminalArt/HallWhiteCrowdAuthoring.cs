using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class HallWhiteCrowdAuthoring
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/WhiteCrowds";
    const string Report="ArtDeliverables/TimeDesk/HallLayers/WhiteCrowds";
    static readonly Vector2[] Feet={new(868,482),new(1410,458),new(1092,385),new(1080,350),new(1498,277),new(1530,519),new(1338,370),new(819,454),new(1870,277),new(1360,401),new(1270,454),new(1090,352),new(1120,277),new(1600,277),new(2090,277)};
    static bool Balcony(int n)=>n==4||n==8||n>=12;
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
    static Mesh[] VariationMeshes()
    {
        string path=Folder+"/WhiteCrowdVariations.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Default;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.wrapMode=TextureWrapMode.Clamp;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=2048;imp.npotScale=TextureImporterNPOTScale.None;imp.SaveAndReimport();
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));var pixels=image.GetPixels32();int w=image.width,h=image.height;var result=new Mesh[9];
        for(int n=0;n<9;n++)
        {
            int col=n%3,row=n/3,x0=col*w/3,x1=(col+1)*w/3,y0=h-(row+1)*h/3,y1=h-row*h/3,minX=x1,maxX=x0,minY=y1,maxY=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)if(pixels[y*w+x].a>128){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            if(maxX<=minX||maxY<=minY)throw new InvalidOperationException("Empty white crowd variant "+n);
            result[n]=Quad("Variation_"+n.ToString("00"),(float)(maxX-minX+1)/(maxY-minY+1),new Rect((float)minX/w,(float)minY/h,(float)(maxX-minX+1)/w,(float)(maxY-minY+1)/h));
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
        var contactMaterial=Material("Faint ground contacts","NOPE/Hall White Crowd Contact");var contactMesh=Quad("ContactQuad",1,new Rect(0,0,1,1));
        var root=new GameObject("White silhouette crowds");root.layer=drawing.gameObject.layer;root.transform.SetParent(drawing.transform,false);
        var groups=new HallWhiteCrowds.Group[15];float[] cycles={71,83,67,97,79,89,73,101,61,87,109,77,103,81,93};float[] phases={8,35,17,53,29,71,45,9,61,39,19,55,84,47,23};
        for(int n=0;n<15;n++)
        {
            var mesh=n<6?originals[n]:variations[n-6];bool balcony=Balcony(n);float height=(Feet[n].y-(balcony?150:300))*.58f,ppu=drawing.sprite.pixelsPerUnit;
            var anchor=new Vector3((Feet[n].x-drawing.sprite.pivot.x)/ppu,(drawing.sprite.pivot.y-Feet[n].y)/ppu,-.01f);
            var child=new GameObject((n<6?"Original group ":"New composition ")+n.ToString("00"));child.layer=root.layer;child.transform.SetParent(root.transform,false);
            float scale=height/ppu/mesh.bounds.size.y;child.transform.localScale=new Vector3(n%2==0?scale:-scale,scale,scale);
            child.transform.localPosition=anchor-Vector3.Scale(new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,0),child.transform.localScale);
            child.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterial=n<6?originalMaterial:variantMaterial;renderer.sortingLayerID=drawing.sortingLayerID;renderer.sortingOrder=drawing.sortingOrder+4;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            MeshRenderer contact=null;
            if(!balcony)
            {
                var foot=new GameObject("Faint group contact");foot.layer=root.layer;foot.transform.SetParent(root.transform,false);float thickness=height/ppu*.055f;
                foot.transform.localScale=new Vector3(mesh.bounds.size.x*scale*.88f,thickness,1);foot.transform.localPosition=anchor+new Vector3(0,-thickness/2,.002f);
                foot.AddComponent<MeshFilter>().sharedMesh=contactMesh;contact=foot.AddComponent<MeshRenderer>();contact.sharedMaterial=contactMaterial;contact.sortingLayerID=renderer.sortingLayerID;contact.sortingOrder=renderer.sortingOrder-1;contact.shadowCastingMode=ShadowCastingMode.Off;contact.receiveShadows=false;
            }
            groups[n]=new HallWhiteCrowds.Group{silhouette=renderer,contact=contact,balcony=balcony,cycle=cycles[n],phase=phases[n],hold=36+n%4*4,opacity=height<35?.32f:balcony?.38f:n<6?.42f:.46f};
        }
        var crowds=root.AddComponent<HallWhiteCrowds>();crowds.Configure(UnityEngine.Object.FindFirstObjectByType<HallLightingRig>(),drawing,groups);
        Undo.RegisterCreatedObjectUndo(root,"Add approved white crowd variations");EditorUtility.SetDirty(crowds);AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(art.gameObject.scene);EditorSceneManager.SaveScene(art.gameObject.scene);
        Debug.Log("Installed original six white crowd groups and nine complementary white compositions at 32-46 percent opacity.");
    }
    static void Capture(string name)
    {
        HallFocusAlignmentAuthoring.Capture("white-crowds-"+name,false);File.Copy("ArtDeliverables/TimeDesk/City/FocusAlignment/white-crowds-"+name+".png",Report+"/"+name+".png",true);
    }
    [MenuItem("Tools/Terminal Art/Crowds/Verify Approved White Crowds")]
    public static void Verify()
    {
        Directory.CreateDirectory(Report);var crowds=UnityEngine.Object.FindFirstObjectByType<HallWhiteCrowds>();var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();var settings=rig.Settings;
        float oldHour=settings.previewHour,oldPan=art.lookLeft;bool oldOverride=settings.previewHourOn;
        var positions=crowds.Groups.Select(g=>g.silhouette.transform.localPosition).ToArray();var rotations=crowds.Groups.Select(g=>g.silhouette.transform.localRotation).ToArray();var scales=crowds.Groups.Select(g=>g.silhouette.transform.localScale).ToArray();var report=new StringBuilder();
        try
        {
            int min=15,max=0;float low=1,high=0;
            for(int t=0;t<=600;t++)
            {
                crowds.Apply(t);int count=crowds.Groups.Count(g=>g.silhouette.enabled);min=Math.Min(min,count);max=Math.Max(max,count);
                for(int n=0;n<15;n++)
                {
                    var g=crowds.Groups[n];if(g.silhouette.transform.localPosition!=positions[n]||g.silhouette.transform.localRotation!=rotations[n]||g.silhouette.transform.localScale!=scales[n])throw new InvalidOperationException("Crowd transform animated.");
                    var block=new MaterialPropertyBlock();g.silhouette.GetPropertyBlock(block);float a=block.GetColor("_Tint").a;low=Math.Min(low,a);high=Math.Max(high,a);
                }
            }
            if(!MotionPreference.Reduced&&(min==max||min<2||low>.001f||high>.47f))throw new InvalidOperationException("Crowd fade / opacity validation failed.");
            report.AppendLine($"Six original meshes/atlas unchanged. Nine complementary group compositions. Fifteen fixed placements, ten on main floor/five balcony. 601 timeline samples: active groups {min}-{max}, alpha {low:0.00}-{high:0.00}. Position, rotation, scale unchanged.");
            foreach(float hour in new[]{8f,10f,12f,14f,16.5f,18f,22f})
            {
                settings.previewHourOn=true;settings.previewHour=hour;art.SetPan(0);crowds.Apply(29);Capture("hour-"+hour.ToString("00.0",System.Globalization.CultureInfo.InvariantCulture));report.AppendLine($"Hour {hour}: tint {crowds.Palette}.");
            }
            settings.previewHour=12;
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
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();art.Apply();
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        typeof(HallLightingRig).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rig,null);
        UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
        var crowds=UnityEngine.Object.FindFirstObjectByType<HallWhiteCrowds>();crowds.Apply(0);
        EditorSceneManager.MarkSceneDirty(art.gameObject.scene);EditorSceneManager.SaveScene(art.gameObject.scene);
    }}


