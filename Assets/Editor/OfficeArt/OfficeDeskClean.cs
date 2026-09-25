using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Offline art installation. Existing readout objects and gameplay components stay in place.
public static class OfficeDeskClean
{
    const string Folder="Assets/Art/Office/DeskClean";
    const string Report="ArtDeliverables/TimeDesk/ImportedOffice/DeskClean";
    sealed class Binding
    {
        public string path,model;public Vector3 position,scale;public float yaw;public bool retainAnchor;
        public Binding(string p,string m,float x,float z,float s,float yaw=0,float y=1.06f,bool retain=false)
        {path=p;model=m;position=new Vector3(x,y,z);scale=Vector3.one*s;this.yaw=yaw;retainAnchor=retain;}
    }
    static Binding[] Bindings()=>new[]{
        new Binding("ImportedOfficeDress/Desk/Retro keyboard","Keyboard",-1.37f,-.60f,1.10f,-20),
        new Binding("HybridOffice/Booth/Finish_Mouse","Mouse",-.78f,-.75f,.90f,-5),
        new Binding("ImportedOfficeDress/Desk/Clerk hotline","Phone",-.62f,.36f,1.75f,8),
        new Binding("ImportedOfficeDress/Desk/Desk calculator","Calculator",1.03f,-.06f,1.60f,-8),
        new Binding("ImportedOfficeDress/Desk/Banker task lamp","Lamp",1.85f,.51f,1.05f,-8),
        new Binding("ImportedOfficeDress/Desk/Pen pot","PenPot",1.09f,.38f,1.80f,0),
        new Binding("ImportedOfficeDress/Desk/Forms stapler","Stapler",1.35f,-.22f,1.25f,18),
        new Binding("ImportedOfficeDress/Desk/Document out tray","Tray",1.72f,-.24f,.70f),
        new Binding("ImportedOfficeDress/Desk/Out tray forms","PaperBundle",1.72f,-.25f,1.20f,0,1.083f),
        new Binding("ImportedOfficeDress/Desk/Spare forms","PaperBundle",2.37f,.20f,1.20f,-10),
        new Binding("ImportedOfficeDress/Desk/Cabinet keys","Keys",-.83f,.66f,1.50f,-15),
        new Binding("ImportedOfficeDress/Desk/Reference binder","Binder",-2.30f,.35f,1.20f,-18),
        new Binding("HybridOffice/Booth/Finish_Till","Till",-1.09f,.39f,.70f,-5,1.06f,true),
        new Binding("HybridOffice/Booth/Finish_FormSorter","FormSorter",1.38f,.70f,1.08f,-6),
        new Binding("HybridOffice/Booth/Finish_Inkpad","Inkpad",1.07f,-.76f,.90f,5),
        new Binding("HybridOffice/Booth/Blender_Stamp","Stamp",1.35f,-.74f,.85f,12),
        new Binding("HybridOffice/Booth/Blender_Next","Next",0,.64f,.90f,0,1.06f,true),
        new Binding("HybridOffice/Booth/Finish_ComputerMedia","ComputerMedia",-1.0f,-.16f,1,-12),
        new Binding("HybridOffice/Booth/Finish_Mat","Blotter",.13f,-.52f,1,0,1.06f){scale=new Vector3(.68f,1,1)}
    };
    static readonly Dictionary<string,(string hex,float rough,float metal)> Palette=new(){
        {"ABS",("C5B89E",.75f,0)},{"Case",("A39883",.78f,0)},{"Grey",("ADAF9F",.78f,0)},
        {"Green",("496A60",.68f,0)},{"GreenDark",("344B46",.78f,0)},{"Teal",("5C8178",.73f,0)},
        {"Rubber",("2D3330",.92f,0)},{"Dark",("343C39",.83f,0)},{"Paper",("DBD1BA",.94f,0)},
        {"Manila",("C9B98C",.91f,0)},{"Wood",("896448",.76f,0)},{"Orange",("BB7F49",.73f,0)},
        {"Metal",("AAA999",.47f,.62f)},{"Brass",("A18A63",.55f,.42f)},{"Glass",("273333",.48f,0)},
        {"Ink",("505A51",.86f,0)},{"Pad",("5B5549",.90f,0)},{"PadEdge",("302D27",.91f,0)}
    };
    [Serializable] public class ItemAudit {public string path,model;public Vector3 position,size,min,max;}
    [Serializable] public class DeskAudit {public List<ItemAudit> items=new();public Vector3 deskMin,deskMax;public string note;}
    static bool Art(Renderer r)=>r is MeshRenderer && !r.GetComponent<TMPro.TMP_Text>() && !r.name.Contains("TMP");
    static Bounds BoundsOf(IEnumerable<Renderer> source)
    {
        var rs=source.ToArray();if(rs.Length==0)throw new InvalidOperationException("No art renderers.");
        Bounds b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    [MenuItem("Tools/Office Art/Apply Clean Desktop")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().path!="Assets/Scenes/OfficeScene.unity")
            throw new InvalidOperationException("Open OfficeScene in Edit mode.");
        var bindings=Bindings();
        foreach(var item in bindings)
        {
            if(!GameObject.Find(item.path))throw new InvalidOperationException("Missing desk target: "+item.path);
            if(!AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/Models/Clean_{item.model}.fbx"))
                throw new InvalidOperationException("Run author_desk_clean.py first: "+item.model);
        }
        var buriedPaper=GameObject.Find("HybridOffice/Booth/Blender_Newspaper");
        if(buriedPaper){Undo.RecordObject(buriedPaper,"Remove buried duplicate paper from desk");buriedPaper.SetActive(false);}
        Directory.CreateDirectory(Folder+"/Materials");AssetDatabase.Refresh();
        var materials=new Dictionary<string,Material>();
        foreach(var entry in Palette)
        {
            string name="DeskClean_"+entry.Key,path=$"{Folder}/Materials/{name}.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(mat,path);}
            ColorUtility.TryParseHtmlString("#"+entry.Value.hex,out var color);
            mat.SetColor("_BaseColor",color);mat.SetTexture("_BaseMap",null);
            mat.SetFloat("_Smoothness",1-entry.Value.rough);mat.SetFloat("_Metallic",entry.Value.metal);
            if(entry.Key=="Glass")
            {mat.SetFloat("_WorkflowMode",0);mat.EnableKeyword("_SPECULAR_SETUP");mat.SetColor("_SpecColor",new Color(.012f,.012f,.012f));}
            mat.enableInstancing=true;EditorUtility.SetDirty(mat);materials[name]=mat;
        }
        int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Clean and arrange desktop props");
        foreach(var item in bindings)
        {
            var root=GameObject.Find(item.path).transform;
            var previous=root.Find("Clean Art");if(previous)Undo.DestroyObjectImmediate(previous.gameObject);
            foreach(var r in root.GetComponentsInChildren<Renderer>(true).Where(Art))
            {Undo.RecordObject(r,"Retain prior desk art");r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
            Undo.RecordObject(root,"Place desktop prop");
            root.SetPositionAndRotation(item.position,Quaternion.Euler(0,item.yaw,0));root.localScale=item.scale;
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            string modelPath=$"{Folder}/Models/Clean_{item.model}.fbx";
            var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importNormals=ModelImporterNormals.Import;
            importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
            var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath),root);
            go.name="Clean Art";Undo.RegisterCreatedObjectUndo(go,"Clean desktop art");
            go.transform.SetLocalPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one;
            foreach(var r in go.GetComponentsInChildren<MeshRenderer>())
            {
                string meshName=r.GetComponent<MeshFilter>().sharedMesh.name;
                string name=r.name.Contains("__")?r.name:meshName;
                string key=name.Substring(name.IndexOf("__",StringComparison.Ordinal)+2);
                if(!materials.TryGetValue(key,out var mat))throw new InvalidOperationException("Unmapped desk material: "+key);
                r.sharedMaterials=Enumerable.Repeat(mat,r.GetComponent<MeshFilter>().sharedMesh.subMeshCount).ToArray();
            }
            if(!item.retainAnchor)
            {
                var b=BoundsOf(go.GetComponentsInChildren<Renderer>());
                go.transform.position+=Vector3.up*(item.position.y-b.min.y);
            }
        }
        Undo.CollapseUndoOperations(undo);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Audit();
    }
    [MenuItem("Tools/Office Art/Audit Clean Desktop")]
    public static void Audit()
    {
        var audit=new DeskAudit {note="Bounds include cables. Papers intentionally rest inside the output tray. Camera, room, floor and global lights are not altered by this tool."};
        foreach(var item in Bindings())
        {
            var root=GameObject.Find(item.path).transform;
            var b=BoundsOf(root.GetComponentsInChildren<Renderer>().Where(r=>Art(r)&&r.enabled));
            audit.items.Add(new ItemAudit{path=item.path,model=item.model,position=root.position,size=b.size,min=b.min,max=b.max});
        }
        var crt=GameObject.Find("ImportedOfficeDress/Desk/Retro CRT/Rebuilt CRT");
        var cb=BoundsOf(crt.GetComponentsInChildren<Renderer>());
        audit.items.Add(new ItemAudit{path="ImportedOfficeDress/Desk/Retro CRT",model="CRT",position=crt.transform.position,size=cb.size,min=cb.min,max=cb.max});
        var desk=GameObject.Find("HybridOffice/Booth/Finish_Desk");var db=BoundsOf(desk.GetComponentsInChildren<Renderer>().Where(r=>Art(r)&&r.enabled));
        audit.deskMin=db.min;audit.deskMax=db.max;
        File.WriteAllText(Report+"/placement_audit.json",JsonUtility.ToJson(audit,true));
    }
    [Serializable] public class Validation
    {
        public bool success;public int installedObjects;public List<string> errors=new();
        public List<string> rayHits=new();public string note="Camera-ray hit targets plus existing event checks; no full gameplay-day test.";
    }
    [MenuItem("Tools/Office Art/Validate Clean Desktop")]
    public static void Validate()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Validate in Play mode with the office view active.");
        var result=new Validation();
        var desk=BoundsOf(GameObject.Find("HybridOffice/Booth/Finish_Desk").GetComponentsInChildren<Renderer>().Where(r=>Art(r)&&r.enabled));
        foreach(var item in Bindings())
        {
            var root=GameObject.Find(item.path).transform;var clean=root.Find("Clean Art");
            if(!clean){result.errors.Add(item.model+": missing installed art");continue;}
            result.installedObjects++;
            foreach(var r in root.GetComponentsInChildren<Renderer>().Where(r=>Art(r)&&r.enabled))
            {
                if(!r.transform.IsChildOf(clean))result.errors.Add(item.model+": old renderer still enabled "+r.name);
                if(r.sharedMaterials.Any(m=>!m||m.shader.name=="Hidden/InternalErrorShader"))result.errors.Add(item.model+": invalid material");
            }
            var b=BoundsOf(clean.GetComponentsInChildren<Renderer>());
            if(b.min.x<desk.min.x || b.max.x>desk.max.x || b.min.z<desk.min.z || b.max.z>desk.max.z)
                result.errors.Add(item.model+": extends beyond desktop");
        }
        foreach(string path in new[]{"OfficeRoot/CRTMonitor","OfficeRoot/ReadySign"})
        {
            var target=GameObject.Find(path);var cam=Camera.main;
            var ray=cam.ScreenPointToRay(cam.WorldToScreenPoint(target.transform.position));
            var hits=Physics2D.GetRayIntersectionAll(ray,100);
            var hit=hits.FirstOrDefault(h=>h.collider&&h.collider.GetComponent<Clickable>());
            if(!hit.collider || hit.collider.gameObject!=target)result.errors.Add(path+": camera ray missed expected interaction");
            else result.rayHits.Add(path);
        }
        result.success=result.errors.Count==0;
        File.WriteAllText(Report+"/runtime_validation.json",JsonUtility.ToJson(result,true));
        if(!result.success)Debug.LogError("Desktop validation: "+string.Join("; ",result.errors));
    }
}
