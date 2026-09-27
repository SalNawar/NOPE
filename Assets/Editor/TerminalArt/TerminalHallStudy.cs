using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

// Art-only authoring. Produces a separate study scene; never saves OfficeScene.
public static class TerminalHallStudy
{
    const string Folder = "Assets/Art/Office/TerminalStudy";
    const string Output = "ArtDeliverables/TimeDesk/HallLayers/UnityStudy";
    static Transform root;
    static Material stone, dark, brass, burgundy, floor, blue;
    static Scene sourceScene;

    [MenuItem("Tools/Terminal Art/Create Isolated Hall Study")]
    public static void Build()
    {
        ShaderUtil.allowAsyncCompilation=false;
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play mode before authoring hall art.");
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Output);
        AssetDatabase.Refresh();
        var original = SceneManager.GetActiveScene();
        var source = SceneManager.GetSceneByPath("Assets/Scenes/OfficeScene.unity");
        bool opened = !source.isLoaded;
        if (opened) source = EditorSceneManager.OpenScene("Assets/Scenes/OfficeScene.unity", Application.isBatchMode ? OpenSceneMode.Single : OpenSceneMode.Additive);
        sourceScene=source;
        var study = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(study);
        try
        {
            root = new GameObject("Terminal Hall — art study").transform;
            stone = Mat("Limestone", "E7DFCF"); dark = Mat("Charcoal", "34343F");
            brass = Mat("Aged brass", "9E784A"); burgundy = Mat("Civic burgundy", "703B4B");
            floor = Mat("Muted stone floor", "928775"); blue = Mat("Portal energy study", "3980B1", true);
            stone.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/DebtRelief/Textures/WornPlaster.png"));
            floor=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Office/Hybrid/BlenderOffice/Materials/Hall_Floor.mat");
            CopyDesk(source);
            Architecture();
            Platforms();
            Furnishings();
            City();
            Crowds();
            var deskCopy=root.Find("Approved desk — visual copies only");
            deskCopy.SetParent(null,true);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Folder+"/TerminalHallArt.prefab");
            deskCopy.SetParent(root,true);
            var sun = new GameObject("Study daylight").AddComponent<Light>();
            sun.transform.SetParent(root); sun.type = LightType.Directional; sun.intensity = 1.15f;
            sun.color = new Color(1,.88f,.73f); sun.transform.rotation = Quaternion.Euler(38,-55,0);
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.57f,.57f,.63f);
            var camera = new GameObject("Study player camera").AddComponent<Camera>();
            camera.GetUniversalAdditionalCameraData().SetRenderer(1);
            camera.transform.SetParent(root); camera.transform.position = new Vector3(0,2.16f,-2.62f);
            camera.transform.rotation = Quaternion.Euler(10,0,0); camera.fieldOfView = 55;
            camera.nearClipPlane = .05f; camera.farClipPlane = 700;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.69f,.76f,.82f);
            foreach(var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=30;
            camera.cullingMask=1<<30; sun.cullingMask=1<<30;
            foreach(var guid in AssetDatabase.FindAssets("",new[]{Folder}))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
            EditorSceneManager.SaveScene(study, Folder + "/TerminalHallStudy.unity");
            double ready=EditorApplication.timeSinceStartup+8;
            EditorApplication.CallbackFunction finish=null;
            finish=()=> {
                EditorApplication.QueuePlayerLoopUpdate();
                if(EditorApplication.timeSinceStartup<ready)return;
                EditorApplication.update-=finish;
                try {
                    Capture(camera,"forward.png");
                    camera.transform.rotation=Quaternion.Euler(12,-60,0);Capture(camera,"left-pan.png");
                    camera.transform.rotation=Quaternion.Euler(10,0,0);
                    File.WriteAllText(Output+"/build-result.txt","Built isolated Unity art study. OfficeScene not saved or modified.\nRenderers: "+root.GetComponentsInChildren<Renderer>().Length);
                    if(Application.isBatchMode)EditorApplication.Exit(0);
                } catch(Exception e) { Debug.LogException(e); if(Application.isBatchMode)EditorApplication.Exit(1); }
            };
            EditorApplication.update+=finish;
        }
        finally
        {
            if (opened) EditorSceneManager.CloseScene(source, true);
            if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
        }
    }

    static Material Mat(string name, string hex, bool unlit=false)
    {
        string path=Folder+"/"+name+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m=new Material(Shader.Find(unlit?"Universal Render Pipeline/Unlit":"NOPE/Desk Anime")); AssetDatabase.CreateAsset(m,path); }
        m.shader=Shader.Find(unlit?"Universal Render Pipeline/Unlit":"NOPE/Desk Anime");
        ColorUtility.TryParseHtmlString("#"+hex,out var c); m.color=c;
        if(m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness",.12f);
        EditorUtility.SetDirty(m); return m;
    }
    static GameObject Box(string name, Vector3 p, Vector3 size, Material material)
    {
        var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(root);
        g.transform.position=p; g.transform.localScale=size; g.GetComponent<Renderer>().sharedMaterial=material;
        if(material==floor)
        {
            string path=Folder+"/"+name+" UV.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(!mesh){mesh=UnityEngine.Object.Instantiate(g.GetComponent<MeshFilter>().sharedMesh);AssetDatabase.CreateAsset(mesh,path);}
            var verts=mesh.vertices;var normals=mesh.normals;var uv=new Vector2[verts.Length];
            for(int i=0;i<verts.Length;i++)
            {
                var w=Vector3.Scale(verts[i],size)+p;
                uv[i]=Mathf.Abs(normals[i].y)>.5f?new Vector2(w.x,w.z)*.5f:new Vector2(w.x+w.z,w.y)*.5f;
            }
            mesh.uv=uv;EditorUtility.SetDirty(mesh);g.GetComponent<MeshFilter>().sharedMesh=mesh;
        }
        else if(material!=null && material.shader.name=="NOPE/Desk Anime" && Mathf.Min(size.x,Mathf.Min(size.y,size.z))>.25f)
        {
            g.GetComponent<MeshFilter>().sharedMesh=SoftBox(size);
            g.transform.localScale=Vector3.one;
        }
        UnityEngine.Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
    }
    static Mesh SoftBox(Vector3 size)
    {
        string key=string.Join("_",new[]{size.x,size.y,size.z}.Select(v=>v.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)));
        string path=Folder+"/Bevel_"+key+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
        float r=Mathf.Min(.08f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.16f);
        var core=size*.5f-Vector3.one*r;
        var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        foreach(var n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
        {
            var u=Mathf.Abs(n.y)>.5f?Vector3.right:Vector3.up;var v=Vector3.Cross(n,u);
            float eu=Mathf.Abs(Vector3.Dot(u,size))*.5f,ev=Mathf.Abs(Vector3.Dot(v,size))*.5f,en=Mathf.Abs(Vector3.Dot(n,size))*.5f;
            var us=new[]{-eu,-eu+r,eu-r,eu};var vs=new[]{-ev,-ev+r,ev-r,ev};int first=vertices.Count;
            for(int j=0;j<4;j++)for(int i=0;i<4;i++)
            {
                var p=n*en+u*us[i]+v*vs[j];var c=new Vector3(Mathf.Clamp(p.x,-core.x,core.x),Mathf.Clamp(p.y,-core.y,core.y),Mathf.Clamp(p.z,-core.z,core.z));
                var normal=(p-c).normalized;vertices.Add(c+normal*r);normals.Add(normal);uv.Add(new Vector2(us[i]/(2*eu)+.5f,vs[j]/(2*ev)+.5f));
            }
            for(int j=0;j<3;j++)for(int i=0;i<3;i++){int a=first+j*4+i;triangles.AddRange(new[]{a,a+1,a+5,a,a+5,a+4});}
        }
        var mesh=new Mesh{name="Soft terminal block "+key};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static void Text(string text, Vector3 p, float size, Color color)
    {
        var g=new GameObject(text); g.transform.SetParent(root); g.transform.position=p;
        var t=g.AddComponent<TextMesh>(); t.text=text; t.fontSize=64; t.characterSize=size*.55f;
        t.anchor=TextAnchor.MiddleCenter; t.alignment=TextAlignment.Center; t.color=color;
    }
    static string PathOf(Transform t) => t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    static void CopyDesk(Scene source)
    {
        var group=new GameObject("Approved desk — visual copies only").transform; group.SetParent(root);
        foreach(var r in source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)))
        {
            string p=PathOf(r.transform);
            if(!r.enabled || !r.gameObject.activeInHierarchy || !(p.StartsWith("HybridOffice/Booth/") || p.StartsWith("ImportedOfficeDress/Desk/"))) continue;
            var mesh=r.GetComponent<MeshFilter>(); if(!mesh || !mesh.sharedMesh)continue;
            var g=new GameObject(r.name); g.transform.SetParent(group);
            g.transform.SetPositionAndRotation(r.transform.position,r.transform.rotation); g.transform.localScale=r.transform.lossyScale;
            g.AddComponent<MeshFilter>().sharedMesh=mesh.sharedMesh;
            g.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
        }
    }
    static void Rail(Vector3 a, Vector3 b)
    {
        var delta=b-a; int steps=Mathf.CeilToInt(delta.magnitude/3);
        for(int i=0;i<=steps;i++) Box("Guard post",Vector3.Lerp(a,b,(float)i/steps)+Vector3.up*.55f,new Vector3(.07f,1.1f,.07f),dark);
        var bar=Box("Guard rail",(a+b)*.5f+Vector3.up*1.08f,new Vector3(.075f,.075f,delta.magnitude),brass);
        bar.transform.rotation=Quaternion.LookRotation(delta);
    }
    static void Architecture()
    {
        // Same desk datum, lower platforms below it. No edits to original floor mesh.
        Box("Inspection concourse",new Vector3(18,-.3f,-.75f),new Vector3(60,.6f,10.5f),floor);
        Box("Lower departure floor",new Vector3(18,-6.3f,40),new Vector3(60,.6f,71),floor);
        Box("East upper gallery",new Vector3(35,5.65f,46),new Vector3(26,.7f,54),stone);
        Box("Upper crossing",new Vector3(18,5.65f,56),new Vector3(60,.7f,4),stone);
        Box("Roof",new Vector3(18,24.5f,35),new Vector3(61,1,82),stone);
        for(int z=8;z<=72;z+=16)
        {
            foreach(float x in new[]{-11f,30f,47f})
            {
                Box("Monumental pier",new Vector3(x,9,z),new Vector3(1.6f,30,1.9f),stone);
                Box("Pier plinth",new Vector3(x,-5.1f,z),new Vector3(2.3f,1.8f,2.5f),dark);
                Box("Capital",new Vector3(x,22.3f,z),new Vector3(2.3f,1,2.7f),brass);
            }
            Box("Roof cross beam",new Vector3(18,23,z),new Vector3(60,1.1f,.7f),dark);
            Box("West window mullion",new Vector3(-12,9,z),new Vector3(.15f,30,.16f),dark);
            Box("Civic banner",new Vector3(29.05f,15,z-.2f),new Vector3(.06f,8,2),burgundy);
        }
        for(int y=0;y<=18;y+=6) Box("West transom",new Vector3(-12,y,35),new Vector3(.14f,.12f,81),dark);
        var glass=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Office/DebtRelief/Materials/Hall_ClearGlass.mat");
        Box("West clear glazing",new Vector3(-12,9,35),new Vector3(.035f,30,81),glass);
        Box("Concourse clear guard",new Vector3(-3,.52f,4.5f),new Vector3(14,1.04f,.025f),glass);
        Box("East wall",new Vector3(48.3f,14,39),new Vector3(.6f,20,74),stone);
        Box("Rear wall",new Vector3(18,9,76),new Vector3(60,30,.8f),stone);
        for(int x=-8;x<48;x+=8)
        {
            Box("Rear pilaster",new Vector3(x,9,75.3f),new Vector3(1.2f,30,.7f),stone);
            Box("Rear civic hanging",new Vector3(x+3,14,75.05f),new Vector3(2.5f,9,.06f),burgundy);
            Box("Rear brass cornice",new Vector3(x+3,19,74.95f),new Vector3(3,.15f,.15f),brass);
        }
        for(int z=16;z<=72;z+=16)
        {
            Box("Gallery support",new Vector3(22,-.3f,z),new Vector3(1.4f,12.6f,1.4f),stone);
            Box("Gallery capital",new Vector3(22,4.9f,z),new Vector3(2.1f,.6f,2.1f),brass);
            Box("Left wall dado",new Vector3(-11.8f,-4.7f,z),new Vector3(.35f,2.6f,14),dark);
            foreach(float x in new[]{-11f,30f,47f})
            {
                Box("Pier recessed stripe",new Vector3(x-.55f,10,z-1.01f),new Vector3(.09f,23,.07f),brass);
                Box("Pier recessed stripe",new Vector3(x+.55f,10,z-1.01f),new Vector3(.09f,23,.07f),brass);
            }
        }
        for(int x=-6;x<48;x+=8) Box("Roof longitudinal rib",new Vector3(x,23.3f,35),new Vector3(.3f,.6f,82),dark);
        // Frameless glazing across the main sightline avoids a rail bisecting gate 01.
        Rail(new Vector3(13,0,4.5f),new Vector3(47,0,4.5f));
        Rail(new Vector3(22,6,19),new Vector3(22,6,73));
        Rail(new Vector3(-11,6,54),new Vector3(47,6,54));
        // Broad stair beside the booth, 36 comfortable risers to the lower floor.
        for(int i=0;i<36;i++) Box("Departure stair "+i,new Vector3(8.5f,-(i+1)/6f-.1f,4.7f+i*.35f),new Vector3(6,.2f,.35f),stone);
        Rail(new Vector3(5.4f,0,4.5f),new Vector3(5.4f,-6,17.3f));
        Rail(new Vector3(11.6f,0,4.5f),new Vector3(11.6f,-6,17.3f));
        Box("Lift core",new Vector3(26,3,24),new Vector3(4,18,4),dark);
        Box("Upper lift link",new Vector3(29,5.65f,24),new Vector3(6,.7f,4),stone);
        Text("LIFT",new Vector3(26,1.7f,21.95f),.15f,Color.white);
        Box("Right arrival corridor floor",new Vector3(41,-.25f,8),new Vector3(14,.5f,12),floor);
        Box("Arrival corridor canopy",new Vector3(41,5,8),new Vector3(14,.6f,12),stone);
        Text("MEDBAY   /   JAIL   /   C-SUITES  >",new Vector3(39,4.2f,3),.19f,Color.white);
    }
    static void Gate(int number, Vector3 foot)
    {
        Box("Platform "+number,foot+new Vector3(0,-.15f,0),new Vector3(9,.3f,6),dark);
        var originals=sourceScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>PathOf(r.transform).StartsWith("HybridOffice/Hall/Blender_PortalRing/") || PathOf(r.transform).StartsWith("HybridOffice/Hall/Hybrid_PortalOpening/"));
        foreach(var r in originals)
        {
            var mf=r.GetComponent<MeshFilter>();if(!mf || !mf.sharedMesh)continue;
            var g=new GameObject("Gate "+number+" / "+r.name);g.transform.SetParent(root);
            g.transform.SetPositionAndRotation(r.transform.position+foot-new Vector3(0,0,20),r.transform.rotation);g.transform.localScale=r.transform.lossyScale;
            g.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;g.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
        }
        Vector3 centre=foot+Vector3.up*3.2f;
        Box("Gate number",centre+new Vector3(0,4,-.1f),new Vector3(2.5f,1.3f,.2f),dark);
        Text(number.ToString("00"),centre+new Vector3(0,4,-.22f),.38f,new Color(.95f,.85f,.6f));
    }
    static void Platforms()
    {
        Gate(1,new Vector3(2,-6,27)); Gate(2,new Vector3(21,-6,47));
        Gate(3,new Vector3(27,6,40)); Gate(4,new Vector3(33,6,63));
        Box("Departure board",new Vector3(6,10,37),new Vector3(13,4,.4f),dark);
        Text("DEPARTURES",new Vector3(6,11.35f,36.75f),.27f,new Color(.9f,.7f,.35f));
        Text("01   CLEARANCE REQUIRED\n02   BOARDING\n03   AWAIT ASSIGNMENT\n04   HOLD",new Vector3(6,9.45f,36.75f),.14f,new Color(.78f,.74f,.64f));
    }
    static void Furnishings()
    {
        for(int x=0;x<5;x++) for(int y=0;y<3;y++)
        {
            Box("Luggage locker",new Vector3(-8+x*1.05f,-4.9f+y*.9f,18),new Vector3(1,.85f,1.2f),dark);
            Box("Locker lock",new Vector3(-7.65f+x*1.05f,-4.9f+y*.9f,17.37f),new Vector3(.09f,.12f,.04f),brass);
        }
        Box("Painting frame",new Vector3(-10,2,30),new Vector3(.15f,3,2),brass);
        Box("Faded painting placeholder",new Vector3(-9.9f,2,30),new Vector3(.03f,2.65f,1.65f),burgundy);
        Box("Artifact display plinth",new Vector3(29,-5,32),new Vector3(1.5f,2,1.5f),stone);
    }
    static void City()
    {
        foreach(var r in sourceScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).Where(r=>PathOf(r.transform).StartsWith("HybridOffice/Exterior/Blender_Megacity")))
        {
            var mf=r.GetComponent<MeshFilter>();if(!mf || !mf.sharedMesh)continue;
            var g=new GameObject("City / "+r.name);g.transform.SetParent(root);
            g.transform.SetPositionAndRotation(r.transform.position*.6f+new Vector3(-90,-25,-20),r.transform.rotation);g.transform.localScale=r.transform.lossyScale*.6f;
            g.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;g.AddComponent<MeshRenderer>().sharedMaterials=r.sharedMaterials;
        }
        var colors=new[]{"B08273","9C969C","9A8A74","79818F","B7A6A0"};
        var sky=GameObject.CreatePrimitive(PrimitiveType.Quad);sky.name="West sky backdrop";sky.transform.SetParent(root);
        sky.transform.position=new Vector3(-260,10,20);sky.transform.rotation=Quaternion.Euler(0,-90,0);sky.transform.localScale=new Vector3(500,300,1);
        sky.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Office/Hybrid/Materials/sky.mat");UnityEngine.Object.DestroyImmediate(sky.GetComponent<Collider>());
        for(int i=0;i<24;i++)
        {
            float h=30+(i*17%65); float x=-40-(i%4)*28; float z=-25+(i/4)*28;
            Box("Distant city block "+i,new Vector3(x-30,-100+h*.5f,z),new Vector3(14,h,16),Mat("City "+i%5,colors[i%5]));
        }
    }
    static void Crowds()
    {
        var positions=new[]{new Vector3(-5,-6,23),new Vector3(12,-6,34),new Vector3(4,-6,47),new Vector3(28,6,31),new Vector3(24,6,52),new Vector3(8,6,56)};
        for(int i=0;i<positions.Length;i++)
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Office/HallCrowds/Meshes/CrowdGroup_"+(i+1).ToString("00")+".asset");
            if(!mesh)continue;
            string p=Folder+"/Crowd "+i+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);
            if(!m){m=new Material(Shader.Find("NOPE/Hall Crowd Silhouette"));AssetDatabase.CreateAsset(m,p);}
            m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/HallCrowds/Textures/CrowdGroups_Atlas.png"));
            m.SetColor("_Tint",new Color(.48f,.47f,.50f));EditorUtility.SetDirty(m);
            var g=new GameObject("Anonymous group "+i);g.transform.SetParent(root);float scale=1.8f/mesh.bounds.size.y;
            g.transform.position=positions[i]-Vector3.up*mesh.bounds.min.y*scale;g.transform.localScale=Vector3.one*scale;
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=m;
        }
    }
    static void Capture(Camera camera,string name)
    {
        var rt=new RenderTexture(1920,1080,24); var previous=RenderTexture.active;
        try { camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes(Output+"/"+name,image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
        } finally { camera.targetTexture=null;RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(rt); }
    }
}
