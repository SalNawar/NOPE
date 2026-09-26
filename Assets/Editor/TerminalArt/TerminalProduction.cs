using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Composed 2.5D art scene: baked architectural lighting, independent sprite furnishings,
// and the approved 3D desk. No gameplay or source scene is saved by this builder.
public static class TerminalProduction
{
    const string Folder="Assets/Art/Office/TerminalProduction";
    const string Report="ArtDeliverables/TimeDesk/HallLayers/Production";
    static Transform root, hall;
    static Camera camera;
    static Material spriteMaterial;
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;

    [MenuItem("Tools/Terminal Art/Build Furnished Terminal")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode before building art.");
        ShaderUtil.allowAsyncCompilation=false;
        Directory.CreateDirectory(Report);AssetDatabase.Refresh();
        var source=SceneManager.GetSceneByPath("Assets/Scenes/OfficeScene.unity");bool opened=!source.isLoaded;
        if(opened)source=EditorSceneManager.OpenScene("Assets/Scenes/OfficeScene.unity",Application.isBatchMode?OpenSceneMode.Single:OpenSceneMode.Additive);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        root=new GameObject("Furnished Terminal Preview").transform;
        camera=new GameObject("Player composition camera").AddComponent<Camera>();camera.transform.SetParent(root);
        camera.transform.SetPositionAndRotation(new Vector3(0,2.16f,-2.62f),Quaternion.Euler(4,0,0));
        camera.fieldOfView=55;camera.aspect=16f/9;camera.nearClipPlane=.05f;camera.farClipPlane=150;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.69f,.72f,.75f);
        var data=camera.GetUniversalAdditionalCameraData();data.SetRenderer(1);data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        CopyDesk(source);
        hall=new GameObject("Layered terminal art").transform;hall.SetParent(root);
        spriteMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PaintedSprite.mat");
        if(!spriteMaterial){spriteMaterial=new Material(Shader.Find("NOPE/Terminal Painted Sprite"));AssetDatabase.CreateAsset(spriteMaterial,Folder+"/PaintedSprite.mat");}
        var architecture=Texture("Architecture");var furniture=Texture("Furnishings");
        Card("01 Architecture and baked lighting",architecture,new Rect(0,0,architecture.width,architecture.height),new Rect(0,0,1,1),65,0);
        Card("02 Luggage lockers and waiting bench",furniture,new Rect(8,389,783,389),new Rect(.085f,.443f,.215f,.189f),40,10);
        Card("02 Neglected framed landscape",furniture,new Rect(306,143,151,240),new Rect(.215f,.257f,.033f,.093f),43,10);
        Card("02 Stone bust and plinth",furniture,new Rect(1130,226,167,450),new Rect(.672f,.375f,.028f,.134f),42,10);
        Card("02 Ceramic artifact display",furniture,new Rect(1350,425,312,341),new Rect(.798f,.515f,.064f,.124f),38,11);
        Crowd(1,.31f,.65f,.058f,32);Crowd(2,.35f,.552f,.044f,42);Crowd(3,.635f,.562f,.047f,38);
        Crowd(4,.77f,.529f,.041f,43);Crowd(5,.414f,.26f,.032f,54);Crowd(6,.589f,.26f,.032f,54);
        Crowd(3,.792f,.218f,.035f,55);Crowd(1,.922f,.184f,.032f,56);
        PrefabUtility.SaveAsPrefabAsset(hall.gameObject,Folder+"/FurnishedTerminalArt.prefab");
        var sun=new GameObject("Warm window daylight").AddComponent<Light>();sun.transform.SetParent(root);
        sun.type=LightType.Directional;sun.color=new Color(1,.9f,.76f);sun.intensity=1.15f;
        sun.transform.rotation=Quaternion.Euler(38,-55,0);sun.shadows=LightShadows.Soft;sun.shadowStrength=.72f;
        RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.54f,.58f,.67f);
        RenderSettings.ambientEquatorColor=new Color(.52f,.46f,.40f);RenderSettings.ambientGroundColor=new Color(.28f,.23f,.22f);
        var lamp=new GameObject("Warm task lamp pool").AddComponent<Light>();lamp.transform.SetParent(root);
        lamp.type=LightType.Spot;lamp.transform.position=new Vector3(.46f,1.9f,.62f);lamp.transform.rotation=Quaternion.Euler(70,180,0);
        lamp.color=new Color(1,.70f,.38f);lamp.intensity=2;lamp.range=3;lamp.spotAngle=85;
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        camera.cullingMask=1<<30;sun.cullingMask=1<<30;lamp.cullingMask=1<<30;
        foreach(var guid in AssetDatabase.FindAssets("",new[]{Folder}))AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        EditorSceneManager.SaveScene(scene,Folder+"/FurnishedTerminal.unity");
        if(opened)EditorSceneManager.CloseScene(source,true);
        double ready=EditorApplication.timeSinceStartup+8;EditorApplication.CallbackFunction finish=null;
        finish=()=>{EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.timeSinceStartup<ready)return;EditorApplication.update-=finish;
            try{Capture();File.WriteAllText(Report+"/validation.json",JsonUtility.ToJson(new Validation{spriteCount=hall.GetComponentsInChildren<SpriteRenderer>().Length,rendererCount=root.GetComponentsInChildren<Renderer>().Length},true));if(Application.isBatchMode)EditorApplication.Exit(0);}
            catch(Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}};
        EditorApplication.update+=finish;
    }
    static Texture2D Texture(string name)
    {
        string path=Folder+"/Textures/"+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
        importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static void Card(string name,Texture2D texture,Rect topPixels,Rect target,float distance,int order)
    {
        var rect=new Rect(topPixels.x,texture.height-topPixels.y-topPixels.height,topPixels.width,topPixels.height);
        string asset=Folder+"/"+name+".asset";var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(asset);
        if(!sprite){sprite=Sprite.Create(texture,rect,new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);sprite.name=name;AssetDatabase.CreateAsset(sprite,asset);}
        var go=new GameObject(name);go.transform.SetParent(hall);go.transform.rotation=camera.transform.rotation;
        go.transform.position=camera.ViewportToWorldPoint(new Vector3(target.center.x,1-target.center.y,distance));
        float height=2*distance*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f),width=height*camera.aspect;
        go.transform.localScale=new Vector3(width*target.width/sprite.bounds.size.x,height*target.height/sprite.bounds.size.y,1);
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=spriteMaterial;sr.sortingOrder=order;
        sr.shadowCastingMode=ShadowCastingMode.Off;sr.receiveShadows=false;
    }
    static void Crowd(int variant,float x,float bottom,float height,float distance)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Office/HallCrowds/Meshes/CrowdGroup_"+variant.ToString("00")+".asset");
        string path=Folder+"/Atmospheric crowd.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("NOPE/Hall Crowd Silhouette"));material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/HallCrowds/Textures/CrowdGroups_Atlas.png"));AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_Tint",new Color(.30f,.31f,.34f));EditorUtility.SetDirty(material);
        float h=2*distance*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)*height,scale=h/mesh.bounds.size.y;
        var go=new GameObject("03 Anonymous crowd "+variant);go.transform.SetParent(hall);go.transform.rotation=camera.transform.rotation;
        go.transform.position=camera.ViewportToWorldPoint(new Vector3(x,1-bottom+height*.5f,distance))-go.transform.rotation*(mesh.bounds.center*scale);
        go.transform.localScale=Vector3.one*scale;go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;
    }
    static void CopyDesk(Scene source)
    {
        var parent=new GameObject("Approved 3D desk — preserved art").transform;parent.SetParent(root);
        foreach(var r in source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)))
        {
            string path=PathOf(r.transform);if(!r.enabled||!r.gameObject.activeInHierarchy||!(path.StartsWith("HybridOffice/Booth/")||path.StartsWith("ImportedOfficeDress/Desk/")))continue;
            var mf=r.GetComponent<MeshFilter>();if(!mf||!mf.sharedMesh)continue;
            var go=new GameObject(r.name);go.transform.SetParent(parent);go.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation);go.transform.localScale=r.transform.lossyScale;
            go.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;go.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
            if(path.Contains("Blender_Next/")&&r.name.EndsWith("__DeskClean_Glass"))Label(r,"NEXT",parent,Color.white);
            if(path.Contains("Blender_Clock/")&&r.name.EndsWith("__CRT_Glass"))Label(r,"09:00",parent,Color.white);
            if(path.Contains("Blender_Stability/")&&r.name.EndsWith("__CRT_Glass"))Label(r,"100%",parent,Color.white);
            if(path.Contains("Blender_DayCalendar/")&&r.name.EndsWith("__Office_Paper"))Label(r,"01",parent,new Color(.08f,.07f,.07f));
        }
    }
    static void Label(Renderer surface,string text,Transform parent,Color color)
    {
        var go=new GameObject("Preview display — "+text);go.transform.SetParent(parent);
        go.transform.SetPositionAndRotation(surface.bounds.center-surface.transform.forward*.012f,surface.transform.rotation);
        var tm=go.AddComponent<TextMesh>();tm.text=text;tm.anchor=TextAnchor.MiddleCenter;tm.alignment=TextAlignment.Center;
        tm.fontSize=64;tm.characterSize=.01f;tm.color=color;
        var b=go.GetComponent<Renderer>().bounds;
        float scale=Mathf.Min(surface.bounds.size.x*.86f/Mathf.Max(.001f,b.size.x),surface.bounds.size.y*.73f/Mathf.Max(.001f,b.size.y));
        go.transform.localScale=Vector3.one*scale;
    }
    static void Capture()
    {
        var rt=new RenderTexture(1920,1080,24);var old=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Report+"/unity-forward.png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
        finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    [MenuItem("Tools/Terminal Art/Open Furnished Terminal")]
    public static void Open()
    {
        EditorSceneManager.OpenScene(Folder+"/FurnishedTerminal.unity",OpenSceneMode.Single);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }
    [Serializable] class Validation
    {
        public string implementation="2.5D sprite hall with baked lighting, real 3D desk and realtime foreground lights";
        public string sourceScene="OfficeScene remains unchanged";
        public int spriteCount,rendererCount;
        public float cameraPitch=4,cameraFov=55;
        public string limitation="Fixed-view art preview; no gameplay routing or continuous left-pan hookup";
    }
}
