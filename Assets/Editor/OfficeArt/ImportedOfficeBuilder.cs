using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

// Authoring only: source pack prefabs stay untouched. All placement and materials
// are saved in the scene; the game has no dependency on this builder.
public static class ImportedOfficeBuilder
{
    const string Root = "ImportedOfficeDress";
    const string Folder = "Assets/Art/Office/ImportedOffice";
    const string Manifest = "ArtDeliverables/TimeDesk/ImportedOffice/layout.json";
    [Serializable] public class Item
    {
        public string name, group, prefab;
        public Vector3 position;
        public float yaw, pitch, roll, scale = 1;
        public Vector3 size;
    }
    [Serializable] public class Pose { public string path; public Vector3 position, rotation, scale; }
    [Serializable] public class Layout { public Item[] items; public string[] disablePaths; public Pose[] poses; }
    [Serializable] public class Placed
    {
        public string name, source, group;
        public Vector3 min, max;
        public int triangles;
    }
    [Serializable] public class Report { public List<Placed> placed = new(); }
    static readonly Dictionary<string, Material> materials = new();

    [MenuItem("Tools/Office Art/Check Paint Shader")]
    public static void CheckShader()
    {
        var shader=Shader.Find("NOPE/Office Painted Surface");
        File.WriteAllLines("ArtDeliverables/TimeDesk/ImportedOffice/shader_errors.txt",
            ShaderUtil.GetShaderMessages(shader).Select(x=>$"{x.severity}: {x.file}:{x.line}: {x.message}"));
    }

