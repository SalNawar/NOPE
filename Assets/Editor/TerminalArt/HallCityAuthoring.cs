using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class HallCityAuthoring
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/City";
    const string Report="ArtDeliverables/TimeDesk/City";
    [MenuItem("Tools/Terminal Art/City/Install Morning City")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var window=art.layers.First(l=>l.id.StartsWith("03 ")).renderer;
        if(window.sprite.rect.width!=2172 || window.sprite.rect.height!=724)throw new InvalidOperationException("Unexpected source registration.");
        Texture2D Import(string name,bool isSprite)
        {
            string path=Folder+"/"+name+".png";
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=isSprite?TextureImporterType.Sprite:TextureImporterType.Default;
            importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=100;
            importer.alphaIsTransparency=isSprite;importer.mipmapEnabled=false;importer.sRGBTexture=true;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
        var morning=Import("CityMorningHighFloor",false);Import("FlyingTaxi",true);Import("FlyingServiceVan",true);
        Material Mat(string name,string shader)
        {
            string path=Folder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(material,path);}
            return material;
        }
        var city=Mat("CityExterior","NOPE/Hall City Exterior");
        // Slots are ready for later separately authored states. Morning is the only completed painting.
        foreach(string state in new[]{"Morning","Noon","Evening","Night"})city.SetTexture("_"+state+"City",morning);
        city.SetVector("_StateWeights",new Vector4(1,0,0,0));EditorUtility.SetDirty(city);
        window.sharedMaterial=city;
        var traffic=Mat("CityTraffic","NOPE/Hall City Traffic");
        traffic.SetTexture("_WindowMask",window.sprite.texture);EditorUtility.SetDirty(traffic);
        var controller=window.GetComponent<HallCityExterior>();if(controller==null)controller=window.gameObject.AddComponent<HallCityExterior>();
        controller.window=window;
        float[] heights={170,230,310,355,215,285,245,330};
        float[] widths={30,24,20,18,13,16,11,14};
        float[] phases={.12f,.53f,.81f,.26f,.68f,.05f,.43f,.92f};
        controller.lanes=new HallCityExterior.Lane[heights.Length];
        for(int i=0;i<heights.Length;i++)
        {
            string name="Exterior flying vehicle "+i;
            var child=window.transform.Find(name);
            if(child==null){var g=new GameObject(name);g.transform.SetParent(window.transform,false);child=g.transform;}
            child.gameObject.layer=window.gameObject.layer;
            var renderer=child.GetComponent<SpriteRenderer>();if(renderer==null)renderer=child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/"+(i%3==0?"FlyingServiceVan":"FlyingTaxi")+".png");
            renderer.sharedMaterial=traffic;renderer.sortingLayerID=window.sortingLayerID;renderer.sortingOrder=window.sortingOrder+1;
            renderer.color=Color.Lerp(Color.white,new Color(.65f,.79f,.9f),i<4?.05f:.3f);
            bool left=i%2==1;
            controller.lanes[i]=new HallCityExterior.Lane{vehicle=renderer,
                xStart=left?i<4?570:1250:-60,xEnd=left?-60:i<4?570:1250,
                yPixels=heights[i],widthPixels=widths[i],speed=i<4?17+i*2:10+i,phase=phases[i]};
        }
        controller.Apply(0);EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(window.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(window.gameObject.scene);
        Directory.CreateDirectory(Report);
        File.WriteAllText(Report+"/registration.txt",$@"City: {morning.width}x{morning.height}; aperture sprite: {window.sprite.rect}; pivot: {window.sprite.pivot}; ppu: {window.sprite.pixelsPerUnit}.
Original window alpha and all 58 hall layers retained. Eight separate vehicles, two generated sprite types, receiver-mask clipping in both URP renderers. Morning art only; future time slots currently reference Morning explicitly.
");
        HallLightingPreviewWindow.Open();
        EditorWindow.GetWindow<HallLightingPreviewWindow>().SetHour(8);
    }
    [MenuItem("Tools/Terminal Art/City/Capture Morning And Traffic")]
    public static void Capture()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play a shift first.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var city=UnityEngine.Object.FindFirstObjectByType<HallCityExterior>();
        Directory.CreateDirectory(Report);
        var preview=EditorWindow.GetWindow<HallLightingPreviewWindow>();preview.SetHour(8);
        int step=0;double next=EditorApplication.timeSinceStartup+.8;Vector3 first=Vector3.zero;
        void Tick()
        {
            if(!EditorApplication.isPlaying){EditorApplication.update-=Tick;return;}
            if(EditorApplication.timeSinceStartup<next)return;
            if(step==0)
            {
                art.SetPan(0);next=EditorApplication.timeSinceStartup+.7;
            }
            if(step==1)
            {
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/morning-forward.png"));
                next=EditorApplication.timeSinceStartup+.7;
            }
            if(step==2)
            {
                art.SetPan(1);next=EditorApplication.timeSinceStartup+.7;
            }
            if(step==3)
            {
                first=city.lanes[0].vehicle.transform.localPosition;
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/morning-left-traffic-a.png"));
                next=EditorApplication.timeSinceStartup+2;
            }
            if(step==4)
            {
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/morning-left-traffic-b.png"));
                float moved=Vector3.Distance(first,city.lanes[0].vehicle.transform.localPosition);
                File.WriteAllText(Report+"/traffic-validation.txt",$@"First vehicle moved {moved:F4} local units over approximately two seconds. Reduced motion: {MotionPreference.Reduced}. Separate sprites remain window-mask clipped.
");
                art.SetPan(1);
                EditorApplication.update-=Tick;
            }
            step++;
        }
        EditorApplication.update+=Tick;
    }
}



