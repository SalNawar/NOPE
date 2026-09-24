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
        public float yaw, scale = 1;
    }
    [Serializable] public class Layout { public Item[] items; }
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
        var oldPencils=GameObject.Find("HybridOffice/Booth/Finish_PencilRack");
        if(oldPencils) {Undo.RecordObject(oldPencils,"Replace pencil organiser");oldPencils.SetActive(false);}
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
                    r.sharedMaterials=r.sharedMaterials.Select(Convert).ToArray();
                    r.shadowCastingMode=ShadowCastingMode.On;r.receiveShadows=true;
                }
                pivot.localScale=Vector3.one*item.scale;
                pivot.rotation=Quaternion.Euler(0,item.yaw,0);pivot.position=item.position;
                b=BoundsOf(renderers);
                report.placed.Add(new Placed { name=item.name,source=item.prefab,group=item.group,min=b.min,max=b.max,
                    triangles=instance.GetComponentsInChildren<MeshFilter>().Sum(x=>x.sharedMesh?(int)Enumerable.Range(0,x.sharedMesh.subMeshCount).Sum(n=>(long)x.sharedMesh.GetIndexCount(n)/3):0) });
            }
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
        }
        bool blank=name=="M_Paper" || name=="PlaceHolder_Paper" || name=="MI_ComputerScreen" || name.StartsWith("HUD_Screen") || name.StartsWith("M_Office_Accessories");
        if(blank)
        {
            albedo=null;
            tint=name.StartsWith("HUD") || name=="MI_ComputerScreen"?new Color(.035f,.105f,.115f):name.Contains("Accessories")?new Color(.22f,.36f,.34f):new Color(.88f,.84f,.7f);
        }
        mat.SetTexture("_BaseMap",albedo);mat.SetColor("_BaseColor",tint);
        mat.SetFloat("_Saturation",sci?.6f:.88f);mat.SetFloat("_Contrast",sci?.8f:.92f);
        mat.SetFloat("_MipBias",sci?1.2f:.7f);mat.SetFloat("_AccentRemap",sci?1:0);
        mat.SetColor("_AccentColor",new Color(.16f,.29f,.27f));
        mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",name.Contains("Glass")?.5f:.22f);
        mat.SetTexture("_BumpMap",source.HasProperty("_Normal")?source.GetTexture("_Normal"):source.HasProperty("_BumpMap")?source.GetTexture("_BumpMap"):null);
        mat.SetFloat("_BumpScale",blank?0:sci?.15f:.18f);
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

}