    [MenuItem("Tools/Office Art/Build Imported Office")]
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Leave Play mode before authoring.");
        if(EditorSceneManager.GetActiveScene().path != "Assets/Scenes/OfficeScene.unity")
            throw new InvalidOperationException("Open OfficeScene before authoring.");
        var layout=JsonUtility.FromJson<Layout>(File.ReadAllText(Manifest));
        foreach(var item in layout.items)
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(item.prefab)) throw new FileNotFoundException(item.prefab);
        if(!Shader.Find("NOPE/Office Painted Surface")) throw new InvalidOperationException("Paint shader has not imported.");
        Directory.CreateDirectory(Folder+"/Materials");
        Undo.IncrementCurrentGroup(); int undo=Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Reconstruct office from imported packs");
        var old=GameObject.Find(Root); if(old) Undo.DestroyObjectImmediate(old);
        var root=new GameObject(Root); Undo.RegisterCreatedObjectUndo(root,"Imported office dress");
        foreach(var path in layout.disablePaths ?? Array.Empty<string>())
        {
            var go=GameObject.Find(path);if(!go)continue;
            Undo.RecordObject(go,"Replace original art");go.SetActive(false);
        }
        foreach(var pose in layout.poses ?? Array.Empty<Pose>())
        {
            var go=GameObject.Find(pose.path);if(!go)throw new InvalidOperationException("Missing scene binding: "+pose.path);
            Undo.RecordObject(go.transform,"Place existing functional art");
            go.transform.SetPositionAndRotation(pose.position,Quaternion.Euler(pose.rotation));
            if(pose.scale!=Vector3.zero)go.transform.localScale=pose.scale;
        }
        var groups=new Dictionary<string,Transform>(); materials.Clear(); var report=new Report();
        try
        {
            foreach(var item in layout.items)
            {
                if(!groups.TryGetValue(item.group,out var group))
                {
                    group=new GameObject(item.group).transform;group.SetParent(root.transform,false);groups.Add(item.group,group);
                }
                var pivot=new GameObject(item.name).transform;pivot.SetParent(group,false);
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(item.prefab),pivot);
                instance.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);
                instance.transform.localScale=Vector3.one;
                foreach(var script in instance.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled=false;
                foreach(var light in instance.GetComponentsInChildren<Light>(true)) light.enabled=false;
                foreach(var audio in instance.GetComponentsInChildren<AudioSource>(true)) audio.enabled=false;
                foreach(var particles in instance.GetComponentsInChildren<ParticleSystem>(true)) particles.gameObject.SetActive(false);
                var renderers=instance.GetComponentsInChildren<MeshRenderer>();
                if(renderers.Length==0) throw new InvalidOperationException("No geometry: "+item.prefab);
                Bounds b=BoundsOf(renderers);
                // Normalize source pivots to visible bottom-centre before sizing and rotating.
                instance.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
                foreach(var r in renderers)
                {
                    var sourceMaterials=r.sharedMaterials;
                    if(item.group=="Hall floor" && item.name.StartsWith("Floor tile"))sourceMaterials=sourceMaterials.Select(_=>AssetDatabase.LoadAssetAtPath<Material>("Assets/80s_Office/Materials/MI_Floor_Tiles.mat")).ToArray();
                    r.sharedMaterials=sourceMaterials.Select(Convert).ToArray();
                    r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
                }
                pivot.localScale=item.size==Vector3.zero?Vector3.one*item.scale:new Vector3(
                    item.size.x/Mathf.Max(b.size.x,.001f),item.size.y/Mathf.Max(b.size.y,.001f),item.size.z/Mathf.Max(b.size.z,.001f));
                pivot.rotation=Quaternion.Euler(item.pitch,item.yaw,item.roll);
                b=BoundsOf(renderers);
                pivot.position+=item.position-new Vector3(b.center.x,b.min.y,b.center.z);
                b=BoundsOf(renderers);
                report.placed.Add(new Placed { name=item.name,source=item.prefab,group=item.group,min=b.min,max=b.max,
                    triangles=instance.GetComponentsInChildren<MeshFilter>().Sum(x=>x.sharedMesh?(int)Enumerable.Range(0,x.sharedMesh.subMeshCount).Sum(n=>(long)x.sharedMesh.GetIndexCount(n)/3):0) });
            }
            ConnectMonitor(root.transform);
            AddTaskLight(root.transform);
            ConfigureHallLighting(root.transform);
            AddHybridLayers(root.transform);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            File.WriteAllText("ArtDeliverables/TimeDesk/ImportedOffice/placed.json",JsonUtility.ToJson(report,true));
            Selection.activeGameObject=root;
            Debug.Log($"Imported office rebuilt: {report.placed.Count} assets, {materials.Count} styled material variants.");
        }
        finally { Undo.CollapseUndoOperations(undo); }
    }

    static Bounds BoundsOf(Renderer[] renderers)
    {
        Bounds b=renderers[0].bounds;foreach(var r in renderers.Skip(1))b.Encapsulate(r.bounds);return b;
    }

    static Material Convert(Material source)
    {
        if(!source)return null;
        string sourcePath=AssetDatabase.GetAssetPath(source);
        if(materials.TryGetValue(sourcePath,out var cached))return cached;
        string name=source.name.Replace(" ","_");
        string path=$"{Folder}/Materials/{name}_Painted.mat";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat) {mat=new Material(Shader.Find("NOPE/Office Painted Surface"));AssetDatabase.CreateAsset(mat,path);}
        mat.shader=Shader.Find("NOPE/Office Painted Surface");
        bool sci=sourcePath.Contains("Creepy_Cat");
        string map=source.HasProperty("_Albedo")?"_Albedo":source.HasProperty("_BaseMap")?"_BaseMap":"_MainTex";
        var albedo=source.HasProperty(map)?source.GetTexture(map):null;
        if(name=="M_Office_Furniture")
        {
            var painted=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/office_furniture_painted.png");
            if(painted)albedo=painted;
        }
        if(name=="HD_Stuff_02_Norm")
        {
            var painted=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/console_painted.png");
            if(painted)albedo=painted;
        }
        Color tint=sci?new Color(.84f,.84f,.75f):Color.white;
        if(sourcePath.Contains("80s_Office"))
        {
            if(source.HasProperty("_Tint")) {var t=source.GetColor("_Tint");t.a=1;tint=t;}
            if(name.Contains("Sofa") || name.Contains("ChairSimple") || name.Contains("FileCabinets"))tint=new Color(.38f,.52f,.46f);
            if(name.Contains("LoungeChair"))tint=new Color(.67f,.4f,.23f);
            if(name=="MI_Floor_Tiles")tint=new Color(.86f,.86f,.8f);
            if(name=="MI_WoodenFurniture" || name=="MI_Wood" || name=="MI_WorkTable")tint=new Color(.85f,.75f,.64f);
            if(name=="MI_Ceilings")tint=new Color(.82f,.85f,.81f);
            if(name.Contains("Walls") || name=="MI_Concrete")tint=new Color(.83f,.82f,.72f);
            if(name=="MI_LampDesk")tint=new Color(.7f,.83f,.7f);
        }
        bool blank=name=="M_Paper" || name=="PlaceHolder_Paper" || name=="MI_ComputerScreen" || name.StartsWith("HUD_Screen") || name.StartsWith("M_Office_Accessories");
        if(blank)
        {
            albedo=null;
            tint=name.StartsWith("HUD") || name=="MI_ComputerScreen"?new Color(.035f,.105f,.115f):name.Contains("Accessories")?new Color(.22f,.36f,.34f):new Color(.88f,.84f,.7f);
        }
        if(name.Contains("Walls") || name=="PlaceHolder_PlasticFrame_Light") {albedo=null;tint=new Color(.73f,.70f,.61f);}
        if(name=="MI_WorkTable")tint=new Color(.45f,.48f,.51f);
        if(name=="M_Office_Furniture")tint=new Color(.68f,.77f,.71f);
        if(name=="MI_LampDesk")
        {
            var lamp=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/lamp_painted.png");if(lamp)albedo=lamp;
            tint=Color.white;
        }
        if(name=="HD_Floor_02_Norm")tint=new Color(.28f,.35f,.36f);
        mat.SetTexture("_BaseMap",albedo);mat.SetColor("_BaseColor",tint);
        mat.SetFloat("_Saturation",sci?.6f:.88f);mat.SetFloat("_Contrast",sci?.8f:.92f);
        mat.SetFloat("_MipBias",sci?1.2f:.7f);mat.SetFloat("_AccentRemap",sci?1:0);
        if(name=="MI_WorkTable"){mat.SetFloat("_Saturation",.5f);mat.SetFloat("_Contrast",.8f);}
        mat.SetColor("_AccentColor",new Color(.16f,.29f,.27f));
        mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",name.Contains("Glass")?.5f:.22f);
        if(name.Contains("Floor") || name.Contains("Ceiling"))mat.SetFloat("_Smoothness",.08f);
        mat.SetTexture("_BumpMap",source.HasProperty("_Normal")?source.GetTexture("_Normal"):source.HasProperty("_BumpMap")?source.GetTexture("_BumpMap"):null);
        mat.SetFloat("_BumpScale",blank?0:sci?.15f:.18f);
        if(name.Contains("Walls") || name.Contains("Floor") || name=="PlaceHolder_PlasticFrame_Light")mat.SetFloat("_BumpScale",0);
        mat.SetTexture("_EmissionMap",null);mat.SetColor("_EmissionColor",Color.black);
        if(source.HasProperty("_EmissionMap") && name.Contains("Stuff_01"))
        {
            mat.SetTexture("_EmissionMap",source.GetTexture("_EmissionMap"));mat.SetColor("_EmissionColor",new Color(.35f,.3f,.17f));
        }
        bool clip=source.IsKeywordEnabled("_ALPHATEST_ON") || name.Contains("Alpha");
        mat.SetFloat("_AlphaClip",clip?1:0);mat.SetFloat("_Cutoff",.4f);mat.SetFloat("_Cull",2);
        if(clip)mat.EnableKeyword("_ALPHATEST_ON");else mat.DisableKeyword("_ALPHATEST_ON");
        mat.enableInstancing=true; EditorUtility.SetDirty(mat);materials[sourcePath]=mat;return mat;
    }

    static void ConnectMonitor(Transform root)
    {
        var pc=root.Find("Desk/Retro CRT");if(!pc)return;
        foreach(var filter in pc.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=filter.GetComponent<Renderer>();var mesh=filter.sharedMesh;
            for(int slot=0;slot<renderer.sharedMaterials.Length;slot++)
            {
                if(!renderer.sharedMaterials[slot].name.Contains("ComputerScreen"))continue;
                var local=mesh.GetSubMesh(slot).bounds;
                var center=filter.transform.TransformPoint(local.center);
                var proxy=GameObject.Find("OfficeRoot/CRTMonitor");
                Undo.RecordObject(proxy.transform,"Align CRT interaction");proxy.transform.position=center;
                var hit=proxy.GetComponent<BoxCollider2D>();Undo.RecordObject(hit,"Fit CRT hit target");
                hit.size=new Vector2(.64f,.48f);
                var zoom=GameObject.Find("Cameras/MonitorVCam").transform;
                Undo.RecordObject(zoom,"Fit monitor zoom");
                var normal=pc.rotation*Vector3.back;
                zoom.position=center+normal*.87f+Vector3.up*.035f;zoom.LookAt(center);
                File.WriteAllText("ArtDeliverables/TimeDesk/ImportedOffice/monitor_binding.json",JsonUtility.ToJson(new Pose {path=proxy.name,position=center,rotation=zoom.eulerAngles,scale=local.size},true));
                return;
            }
        }
        throw new InvalidOperationException("Imported CRT screen submesh not found; interaction was not moved.");
    }

    static void AddTaskLight(Transform root)
    {
        var obj=new GameObject("Realtime desk lamp light");obj.transform.SetParent(root,false);
        obj.transform.position=new Vector3(1.65f,1.62f,.15f);obj.transform.LookAt(new Vector3(.35f,1.07f,-.45f));
        var light=obj.AddComponent<Light>();light.type=LightType.Spot;light.color=new Color(1,.79f,.52f);
        light.intensity=1.25f;light.range=4;light.spotAngle=108;light.innerSpotAngle=70;light.shadows=LightShadows.Soft;
    }

    static void ConfigureHallLighting(Transform root)
    {
        var sun=GameObject.Find("HybridOffice/OfficeDaylight").GetComponent<Light>();
        Undo.RecordObject(sun,"Soften office daylight");sun.shadowBias=.65f;sun.shadowNormalBias=.45f;sun.shadowStrength=.58f;
        var fill=GameObject.Find("HybridOffice/OfficeInteriorFill").GetComponent<Light>();
        Undo.RecordObject(fill,"Balance indoor fill");fill.intensity=.55f;
        foreach(float x in new[]{-4.5f,4.5f})
        {
            var go=new GameObject("Ceiling bounce "+x);go.transform.SetParent(root,false);go.transform.position=new Vector3(x,5.8f,11);
            var light=go.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(.91f,.91f,.81f);light.intensity=24;light.range=17;
        }
    }

    static void AddHybridLayers(Transform root)
    {
        // Real SpriteRenderer cards, not meshes pretending to be additional props.
        const string paper="Assets/Art/Office/Hybrid/Images/floor_paper.png";
        var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(paper);
        if(tex)
        {
            string path=Folder+"/PaperSprite.asset";
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if(!sprite){sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100);AssetDatabase.CreateAsset(sprite,path);}
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/PaperSprite.mat");
            if(!mat){mat=new Material(Shader.Find("NOPE/Office Painted Surface"));AssetDatabase.CreateAsset(mat,Folder+"/Materials/PaperSprite.mat");}
            mat.SetTexture("_BaseMap",tex);mat.SetFloat("_Cull",0);mat.SetFloat("_AlphaClip",1);mat.EnableKeyword("_ALPHATEST_ON");mat.SetFloat("_Smoothness",0);mat.SetFloat("_BumpScale",0);
            SpriteCard(root,sprite,mat,"Desk loose document",new Vector3(1.23f,1.074f,-.38f),new Vector3(90,0,-12),.28f);
            SpriteCard(root,sprite,mat,"Hall maintenance notice",new Vector3(-8.57f,1.35f,7),new Vector3(0,-90,0),.35f);
            SpriteCard(root,sprite,mat,"Portal service notice",new Vector3(3.35f,1.55f,20.23f),new Vector3(0,0,0),.28f);
        }
        var city=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/painted_megacity.png");
        if(city)
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/Materials/PaintedMegacity.mat");
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(mat,Folder+"/Materials/PaintedMegacity.mat");}
            mat.SetTexture("_BaseMap",city);mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Cull",0);
            var go=GameObject.CreatePrimitive(PrimitiveType.Quad);go.name="Painted skyline layer";go.transform.SetParent(root,false);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.position=new Vector3(0,20,70);go.transform.localScale=new Vector3(140,78.75f,1);go.GetComponent<Renderer>().sharedMaterial=mat;
            go.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;
            var exterior=GameObject.Find("HybridOffice/Exterior");
            foreach(Transform child in exterior.transform)
                if(child.name.Contains("Megacity")){Undo.RecordObject(child.gameObject,"Replace backdrop with painted layer");child.gameObject.SetActive(false);}
        }
    }

    static void SpriteCard(Transform root,Sprite sprite,Material mat,string name,Vector3 position,Vector3 rotation,float width)
    {
        var go=new GameObject(name);go.transform.SetParent(root,false);go.transform.SetPositionAndRotation(position,Quaternion.Euler(rotation));
        go.transform.localScale=Vector3.one*(width/sprite.bounds.size.x);
        var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sharedMaterial=mat;
    }

}
