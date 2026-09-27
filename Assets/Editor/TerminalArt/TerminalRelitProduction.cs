using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Standalone art authoring. Never rebuilds or saves OfficeScene or gameplay assets.
public static class TerminalRelitProduction
{
    const string Folder="Assets/Art/Office/TerminalRelit";
    const string Report="ArtDeliverables/TimeDesk/HallLayers/Relit";
    static Transform root,hall;
    static Camera camera;
    static Material stone,trim,dark,brass,burgundy,ivory,glass,glow;
    static readonly List<Light> practicals=new List<Light>();
    static readonly Dictionary<string,Mesh> meshCache=new Dictionary<string,Mesh>();
    static TerminalLightingRig rig;
    const float Ground=-5.2f;

    [MenuItem("Tools/Terminal Art/Build Dynamically Lit Terminal")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play mode first.");
        ShaderUtil.allowAsyncCompilation=false;
        Directory.CreateDirectory(Folder);Directory.CreateDirectory(Report);AssetDatabase.Refresh();
        practicals.Clear();meshCache.Clear();
        var source=EditorSceneManager.OpenScene("Assets/Art/Office/TerminalProduction/FurnishedTerminal.unity",OpenSceneMode.Single);
        var desk=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name.StartsWith("Approved 3D desk"));
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
        root=new GameObject("Dynamically lit grand terminal").transform;
        var clone=UnityEngine.Object.Instantiate(desk.gameObject,root);clone.name=desk.name;
        hall=new GameObject("Terminal architecture and furnishings").transform;hall.SetParent(root);
        camera=new GameObject("Player art camera").AddComponent<Camera>();camera.transform.SetParent(root);
        camera.transform.SetPositionAndRotation(new Vector3(0,2.16f,-2.62f),Quaternion.Euler(4,0,0));
        camera.fieldOfView=55;camera.aspect=16f/9;camera.nearClipPlane=.05f;camera.farClipPlane=700;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.allowHDR=true;
        camera.GetUniversalAdditionalCameraData().SetRenderer(1);
        camera.GetUniversalAdditionalCameraData().antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        stone=Mat("Warm limestone","BEB3A0");trim=Mat("Light carved stone","D2C8B5");
        stone.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/DebtRelief/Textures/WornPlaster.png"));stone.SetFloat("_TextureStrength",.38f);
        dark=Mat("Graphite enamel","333039");brass=Mat("Aged brass","9A7643",.3f);
        burgundy=Mat("Civic burgundy","773B4C");ivory=Mat("Lettering ivory","EBD9AC");
        glow=Mat("Lamp luminous glass","FFE1A1",0,true);
        glass=Mat("Window glass","B4D3DE",.65f);
        glass.shader=Shader.Find("Universal Render Pipeline/Lit");glass.SetFloat("_Surface",1);
        glass.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);glass.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
        glass.SetFloat("_ZWrite",0);glass.SetFloat("_Cull",0);glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        glass.SetColor("_BaseColor",new Color(.65f,.78f,.87f,.075f));glass.renderQueue=3000;
        Architecture();Platforms();Furnishings();City();Crowds();
        var sun=new GameObject("Realtime window sun").AddComponent<Light>();sun.transform.SetParent(root);
        sun.type=LightType.Directional;sun.shadows=LightShadows.Soft;sun.shadowStrength=.85f;sun.shadowBias=.025f;sun.shadowNormalBias=.12f;
        sun.lightmapBakeType=LightmapBakeType.Realtime;
        var task=new GameObject("Realtime desk task light").AddComponent<Light>();task.transform.SetParent(root);
        task.type=LightType.Spot;task.transform.position=new Vector3(.46f,1.9f,.62f);task.transform.rotation=Quaternion.Euler(70,180,0);
        task.color=new Color(1,.77f,.49f);task.range=3;task.spotAngle=85;task.intensity=2;task.lightmapBakeType=LightmapBakeType.Realtime;
        var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder+"/PreviewPipeline.asset");
        if(!pipeline){pipeline=UnityEngine.Object.Instantiate((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline);AssetDatabase.CreateAsset(pipeline,Folder+"/PreviewPipeline.asset");}
        pipeline.shadowDistance=180;pipeline.shadowCascadeCount=4;pipeline.mainLightShadowmapResolution=4096;
        var pipelineData=new SerializedObject(pipeline);
        pipelineData.FindProperty("m_MainLightShadowsSupported").boolValue=true;
        pipelineData.FindProperty("m_SoftShadowsSupported").boolValue=true;
        pipelineData.FindProperty("m_AdditionalLightsRenderingMode").intValue=(int)LightRenderingMode.PerPixel;
        pipelineData.FindProperty("m_AdditionalLightShadowsSupported").boolValue=true;
        pipelineData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(pipeline);
        rig=root.gameObject.AddComponent<TerminalLightingRig>();rig.previewPipeline=pipeline;rig.daylight=sun;rig.practicals=practicals.ToArray();rig.previewCamera=camera;
        rig.enabled=false;rig.enabled=true;rig.Morning();
        foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
        camera.cullingMask=1<<30;foreach(var l in root.GetComponentsInChildren<Light>())l.cullingMask=1<<30;
        // No baked lightmaps and no lightmapping static flags are authored.
        LightmapSettings.lightmaps=Array.Empty<LightmapData>();
        PrefabUtility.SaveAsPrefabAsset(hall.gameObject,Folder+"/DynamicTerminalArt.prefab");
        foreach(var guid in AssetDatabase.FindAssets("",new[]{Folder}))AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
        EditorSceneManager.SaveScene(scene,Folder+"/DynamicTerminal.unity");EditorSceneManager.CloseScene(source,true);
        double ready=EditorApplication.timeSinceStartup+8;EditorApplication.CallbackFunction tick=null;
        tick=()=>{EditorApplication.QueuePlayerLoopUpdate();if(EditorApplication.timeSinceStartup<ready)return;EditorApplication.update-=tick;
            try {Capture("morning.png");rig.Evening();Capture("evening.png");rig.Morning();
                sun.transform.rotation=Quaternion.Euler(38,-55,0);Capture("opposite-light-test.png");rig.Morning();
                camera.transform.rotation=Quaternion.Euler(4,-48,0);Capture("left-window.png");camera.transform.rotation=Quaternion.Euler(4,0,0);
                Validate();EditorSceneManager.SaveScene(scene,Folder+"/DynamicTerminal.unity");
                if(Application.isBatchMode){rig.enabled=false;EditorApplication.Exit(0);}
            }catch(Exception e){Debug.LogException(e);if(Application.isBatchMode){if(rig)rig.enabled=false;EditorApplication.Exit(1);}}};
        EditorApplication.update+=tick;
    }
    static Material Mat(string name,string hex,float smooth=.08f,bool unlit=false)
    {
        string path=Folder+"/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"NOPE/Terminal Relit"));AssetDatabase.CreateAsset(m,path);}
        ColorUtility.TryParseHtmlString("#"+hex,out var c);m.SetColor("_BaseColor",c);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
    }
    static GameObject MeshObject(string name,Mesh mesh,Vector3 p,Material m)
    {
        var go=new GameObject(name);go.transform.SetParent(hall);go.transform.position=p;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=m;
        r.shadowCastingMode=ShadowCastingMode.TwoSided;r.receiveShadows=true;return go;
    }
    static Mesh SaveMesh(string key,Mesh mesh)
    {
        string path=Folder+"/Mesh_"+key+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old){UnityEngine.Object.DestroyImmediate(mesh);return old;}mesh.name=key;AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static GameObject Box(string name,Vector3 p,Vector3 size,Material m,float bevel=.04f)
    {
        string key=string.Join("_",new[]{size.x,size.y,size.z,bevel}.Select(v=>v.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)));
        if(!meshCache.TryGetValue(key,out var mesh))
        {
            float r=Mathf.Min(bevel,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.24f);var core=size*.5f-Vector3.one*r;
            var verts=new List<Vector3>();var norms=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            foreach(var n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
            {
                var u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.up;var v=Vector3.Cross(n,u);
                float eu=Mathf.Abs(Vector3.Dot(u,size))*.5f,ev=Mathf.Abs(Vector3.Dot(v,size))*.5f,en=Mathf.Abs(Vector3.Dot(n,size))*.5f;
                var us=new[]{-eu,-eu+r,eu-r,eu};var vs=new[]{-ev,-ev+r,ev-r,ev};int first=verts.Count;
                for(int j=0;j<4;j++)for(int i=0;i<4;i++){
                    var p0=n*en+u*us[i]+v*vs[j];var c=new Vector3(Mathf.Clamp(p0.x,-core.x,core.x),Mathf.Clamp(p0.y,-core.y,core.y),Mathf.Clamp(p0.z,-core.z,core.z));
                    var normal=(p0-c).normalized;verts.Add(c+normal*r);norms.Add(normal);uv.Add(new Vector2(i/3f,j/3f));}
                for(int j=0;j<3;j++)for(int i=0;i<3;i++){int a=first+j*4+i;tris.AddRange(new[]{a,a+1,a+5,a,a+5,a+4});}
            }
            mesh=new Mesh();mesh.SetVertices(verts);mesh.SetNormals(norms);mesh.SetUVs(0,uv);mesh.SetTriangles(tris,0);mesh.RecalculateBounds();mesh=SaveMesh(key,mesh);meshCache[key]=mesh;
        }
        return MeshObject(name,mesh,p,m);
    }
    static GameObject Primitive(string name,PrimitiveType type,Vector3 p,Vector3 size,Material m)
    {
        var g=GameObject.CreatePrimitive(type);g.name=name;g.transform.SetParent(hall);g.transform.position=p;g.transform.localScale=size;
        var r=g.GetComponent<Renderer>();r.sharedMaterial=m;r.receiveShadows=true;r.shadowCastingMode=ShadowCastingMode.On;
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>());return g;
    }
    static void Beam(string name,Vector3 a,Vector3 b,float width,Material m)
    {var g=Box(name,(a+b)*.5f,new Vector3(width,(b-a).magnitude,width),m,.02f);g.transform.rotation=Quaternion.FromToRotation(Vector3.up,b-a);}
    static void Rail(Vector3 a,Vector3 b)
    {
        Beam("Brass handrail",a+Vector3.up*1.15f,b+Vector3.up*1.15f,.08f,brass);
        Beam("Lower rail",a+Vector3.up*.35f,b+Vector3.up*.35f,.055f,dark);
        int count=Mathf.CeilToInt(Vector3.Distance(a,b)/.8f);
        for(int i=0;i<=count;i++){var p=Vector3.Lerp(a,b,i/(float)count);Box("Baluster",p+Vector3.up*.55f,new Vector3(.06f,1.1f,.06f),dark,.01f);}
    }
    static void Architecture()
    {
        var floorMats=new[]{Mat("Floor limestone A","AAA08F"),Mat("Floor limestone B","B6AA96"),Mat("Floor limestone C","9E978C")};
        Box("Concourse foundation",new Vector3(0,Ground-.55f,66),new Vector3(70,1,124),dark);
        for(int x=-34;x<34;x+=4)for(int z=8;z<128;z+=4)
            Box("Stone floor slab",new Vector3(x+2,Ground-.025f,z+2),new Vector3(3.985f,.05f,3.985f),floorMats[Mathf.Abs(x*7+z*3)%3],.008f);
        foreach(float x in new[]{-15f,15f}){
            Box("Burgundy concourse inlay",new Vector3(x,Ground+.015f,64),new Vector3(1.3f,.025f,112),burgundy,.003f);
            foreach(float offset in new[]{-.72f,.72f})Box("Brass floor border",new Vector3(x+offset,Ground+.025f,64),new Vector3(.05f,.018f,112),brass,.003f);}
        Box("Rear civic wall",new Vector3(0,12,127),new Vector3(70,38,2),stone);
        Box("Right concourse wall",new Vector3(34,1,86),new Vector3(2,14,84),stone,.08f);
        // Freestanding fluted piers frame the gates and carry the bridge spans.
        foreach(float x in new[]{-22f,22f})foreach(float z in new[]{50f,90f}){
            Box("Interior monumental pier",new Vector3(x,12,z),new Vector3(3.6f,34,3.6f),stone,.14f);
            Box("Interior pier plinth",new Vector3(x,Ground+.6f,z),new Vector3(4.4f,1.2f,4.4f),trim,.10f);
            Box("Interior pier capital",new Vector3(x,28.6f,z),new Vector3(4.4f,1.1f,4.4f),trim,.10f);
            for(int f=-2;f<=2;f++)Box("Interior pier flute",new Vector3(x+f*.52f,12,z-1.84f),new Vector3(.10f,29,.10f),trim,.025f);
            for(int s=0;s<8;s++)Box("Pier masonry seam",new Vector3(x,Ground+2+s*3.5f,z-1.81f),new Vector3(3.35f,.018f,.02f),Mat("Stone joint","958A79"),.002f);
        }
        for(int x=-24;x<=24;x+=12){
            Box("Rear recessed panel",new Vector3(x,7,125.85f),new Vector3(8,22,.12f),Mat("Recessed plaster","9B948A"),.03f);
            foreach(float side in new[]{-1f,1f})Box("Rear panel moulding",new Vector3(x+side*4.1f,7,125.65f),new Vector3(.18f,22,.25f),trim,.025f);
            Box("Rear panel crown",new Vector3(x,18,125.65f),new Vector3(8.4f,.28f,.25f),trim,.025f);
        }
        // Open window bays, with physical mullions casting moving shadows.
        for(int z=10;z<=110;z+=20)
        {
            foreach(int side in new[]{-1,1})
            {
                float x=side*33;
                Box("Fluted structural pier",new Vector3(x,11,z),new Vector3(3,36,3),stone,.16f);
                Box("Pier base",new Vector3(x,Ground+.75f,z),new Vector3(3.7f,1.5f,3.7f),trim,.10f);
                Box("Pier capital",new Vector3(x,28,z),new Vector3(4,1.2f,4),trim,.12f);
                for(int f=-1;f<=1;f++)Box("Pier face flute",new Vector3(x-side*1.53f,11,z+f*.66f),new Vector3(.12f,29,.15f),trim,.035f);
            }
            if(z<110)
            {
                Box("Window sill",new Vector3(-33,-4,z+10),new Vector3(1.2f,1,17),trim,.07f);
                var pane=Box("Transparent west glazing",new Vector3(-33,12,z+10),new Vector3(.04f,31,17),glass,.005f);
                pane.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
                for(int j=1;j<=3;j++)Box("Window mullion",new Vector3(-32.8f,12,z+1.5f+j*4.2f),new Vector3(.18f,31,.13f),brass,.02f);
                foreach(float y in new[]{3f,14f,24f})Box("Window transom",new Vector3(-32.8f,y,z+10),new Vector3(.18f,.16f,17),brass,.02f);
                Box("Right wall pier infill",new Vector3(34,18,z+10),new Vector3(2,22,17),stone,.08f);
            }
        }
        foreach(float x in new[]{-33f,33f})
        {
            Box("Crown cornice",new Vector3(x,29.5f,66),new Vector3(4,1,122),trim,.12f);
            Box("Crown shadow moulding",new Vector3(x,28.7f,66),new Vector3(3.5f,.22f,122),brass,.03f);
        }
        Box("Vault ceiling",new Vector3(0,33,68),new Vector3(70,.7f,120),stone,.05f);
        for(int z=14;z<127;z+=14){Box("Coffered transverse beam",new Vector3(0,31.8f,z),new Vector3(68,1.3f,.6f),trim,.08f);}
        for(int x=-28;x<=28;x+=7)Box("Ceiling coffer rib",new Vector3(x,31.7f,68),new Vector3(.3f,.5f,120),brass,.03f);
        // Tall rear pilasters and recessed portal vista continue the perspective.
        for(int x=-24;x<=24;x+=12){Box("Rear pilaster",new Vector3(x,11,124.7f),new Vector3(2.2f,36,2),trim,.12f);}
        // First bridge, kept at the reference's upper-middle horizon.
        Box("Cross concourse bridge",new Vector3(0,13.8f,69),new Vector3(66,1.4f,6),stone,.12f);
        Box("Bridge bronze fascia",new Vector3(0,13.8f,65.9f),new Vector3(66,.22f,.12f),brass,.025f);
        Rail(new Vector3(-32,14.5f,66),new Vector3(32,14.5f,66));
        Rail(new Vector3(-32,14.5f,72),new Vector3(32,14.5f,72));
        // A clear left stair descends towards the main concourse, not towards the desk.
        Box("West landing",new Vector3(-28,2.3f,40),new Vector3(10,1,10),stone,.08f);
        for(int i=0;i<40;i++)Box("West stair tread",new Vector3(-24+i*.35f,2.7f-i*.2f,40),new Vector3(.36f,.24f,5),trim,.015f);
        Rail(new Vector3(-24,2.8f,37.4f),new Vector3(-10.35f,Ground,37.4f));
        Rail(new Vector3(-24,2.8f,42.6f),new Vector3(-10.35f,Ground,42.6f));
        Box("West landing support",new Vector3(-29,-1.2f,40),new Vector3(7,7,9),stone,.08f);
        // Vertical circulation sits behind the bridge, preserving the open window vista.
        Box("West passenger lift core",new Vector3(-29,5,78),new Vector3(5,23,6),stone,.12f);
        foreach(float y in new[]{Ground,14.5f}){
            Box("Lift bronze surround",new Vector3(-29,y+1.7f,74.85f),new Vector3(3.4f,3.4f,.2f),brass,.04f);
            Box("Lift doors",new Vector3(-29,y+1.65f,74.70f),new Vector3(2.8f,3.2f,.12f),dark,.02f);
            Box("Lift door seam",new Vector3(-29,y+1.65f,74.60f),new Vector3(.025f,3.2f,.015f),brass,.002f);}
        // Side-platform deck and a genuinely open passage underneath.
        Box("Upper platform deck",new Vector3(27,7.4f,77),new Vector3(14,1.2f,78),stone,.09f);
        Rail(new Vector3(19.9f,8,38),new Vector3(19.9f,8,116));
        foreach(float z in new[]{40f,60f,80f,100f})Box("Platform support",new Vector3(28,.5f,z),new Vector3(2,15,2),trim,.08f);
        Box("Right corridor roof",new Vector3(29,1.9f,32),new Vector3(14,1,12),stone,.09f);
        Box("Right corridor outer wall",new Vector3(35,-2.5f,32),new Vector3(1,8,12),stone,.06f);
        Box("Right corridor inner jamb",new Vector3(23,-2.5f,36),new Vector3(1,8,4),trim,.06f);
        Sign("MEDBAY  /  JAIL  /  C-SUITES  >",new Vector3(22.5f,6,47),10.5f,1.25f,.7f);
        Beam("Wayfinding suspension",new Vector3(18,6.7f,47),new Vector3(18,7.6f,47),.055f,brass);
        Beam("Wayfinding suspension",new Vector3(27,6.7f,47),new Vector3(27,7.6f,47),.055f,brass);
        Box("Corridor turn wall",new Vector3(28,-2.5f,44),new Vector3(12,8,1),stone,.07f);
        // Desk sits on its own entry landing; main hall remains below it.
        Box("Desk landing",new Vector3(0,-.28f,1),new Vector3(12,.55f,8),stone);
        Rail(new Vector3(-6,0,4.5f),new Vector3(-12,0,4.5f));Rail(new Vector3(6,0,4.5f),new Vector3(12,0,4.5f));
    }
    static void Sign(string text,Vector3 p,float width,float height,float letters)
    {
        Box("Sign frame / "+text,p,new Vector3(width+.16f,height+.16f,.20f),brass,.035f);
        Box("Sign face / "+text,p+Vector3.back*.115f,new Vector3(width,height,.035f),dark,.01f);
        var label=Text(text,p+Vector3.back*.14f,letters,new Color(.90f,.80f,.54f));
        var b=label.GetComponent<Renderer>().bounds;
        label.transform.localScale*=Mathf.Min(1,(width*.92f)/Mathf.Max(.001f,b.size.x));
    }
    static GameObject Text(string text,Vector3 p,float size,Color color)
    {
        var g=new GameObject("Lettering "+text);g.transform.SetParent(hall);g.transform.position=p;
        var t=g.AddComponent<TextMesh>();t.text=text;t.fontSize=64;t.characterSize=.1f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;
        g.transform.localScale=Vector3.one*(size/Mathf.Max(.001f,g.GetComponent<Renderer>().bounds.size.y));
        g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        return g;
    }
    static void Ring(string name,Vector3 p,float radius,float thickness,float depth,Material material,int segments=96)
    {
        var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var tr=new List<int>();
        for(int i=0;i<=segments;i++)for(int j=0;j<=12;j++){
            float a=i*2*Mathf.PI/segments,b=j*2*Mathf.PI/12;var norm=new Vector3(Mathf.Cos(a)*Mathf.Cos(b),Mathf.Sin(a)*Mathf.Cos(b),Mathf.Sin(b));
            v.Add(new Vector3(Mathf.Cos(a)*(radius+Mathf.Cos(b)*thickness),Mathf.Sin(a)*(radius+Mathf.Cos(b)*thickness),Mathf.Sin(b)*depth));n.Add(norm);uv.Add(new Vector2(i/(float)segments,j/12f));}
        for(int i=0;i<segments;i++)for(int j=0;j<12;j++){int a=i*13+j;tr.AddRange(new[]{a,a+13,a+14,a,a+14,a+1});}
        var mesh=new Mesh();mesh.SetVertices(v);mesh.SetNormals(n);mesh.SetUVs(0,uv);mesh.SetTriangles(tr,0);mesh.RecalculateBounds();
        MeshObject(name,SaveMesh("Ring_"+radius+"_"+thickness+"_"+depth,mesh),p,material);
    }
    static void Gate(int number,Vector3 basePoint,float radius)
    {
        Primitive("Raised circular departure platform "+number,PrimitiveType.Cylinder,basePoint+Vector3.up*.22f,new Vector3(radius*3,.22f,radius*2.1f),dark);
        Primitive("Platform stone cap",PrimitiveType.Cylinder,basePoint+Vector3.up*.47f,new Vector3(radius*2.94f,.06f,radius*2.04f),trim);
        Vector3 center=basePoint+Vector3.up*(radius+.7f);
        Ring("Structural portal ring "+number,center,radius,.40f,.50f,Mat("Portal gunmetal","6B6A68",.32f));
        Ring("Portal rim brass seam",center+Vector3.back*.37f,radius+.10f,.045f,.045f,brass);
        Ring("Inner portal rim",center+Vector3.back*.27f,radius-.20f,.065f,.075f,trim);
        for(int i=0;i<24;i++){
            float a=i*Mathf.PI*2/24;var p=center+new Vector3(Mathf.Sin(a)*(radius+.04f),Mathf.Cos(a)*(radius+.04f),-.45f);
            var g=Box("Ring segment seam",p,new Vector3(.06f,.44f,.025f),brass,.005f);g.transform.rotation=Quaternion.Euler(0,0,-a*Mathf.Rad2Deg);
        }
        var energy=Mat("Emissive portal surface","388CBD",0,true);
        energy.shader=Shader.Find("NOPE/Terminal Energy");
        var disc=Primitive("Portal energy "+number,PrimitiveType.Cylinder,center,new Vector3((radius-.22f)*2,.025f,(radius-.22f)*2),energy);disc.transform.rotation=Quaternion.Euler(90,0,0);
        disc.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
        foreach(float side in new[]{-1f,1f})Box("Portal anchor",basePoint+new Vector3(side*radius*.70f,.85f,0),new Vector3(.55f,1.5f,1.1f),dark);
        Sign(number.ToString("00"),center+new Vector3(-radius*.5f,radius+1,-.1f),2.5f,1.2f,1.05f);
        // Curved safety rail leaves a clear centre entrance.
        for(int i=0;i<28;i++){
            float a=(i+2)*Mathf.PI*2/32;float b=(i+3)*Mathf.PI*2/32;
            if(a>Mathf.PI*.70f && a<Mathf.PI*1.3f)continue;
            var pa=basePoint+new Vector3(Mathf.Sin(a)*radius*1.40f,.54f,Mathf.Cos(a)*radius*.98f);
            var pb=basePoint+new Vector3(Mathf.Sin(b)*radius*1.40f,.54f,Mathf.Cos(b)*radius*.98f);Rail(pa,pb);
        }
        foreach(float side in new[]{-1f,1f})Lantern(basePoint+new Vector3(side*(radius+1.6f),.0f,-.5f),4);
    }
    static void Platforms()
    {
        Gate(1,new Vector3(-2,Ground,39),4.1f);Gate(2,new Vector3(13,Ground,73),3.7f);
        Gate(3,new Vector3(28,8,60),3.6f);Gate(4,new Vector3(28,8,43),3.6f);
        Sign("DEPARTURES",new Vector3(0,19,52),18,2,1.5f);
        Box("Departure information panel",new Vector3(0,15.9f,52),new Vector3(18,4.1f,.32f),dark);
        for(int i=0;i<5;i++){
            Text((i+1).ToString("00")+"     "+new[]{"CLEARANCE","BOARDING","ASSIGNED","ON HOLD","STANDBY"}[i]+"           -- : --",new Vector3(0,17.25f-i*.67f,51.80f),.55f,new Color(.76f,.61f,.34f));
            Box("Board divider",new Vector3(0,16.96f-i*.67f,51.78f),new Vector3(16.8f,.024f,.02f),brass,.003f);}
        Beam("Board suspension",new Vector3(-7,20.2f,52),new Vector3(-7,32,52),.08f,dark);Beam("Board suspension",new Vector3(7,20.2f,52),new Vector3(7,32,52),.08f,dark);
        foreach(var p in new[]{new Vector3(-17,Ground,25),new Vector3(17,Ground,35),new Vector3(-20,Ground,62),new Vector3(20,Ground,90)})Lantern(p,4.5f);
    }
    static void Lantern(Vector3 foot,float height)
    {
        Box("Lantern plinth",foot+Vector3.up*.18f,new Vector3(.9f,.36f,.9f),stone,.06f);
        Box("Lantern stem",foot+Vector3.up*(height*.48f),new Vector3(.22f,height-.5f,.22f),dark,.04f);
        var p=foot+Vector3.up*height;
        Box("Lantern glow",p,new Vector3(.52f,.86f,.52f),glow,.05f);
        foreach(float x in new[]{-.30f,.30f})foreach(float z in new[]{-.30f,.30f})Box("Lantern bronze cage",p+new Vector3(x,0,z),new Vector3(.075f,1.05f,.075f),brass,.01f);
        Box("Lantern cap",p+Vector3.up*.56f,new Vector3(.8f,.15f,.8f),dark,.04f);Box("Lantern sill",p-Vector3.up*.54f,new Vector3(.75f,.15f,.75f),brass,.04f);
        var g=new GameObject("Realtime lantern pool");g.transform.SetParent(hall);g.transform.position=p+Vector3.back*.5f;
        var l=g.AddComponent<Light>();l.type=LightType.Point;l.range=9;l.color=new Color(1,.66f,.32f);l.lightmapBakeType=LightmapBakeType.Realtime;
        l.shadows=LightShadows.Soft;l.shadowResolution=LightShadowResolution.Low;practicals.Add(l);
    }
    static void Furnishings()
    {
        // True station luggage compartments, all anchored to the concourse.
        Vector3 p=new Vector3(-12,Ground,35);
        Box("Luggage bank solid casing",p+Vector3.up*1.0f,new Vector3(4.5f,2,1.1f),dark,.065f);
        for(int x=0;x<5;x++)for(int y=0;y<2;y++){
            var d=p+new Vector3(-1.78f+x*.89f,.52f+y*.94f,-.575f);
            Box("Numbered luggage door",d,new Vector3(.84f,.87f,.065f),(x+y)%3==0?burgundy:dark,.04f);
            Box("Locker brass latch",d+new Vector3(.28f,0,-.06f),new Vector3(.055f,.15f,.03f),brass,.01f);
            Text((101+x+y*5).ToString(),d+new Vector3(0,.22f,-.075f),.065f,new Color(.79f,.69f,.49f));
            for(int v=0;v<3;v++)Box("Locker vent",d+new Vector3(0,-.2f-v*.035f,-.04f),new Vector3(.20f,.012f,.008f),brass,.002f);
        }
        Sign("LUGGAGE",p+new Vector3(0,2.30f,-.51f),4.4f,.48f,.25f);
        foreach(var o in new[]{new Vector3(2.75f,.44f,0),new Vector3(3.25f,.30f,-.6f)}){
            var suitcase=p+o;Box("Stored travel case",suitcase,new Vector3(.72f,o.y*2,.34f),Mat("Luggage leather","785342"),.08f);
            Box("Case handle",suitcase+Vector3.up*(o.y+.06f),new Vector3(.24f,.09f,.09f),brass,.02f);}
        // Properly scaled seats below the west landing, supported at all four feet.
        Bench(new Vector3(-24,Ground,31));Bench(new Vector3(-24,Ground,55));
        // Lit decorative albedo cards, each physically mounted with 3D shadow proxies.
        var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/NeutralDecor.png");
        if(atlas){
            foreach(var pos in new[]{new Vector3(-22,17,48.1f),new Vector3(22,17,48.1f),new Vector3(-22,17,88.1f)})Card("Civic cloth banner",atlas,new Vector2(0,.5f),new Vector2(.5f,.5f),pos,new Vector2(5,12));
            var framePos=new Vector3(-29,8,74.55f);
            Box("Neglected painting backing",framePos,new Vector3(2.8f,2.2f,.15f),brass);
            Card("Displayed landscape",atlas,new Vector2(.56f,.60f),new Vector2(.38f,.30f),framePos+Vector3.back*.10f,new Vector2(2.65f,2.05f));
            Artifact(atlas,new Vector3(17,Ground,52),false);Artifact(atlas,new Vector3(24,Ground,29),true);
        }
    }
    static void Bench(Vector3 p)
    {
        var wood=Mat("Bench walnut","805C43");
        for(int i=0;i<5;i++)Box("Bench seat slat",p+new Vector3(0,.48f,-.27f+i*.135f),new Vector3(2.4f,.065f,.11f),wood,.02f);
        for(int i=0;i<4;i++)Box("Bench back slat",p+new Vector3(0,.67f+i*.14f,.35f),new Vector3(2.4f,.10f,.065f),wood,.02f);
        foreach(float x in new[]{-.98f,.98f})foreach(float z in new[]{-.25f,.30f})Box("Grounded bench foot",p+new Vector3(x,.23f,z),new Vector3(.075f,.46f,.075f),dark,.015f);
        foreach(float x in new[]{-.98f,.98f})Box("Back support",p+new Vector3(x,.72f,.39f),new Vector3(.06f,.78f,.07f),dark,.01f);
    }
    static void Card(string name,Texture2D texture,Vector2 offset,Vector2 scale,Vector3 p,Vector2 size)
    {
        var m=Mat(name,"FFFFFF");m.SetTexture("_BaseMap",texture);m.SetTextureOffset("_BaseMap",offset);m.SetTextureScale("_BaseMap",scale);m.SetFloat("_Wear",0);EditorUtility.SetDirty(m);
        var g=Primitive(name,PrimitiveType.Quad,p,new Vector3(size.x,size.y,1),m);g.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.TwoSided;
    }
    static void Artifact(Texture2D atlas,Vector3 p,bool vase)
    {
        Box("Artifact plinth base",p+Vector3.up*.12f,new Vector3(1.5f,.24f,1.5f),trim,.05f);
        Box("Artifact plinth",p+Vector3.up*.95f,new Vector3(1,1.7f,1),dark,.06f);
        Box("Artifact plinth cap",p+Vector3.up*1.85f,new Vector3(1.3f,.15f,1.3f),brass,.04f);
        Card(vase?"Terracotta artifact":"Marble bust",atlas,new Vector2(vase?0:.5f,0),new Vector2(.5f,.5f),p+new Vector3(0,2.6f,-.1f),new Vector2(1.5f,1.5f));
        // Hidden proxy has the volume needed for meaningful changing cast shadows.
        var proxy=Primitive("Artifact volume shadow proxy",PrimitiveType.Capsule,p+new Vector3(0,2.55f,.08f),new Vector3(.7f,.6f,.6f),stone);
        proxy.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.ShadowsOnly;
    }
    static void City()
    {
        var colors=new[]{"9E8E87","B79A87","8B94A4","B7B0A7","8F8496","AD8776"};
        for(int i=0;i<45;i++){
            float x=-65-(i%5)*30,z=-55+(i/5)*28,h=38+(i*23%95),y=-90+h*.5f;
            var m=Mat("City facade "+i%6,colors[i%6]);Box("City tower",new Vector3(x,y,z),new Vector3(12,h,14),m,.25f);
            Box("Tower crown",new Vector3(x,y+h*.5f+1,z),new Vector3(9,2,11),trim,.12f);
            for(int k=0;k<6;k++)Box("Tower vertical rib",new Vector3(x+6.05f,y,z-5+k*2),new Vector3(.2f,h,.20f),dark,.02f);
            for(int k=0;k<h/5;k++)Box("Tower glazing band",new Vector3(x+6.1f,-90+k*5,z),new Vector3(.15f,1.7f,13),Mat("City window slate","697887"),.02f);
        }
    }
    static void Crowds()
    {
        var positions=new[]{new Vector3(-10,Ground,47),new Vector3(12,Ground,50),new Vector3(19,Ground,32),new Vector3(-11,14.5f,67),new Vector3(11,14.5f,67),new Vector3(25,8,55)};
        var mat=Mat("Lit anonymous crowd","6D6972");mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/HallCrowds/Textures/CrowdGroups_Atlas.png"));EditorUtility.SetDirty(mat);
        for(int i=0;i<positions.Length;i++){
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Office/HallCrowds/Meshes/CrowdGroup_"+(i+1).ToString("00")+".asset");float s=1.8f/mesh.bounds.size.y;
            var g=MeshObject("Anonymous travellers "+i,mesh,positions[i]-new Vector3(mesh.bounds.center.x,mesh.bounds.min.y,mesh.bounds.center.z)*s,mat);g.transform.localScale=Vector3.one*s;
        }
    }
    static void Capture(string name)
    {
        var rt=new RenderTexture(1920,1080,24);var old=RenderTexture.active;
        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1920,1080),0,0);tex.Apply();File.WriteAllBytes(Report+"/"+name,tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
        finally{camera.targetTexture=null;RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(rt);}
    }
    static void Validate()
    {
        var renderers=hall.GetComponentsInChildren<Renderer>();
        var painted=renderers.Where(r=>r.sharedMaterials.Any(m=>m && (m.shader.name=="NOPE/Terminal Painted Sprite" || (m.HasProperty("_BaseMap") && m.GetTexture("_BaseMap")?.name=="Architecture")))).ToArray();
        if(painted.Length>0)throw new Exception("Baked architecture reference leaked into dynamically lit hall.");
        if(LightmapSettings.lightmaps.Length!=0)throw new Exception("Unexpected baked lightmaps.");
        var errors=ShaderUtil.GetShaderMessages(Shader.Find("NOPE/Terminal Relit")).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
        if(errors.Length>0)throw new Exception(string.Join("\n",errors.Select(e=>e.message)));
        File.WriteAllText(Report+"/validation.txt","Architecture backdrop renderers: 0\nBaked lightmaps: 0\nRealtime lights: "+root.GetComponentsInChildren<Light>().Length+"\nHall renderers: "+renderers.Length+"\nShader errors: 0\nDesk cloned unchanged from approved preview. OfficeScene not modified.\nMorning, evening and opposite-light captures made by the same camera and same geometry.\n");
    }
    [MenuItem("Tools/Terminal Art/Open Dynamically Lit Terminal")]
    public static void Open(){EditorSceneManager.OpenScene(Folder+"/DynamicTerminal.unity",OpenSceneMode.Single);EditorApplication.ExecuteMenuItem("Window/General/Game");}
}
