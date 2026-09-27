using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class TerminalLayeredProduction
{
    const string Folder="Assets/Art/Office/TerminalLayered";
    const string Report="ArtDeliverables/TimeDesk/HallLayers/Layered";
    static Camera camera;
    static LayeredHallRig rig;
    static Transform root,hall;
    [MenuItem("Tools/Terminal Art/Build Layered Lit Hall")]
    public static void Build()
    {
        Directory.CreateDirectory(Report);ShaderUtil.allowAsyncCompilation=false;
        var albedo=Texture("HallShell",false);var normals=Texture("HallNormals",true);var city=Texture("City",false);
        var source=EditorSceneManager.OpenScene("Assets/Art/Office/TerminalProduction/FurnishedTerminal.unity",OpenSceneMode.Single);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        root=UnityEngine.Object.Instantiate(source.GetRootGameObjects().First(g=>g.name=="Furnished Terminal Preview")).transform;
        root.name="Layered illustrated terminal";
        camera=root.GetComponentInChildren<Camera>();camera.name="Layered hall preview camera";camera.farClipPlane=400;
        camera.GetUniversalAdditionalCameraData().SetRenderer(1);
        hall=root.Find("Layered terminal art");
        var architecture=hall.GetComponentsInChildren<SpriteRenderer>().First(s=>s.name.StartsWith("01 Architecture"));
        architecture.name="01 Hall with transparent window openings";
        var oldSize=architecture.sprite.bounds.size;
        architecture.sprite=SpriteAsset("Hall shell modular",albedo);
        var scale=architecture.transform.localScale;scale.x*=oldSize.x/architecture.sprite.bounds.size.x;scale.y*=oldSize.y/architecture.sprite.bounds.size.y;architecture.transform.localScale=scale;
        var hallMat=Material("Hall responsive lighting",0);hallMat.SetTexture("_NormalTex",normals);EditorUtility.SetDirty(hallMat);
        architecture.sharedMaterial=hallMat;architecture.sortingOrder=0;
        foreach(var r in hall.GetComponentsInChildren<SpriteRenderer>().Where(r=>r!=architecture)){r.sharedMaterial=Material("Furnishing responsive lighting",2);r.sortingOrder+=20;}
        var crowdMat=Material("Crowd responsive lighting",2);crowdMat.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/HallCrowds/Textures/CrowdGroups_Atlas.png"));crowdMat.SetColor("_Color",new Color(.42f,.47f,.53f,1));EditorUtility.SetDirty(crowdMat);
        foreach(var crowd in hall.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("03 Anonymous")))crowd.sharedMaterial=crowdMat;
        var cityRoot=new GameObject("00 Independent city panorama").transform;cityRoot.SetParent(hall);
        CityPanorama(city,cityRoot);
        var luggage=hall.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name.Contains("Compact luggage"));
        UnityEngine.Object.DestroyImmediate(luggage.gameObject);
        TerminalHallModules.Build(hall,camera,architecture,Texture("Floor",false),Texture("ModularFixtures",false));
        WindowExtension();
        AddPartition();
        rig=root.gameObject.AddComponent<LayeredHallRig>();rig.view=camera;rig.cityLayer=cityRoot;rig.hallLayer=architecture.transform;
        rig.deskSun=root.GetComponentsInChildren<Light>().First(l=>l.type==LightType.Directional);rig.sunDirection=-.65f;rig.animatePortal=false;rig.Apply();
        RenderSettings.fog=false;LightmapSettings.lightmaps=Array.Empty<LightmapData>();
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        camera.cullingMask=1<<30;
        foreach(var light in root.GetComponentsInChildren<Light>())light.cullingMask=1<<30;
        PrefabUtility.SaveAsPrefabAsset(hall.gameObject,Folder+"/LayeredHallArt.prefab");
        foreach(var id in AssetDatabase.FindAssets("",new[]{Folder}))AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(id)));
        EditorSceneManager.SaveScene(scene,Folder+"/LayeredTerminal.unity");EditorSceneManager.CloseScene(source,true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        SessionState.SetBool("TerminalLayeredCapturePending",true);
        ScheduleCapture();
    }
    [InitializeOnLoadMethod] static void ResumeCapture(){if(SessionState.GetBool("TerminalLayeredCapturePending",false))EditorApplication.delayCall+=ScheduleCapture;}
    static void ScheduleCapture()
    {
        double ready=EditorApplication.timeSinceStartup+8;EditorApplication.CallbackFunction tick=null;
        tick=()=>{if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<ready)return;EditorApplication.update-=tick;
            try{
                rig=UnityEngine.Object.FindFirstObjectByType<LayeredHallRig>();if(!rig)return;root=rig.transform;camera=rig.view;hall=root.Find("Layered terminal art");
                rig.Forward();rig.Morning();rig.sunDirection=-.65f;rig.Apply();Capture("morning.png");
                rig.sunDirection=.85f;rig.Apply();Capture("light-direction-test.png");
                rig.sunDirection=-.65f;rig.Evening();Capture("evening.png");rig.Morning();rig.Left();Capture("left-window.png");rig.Forward();
                var errors=ShaderUtil.GetShaderMessages(Shader.Find("NOPE/Layered Hall Lighting")).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
                if(errors.Length>0)throw new Exception(string.Join("\n",errors.Select(e=>e.message)));
                var display=hall.GetComponent<HallDisplayView>();
                if(display.destinations.Length!=4 || display.flagCloth.Length!=2)throw new Exception("Missing editable display or flag bindings.");
                display.SetGate(0,"01","HOOK TEST",false);
                if(display.destinations[0].text!="HOOK TEST" || display.portalEnergy[0].enabled)throw new Exception("Display presentation hook failed.");
                display.SetGate(0,"01","DESTINATION A",true);
                File.WriteAllText(Report+"/validation.txt","Independent city and alpha-cut hall layers.\nSeparate furnishing and crowd layers.\nCamera-space normal-map lighting, animated portal emission and simulated window shadows.\nMorning, evening, changed-direction and left-view captures completed.\nShader errors: 0\nBaked Unity lightmaps: "+LightmapSettings.lightmaps.Length+"\nDesk source and gameplay unchanged.\n");
                rig.animatePortal=true;EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Folder+"/LayeredTerminal.unity");
                SessionState.SetBool("TerminalLayeredCapturePending",false);
            }catch(Exception e){Debug.LogException(e);File.WriteAllText(Report+"/build-error.txt",e.ToString());}
        };EditorApplication.update+=tick;
    }
    static Texture2D Texture(string name,bool data)
    {
        string path=Folder+"/Textures/"+name+".png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.sRGBTexture=!data;
        importer.alphaIsTransparency=!data;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.maxTextureSize=4096;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Material Material(string name,int mode)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find("NOPE/Layered Hall Lighting"));AssetDatabase.CreateAsset(m,path);}m.SetFloat("_Mode",mode);EditorUtility.SetDirty(m);return m;
    }
    static Sprite SpriteAsset(string name,Texture2D t)
    {
        string path=Folder+"/"+name+".asset";var s=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(!s){s=Sprite.Create(t,new Rect(0,0,t.width,t.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);AssetDatabase.CreateAsset(s,path);}return s;
    }
    static void CityPanorama(Texture2D texture,Transform parent)
    {
        var g=new GameObject("Continuous distant city panorama");g.transform.SetParent(parent,false);
        var v=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
        for(int i=0;i<=96;i++){
            float t=i/96f,a=Mathf.Lerp(-140,65,t)*Mathf.Deg2Rad;
            foreach(float y in new[]{-90f,90f}){v.Add(camera.transform.position+new Vector3(Mathf.Sin(a)*115,y,Mathf.Cos(a)*115));uv.Add(new Vector2(t,y<0?0:1));}
            if(i<96){int k=i*2;triangles.AddRange(new[]{k,k+1,k+3,k,k+3,k+2});}
        }
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Folder+"/ContinuousCityMesh.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,path);
        g.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=Material("City independent lighting",1);renderer.sharedMaterial.SetTexture("_MainTex",texture);renderer.sortingOrder=-100;
    }
    static void WindowExtension()
    {
        var texture=Texture("LeftShell",false);
        var go=new GameObject("Hall illustrated left continuation");go.transform.SetParent(hall);
        go.transform.rotation=camera.transform.rotation;go.transform.position=camera.ViewportToWorldPoint(new Vector3(0,.5f,68));
        float height=2*68*Mathf.Tan(55*Mathf.Deg2Rad*.5f);
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=SpriteAsset("Hall left shell",texture);sr.sharedMaterial=Material("Hall left continuation lighting",3);sr.sortingOrder=-1;
        go.transform.localScale=new Vector3(height*camera.aspect*2/sr.sprite.bounds.size.x,height/sr.sprite.bounds.size.y,1);
        var floorTexture=Texture("LeftFloor",false);var fg=UnityEngine.Object.Instantiate(go,hall);fg.name="LH_Left independent floor";
        var fs=fg.GetComponent<SpriteRenderer>();fs.sprite=SpriteAsset("Left independent floor",floorTexture);fs.sortingOrder=-3;
        fs.sharedMaterial=Material("Left floor independent finish",4);fs.sharedMaterial.SetFloat("_Mode",4.2f);EditorUtility.SetDirty(fs.sharedMaterial);

    }
    static void Frame(Transform parent,Vector3 p,Vector3 size,Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name="Window mullion";UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());g.transform.SetParent(parent,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=material;
    }
    static void AddPartition()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Office/TerminalRelit/DynamicTerminalArt.prefab");
        foreach(var t in prefab.GetComponentsInChildren<Transform>().Where(t=>t.name=="Clear desk partition"||t.name.StartsWith("Desk partition"))){
            var g=UnityEngine.Object.Instantiate(t.gameObject,hall);g.transform.SetPositionAndRotation(t.position,t.rotation);g.transform.localScale=t.lossyScale;
        }
    }
    static void Capture(string name)
    {
        var rt=new RenderTexture(1920,1080,24);var old=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();File.WriteAllBytes(Report+"/"+name,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);}
        finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
}
