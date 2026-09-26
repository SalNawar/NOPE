using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// The desk-layout pass: the five approved desk notes and the worn surfaces.
public static partial class OfficeDebtReliefArt
{
    // The layout pass's report folder, and the prop roots it moves. The phone, the
    // calculator and NEXT are scene-contract fallbacks: the gameplay binder follows them at load.
    const string LayoutReport = ReportFolder + "/LayoutWear";
    const string PhoneRoot = "ImportedOfficeDress/Desk/Clerk hotline";
    const string LampRoot = "ImportedOfficeDress/Desk/Banker task lamp";
    const string CalcRoot = "ImportedOfficeDress/Desk/Desk calculator";
    const string NextRoot = "HybridOffice/Booth/Blender_Next";
    const string MouseWire = "HybridOffice/Booth/Finish_Mouse/Clean Art/Clean_Mouse__DeskClean_Rubber";
    /// <summary>True for a path at or under one of the four prop roots the desk notes move.</summary>
    static bool RequestedDeskChange(string path) => new[] {PhoneRoot,LampRoot,CalcRoot,NextRoot}.Any(p=>path==p || path.StartsWith(p+"/",StringComparison.Ordinal));

    /// <summary>Moves a prop root on the desk plane (its height kept) and sets its heading.</summary>
    static void PlaceDeskRoot(string path, float x, float z, float yaw)
    {
        var t=Require(path); Undo.RecordObject(t,"Apply requested desk layout");
        t.SetPositionAndRotation(new Vector3(x,t.position.y,z),Quaternion.Euler(0,yaw,0));
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);EditorUtility.SetDirty(t);
    }
    /// <summary>Imports a generated wear texture (DebtRelief/Textures) with the pass's settings.</summary>
    static Texture2D WearTexture(string name)
    {
        string path=ArtFolder+"/Textures/"+name+".png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(!importer)throw new InvalidOperationException("Missing wear texture: "+path);
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
        importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;
        importer.maxTextureSize=1024;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    /// <summary>
    /// Gives a broad surface planar UVs so a wear texture spans it once: a copy of its
    /// mesh (saved under DebtRelief/Meshes, reused on reruns) gets UVs from the world
    /// position along axes <paramref name="u"/> and <paramref name="v"/> (0 x, 1 y, 2 z)
    /// normalised to the renderer's bounds. The original FBX mesh and its UVs stay.
    /// </summary>
    static void SurfaceUV(Renderer r,int u,int v)
    {
        var mf=r.GetComponent<MeshFilter>();if(!mf || !mf.sharedMesh)throw new InvalidOperationException("No mesh on "+PathOf(r.transform));
        EnsureFolder(ArtFolder+"/Meshes");
        string path=ArtFolder+"/Meshes/"+PathOf(r.transform).Replace('/','_').Replace(' ','_')+"_WearUV.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(!mesh){mesh=UnityEngine.Object.Instantiate(mf.sharedMesh);mesh.name=r.name+"_WearUV";AssetDatabase.CreateAsset(mesh,path);}
        var b=r.bounds;var vertices=mesh.vertices;var uv=new Vector2[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            var p=r.transform.TransformPoint(vertices[i]);
            uv[i]=new Vector2((p[u]-b.min[u])/Mathf.Max(.001f,b.size[u]),(p[v]-b.min[v])/Mathf.Max(.001f,b.size[v]));
        }
        mesh.uv=uv;EditorUtility.SetDirty(mesh);
        Undo.RecordObject(mf,"Map worn surface");mf.sharedMesh=mesh;
        PrefabUtility.RecordPrefabInstancePropertyModifications(mf);EditorUtility.SetDirty(mf);
    }
    /// <summary>
    /// Puts DebtRelief/Materials/Wear_{key}.mat (a copy of the renderer's material, with
    /// the texture; white base colour when <paramref name="replaceColour"/>) in slot 0,
    /// and planar UVs when axes are given. Refuses a protected renderer.
    /// </summary>
    static void ApplyWearMap(Renderer r,Texture2D texture,string key,bool replaceColour,int u=-1,int v=-1)
    {
        if(Excluded(PathOf(r.transform)))throw new InvalidOperationException("Protected target: "+r.name);
        var source=r.sharedMaterial;
        string path=ArtFolder+"/Materials/Wear_"+key+".mat";
        var m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!m){m=new Material(source){name="Wear_"+key};AssetDatabase.CreateAsset(m,path);}
        Undo.RecordObject(m,"Worn office surface");m.SetTexture("_BaseMap",texture);
        m.SetTextureScale("_BaseMap",Vector2.one);m.SetTextureOffset("_BaseMap",Vector2.zero);
        if(replaceColour)m.SetColor("_BaseColor",Color.white);
        EditorUtility.SetDirty(m);Assign(r,0,m);
        if(u>=0)SurfaceUV(r,u,v);
    }
    /// <summary>
    /// The layout pass: phone to the rear right, NEXT beside the mat, the lamp behind
    /// NEXT, the calculator turned to the player, the mouse wire hidden, and the worn
    /// plaster, cork and handled surfaces. Captures its baseline on the first run,
    /// refuses to save when a protected or gameplay hash changed, then validates.
    /// </summary>
    [MenuItem("Tools/Office Art/Debt Relief/Apply Desk Notes And Wear")]
    public static void ApplyDeskNotesAndWear()
    {
        CheckScene();Directory.CreateDirectory(LayoutReport);
        string baseline=LayoutReport+"/before.json";
        if(!File.Exists(baseline))File.WriteAllText(baseline,JsonUtility.ToJson(Capture(),true));
        string protectedBefore=ProtectedHash(),gameplayBefore=GameplayHash();
        int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Requested desk notes and worn surfaces");
        PlaceDeskRoot(PhoneRoot,2.05f,.62f,0);
        PlaceDeskRoot(NextRoot,.13f,.16f,0);
        PlaceDeskRoot(LampRoot,.46f,.62f,-8);
        float calcYaw=Mathf.Atan2(1.08f-Camera.main.transform.position.x,.13f-Camera.main.transform.position.z)*Mathf.Rad2Deg;
        PlaceDeskRoot(CalcRoot,1.08f,.13f,calcYaw);
        var wire=Require(MouseWire).GetComponent<Renderer>();
        Undo.RecordObject(wire,"Remove mouse wire");wire.enabled=false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(wire);EditorUtility.SetDirty(wire);
        var plaster=WearTexture("WornPlaster");var cork=WearTexture("WornCork");var handled=WearTexture("HandledSurface");
        foreach(var r in Transforms().SelectMany(t=>t.GetComponents<Renderer>()).Where(IsArt).ToArray())
        {
            string p=PathOf(r.transform);if(Excluded(p)||r is SpriteRenderer)continue;
            if(p.Contains("Partition/") && r.name=="Office_Partition__Office_Teal")
                ApplyWearMap(r,plaster,"PartitionPlaster",true,2,1);
            else if(p=="HybridOffice/Hall/Blender_HallStructure/Hall_Structure__Hall_Plaster")
                ApplyWearMap(r,plaster,"HallPlaster",true);
            else if((p.Contains("/Finish_Board/")||p.Contains("/Finish_RightBoard/"))&&r.name.EndsWith("__Finish_Enamel"))
                ApplyWearMap(r,cork,"NoticeboardCork",true,0,1);
            else if(p.Contains("/Finish_Mat/Clean Art/") && r.name.EndsWith("__DeskClean_Pad"))
                ApplyWearMap(r,handled,"BlotterSurface",false,0,2);
            else if((p.StartsWith(PhoneRoot+"/")&&r.name.EndsWith("__DeskClean_PhoneBody")) ||
                    (p.StartsWith(LampRoot+"/")&&(r.name.EndsWith("__DeskClean_Green")||r.name.EndsWith("__DeskClean_GreenDark"))) ||
                    (p.Contains("/Finish_Till/Clean Art/")&&(r.name.EndsWith("__DeskClean_Green")||r.name.EndsWith("__DeskClean_GreenDark"))))
                ApplyWearMap(r,handled,r.sharedMaterial.name.Replace("Wear_",""),false);
        }
        AssetDatabase.SaveAssets();
        if(ProtectedHash()!=protectedBefore||GameplayHash()!=gameplayBefore)throw new InvalidOperationException("Protected content changed; do not save.");
        Undo.CollapseUndoOperations(undo);EditorSceneManager.MarkSceneDirty(Scene);EditorSceneManager.SaveScene(Scene);
        ValidateDeskNotesAndWear();
    }
    /// <summary>The layout pass's check (LayoutWear/validation.json).</summary>
    [Serializable] public class LayoutWearValidation
    {
        /// <summary>True when there are no errors.</summary>
        public bool success;

        /// <summary>The protected hash matches the layout baseline.</summary>
        public bool pcAndFloorUnchanged;

        /// <summary>The gameplay hash matches the layout baseline.</summary>
        public bool gameplayUnchanged;

        /// <summary>The phone sits on the right (its bounds start right of x = 1.5).</summary>
        public bool phoneOnRight;

        /// <summary>NEXT is behind the mat's back edge by less than 12 cm.</summary>
        public bool nextNearMat;

        /// <summary>The lamp is behind NEXT and within 60 cm of it sideways.</summary>
        public bool lampBehindNext;

        /// <summary>The calculator faces the camera within 2 degrees.</summary>
        public bool calculatorFacingPlayer;

        /// <summary>The mouse wire renderer is off.</summary>
        public bool mouseWireHidden;

        /// <summary>The gap between the mat's back edge and NEXT, metres.</summary>
        public float nextMatGap;

        /// <summary>The angle between the calculator's front and the camera, degrees.</summary>
        public float calculatorFacingAngle;

        /// <summary>Renderers using a Wear_ material (at least 9 expected).</summary>
        public int texturedRenderers;

        /// <summary>Crowd silhouettes that overlap the portal in the office camera (0 expected).</summary>
        public int portalOverlaps;

        /// <summary>Crowd silhouettes found (19 expected).</summary>
        public int crowdGroups;

        /// <summary>Desk props whose bounds overlap (none expected).</summary>
        public List<string> intersections=new();

        /// <summary>What failed.</summary>
        public List<string> errors=new();
    }

    /// <summary>Read-only: checks the five notes, the wear and the crowd clearance against the layout baseline, writes after.json and validation.json, throws on failure.</summary>
    [MenuItem("Tools/Office Art/Debt Relief/Validate Desk Notes And Wear")]
    public static void ValidateDeskNotesAndWear()
    {
        CheckScene();var before=JsonUtility.FromJson<AuditReport>(File.ReadAllText(LayoutReport+"/before.json"));
        var after=Capture();var v=new LayoutWearValidation();
        v.pcAndFloorUnchanged=before.protectedHash==after.protectedHash;v.gameplayUnchanged=before.gameplayHash==after.gameplayHash;
        var phone=ArtBounds(Require(PhoneRoot));var next=ArtBounds(Require(NextRoot));var lamp=ArtBounds(Require(LampRoot));
        var mat=ArtBounds(Require("HybridOffice/Booth/Finish_Mat"));var calc=Require(CalcRoot);
        v.phoneOnRight=phone.min.x>1.5f;
        v.nextMatGap=next.min.z-mat.max.z;v.nextNearMat=v.nextMatGap>=0&&v.nextMatGap<.12f;
        v.lampBehindNext=lamp.min.z>next.max.z && Mathf.Abs(lamp.center.x-next.center.x)<.6f;
        Vector3 toward=Camera.main.transform.position-calc.position;toward.y=0;
        v.calculatorFacingAngle=Vector3.Angle(-calc.forward,toward);v.calculatorFacingPlayer=v.calculatorFacingAngle<2;
        v.mouseWireHidden=!Require(MouseWire).GetComponent<Renderer>().enabled;
        v.texturedRenderers=after.renderers.Count(r=>r.materials.Any(m=>m.path.StartsWith(ArtFolder+"/Materials/Wear_")));
        foreach(var other in new[]{"HybridOffice/Booth/Finish_FormSorter","ImportedOfficeDress/Desk/Spare forms","ImportedOfficeDress/Desk/Pen pot"})
        {if(phone.Intersects(ArtBounds(Require(other))))v.intersections.Add("Phone / "+other);}
        if(ArtBounds(calc).Intersects(ArtBounds(Require("ImportedOfficeDress/Desk/Pen pot"))))v.intersections.Add("Calculator / pen cup");
        var portal=PortalRect(Camera.main);
        foreach(var r in Require("OfficeHallCrowds").GetComponentsInChildren<Renderer>().Where(r=>r.name=="Merged silhouettes"))
        {v.crowdGroups++;if(Project(Camera.main,r.bounds).Overlaps(portal))v.portalOverlaps++;}
        if(!v.pcAndFloorUnchanged)v.errors.Add("PC or floor changed.");if(!v.gameplayUnchanged)v.errors.Add("Gameplay data changed.");
        if(!v.phoneOnRight||!v.nextNearMat||!v.lampBehindNext||!v.calculatorFacingPlayer||!v.mouseWireHidden)v.errors.Add("One or more desk notes are not applied.");
        if(v.texturedRenderers<9)v.errors.Add("Worn textures are missing.");
        if(v.portalOverlaps>0||v.crowdGroups!=19)v.errors.Add("Crowd clearance changed.");
        if(v.intersections.Count>0)v.errors.Add("Desk items overlap.");
        v.success=v.errors.Count==0;
        File.WriteAllText(LayoutReport+"/after.json",JsonUtility.ToJson(after,true));
        File.WriteAllText(LayoutReport+"/validation.json",JsonUtility.ToJson(v,true));
        if(!v.success)throw new InvalidOperationException(string.Join("; ",v.errors));
        Debug.Log("All five desk notes and worn textures verified; protected PC/floor unchanged.");
    }
}
