using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Previews and validates the hall's painted crowd groups (the OfficeHallCrowds
/// root and its OfficeHallCrowdPalette). The groups are authored scene content:
/// the Debt Relief pass owns their positions and colours.
/// </summary>
public static class OfficeHallCrowds
{
    const string Root = "OfficeHallCrowds";
    const string ReportFolder = "ArtDeliverables/TimeDesk/HallCrowds";
    const int ExpectedGroups = 19;

    [MenuItem("Tools/Office Art/Hall Crowds/Preview Morning")]
    public static void Morning()=>Preview(OfficeHallCrowdPalette.PreviewMode.Morning);
    [MenuItem("Tools/Office Art/Hall Crowds/Preview Evening")]
    public static void Evening()=>Preview(OfficeHallCrowdPalette.PreviewMode.Evening);
    [MenuItem("Tools/Office Art/Hall Crowds/Follow Shift")]
    public static void Automatic()=>Preview(OfficeHallCrowdPalette.PreviewMode.Automatic);
    static void Preview(OfficeHallCrowdPalette.PreviewMode mode)
    {
        var palette=GameObject.Find(Root).GetComponent<OfficeHallCrowdPalette>();
        if(!EditorApplication.isPlaying)Undo.RecordObject(palette,"Crowd palette preview");
        palette.SetPreview(mode);EditorUtility.SetDirty(palette);SceneView.RepaintAll();
    }
    [Serializable] sealed class Check
    {
        public bool success;public int groups,uniqueCompositions,visibleGroups,colliders,renderers;
        public float floorTop,eveningBlend;public string mode;public List<string> errors=new();
    }
    [MenuItem("Tools/Office Art/Hall Crowds/Validate")]
    public static void Validate()
    {
        var report=new Check();var root=GameObject.Find(Root);
        if(!root)throw new InvalidOperationException("Crowds are not installed.");
        var camera=GameObject.Find("Main Camera").GetComponent<Camera>();
        var planes=GeometryUtility.CalculateFrustumPlanes(camera);
        var cards=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Merged silhouettes").ToArray();
        report.groups=root.transform.childCount;report.renderers=root.GetComponentsInChildren<Renderer>().Length;
        report.uniqueCompositions=cards.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Distinct().Count();
        report.visibleGroups=cards.Count(r=>GeometryUtility.TestPlanesAABB(planes,r.bounds));
        report.colliders=root.GetComponentsInChildren<Collider>(true).Length;
        report.floorTop=GameObject.Find("HybridOffice/Hall/Blender_HallFloor/Hall_Floor__Hall_Floor").GetComponent<Renderer>().bounds.max.y;
        var palette=root.GetComponent<OfficeHallCrowdPalette>();report.eveningBlend=palette.EveningBlend;report.mode=palette.Preview.ToString();
        if(report.groups!=ExpectedGroups || report.uniqueCompositions!=6)report.errors.Add("Expected all placements and six authored compositions.");
        if(report.colliders!=0)report.errors.Add("Background crowds must not intercept gameplay input.");
        foreach(var r in cards)
        {
            if(!r.sharedMaterial || ShaderUtil.ShaderHasError(r.sharedMaterial.shader))report.errors.Add("Invalid silhouette material.");
            if(Mathf.Abs(r.bounds.min.y-report.floorTop)>.015f)report.errors.Add(r.transform.parent.name+" feet miss floor.");
            if(r.shadowCastingMode!=ShadowCastingMode.Off)report.errors.Add("Unexpected 3D crowd shadow.");
            if(r.transform.position.z<7)report.errors.Add("Crowd entered the desk foreground.");
        }
        var so=new SerializedObject(palette);
        if(!so.FindProperty("orchestrator").objectReferenceValue)report.errors.Add("Missing shift binding.");
        var entries=so.FindProperty("groups");
        var block=new MaterialPropertyBlock();
        for(int i=0;i<entries.arraySize;i++)
        {
            var p=entries.GetArrayElementAtIndex(i);
            var r=(Renderer)p.FindPropertyRelative("renderer").objectReferenceValue;
            var morning=(Material)p.FindPropertyRelative("morning").objectReferenceValue;
            var evening=(Material)p.FindPropertyRelative("evening").objectReferenceValue;
            if(!r||!morning||!evening){report.errors.Add("Incomplete palette entry.");continue;}
            r.GetPropertyBlock(block);
            Color expected=Color.Lerp(morning.GetColor("_Tint"),evening.GetColor("_Tint"),palette.EveningBlend);
            if(((Vector4)block.GetColor("_Tint")-(Vector4)expected).sqrMagnitude>.00001f)report.errors.Add("Renderer palette does not match shift.");
        }
        report.success=report.errors.Count==0;Directory.CreateDirectory(ReportFolder);
        File.WriteAllText(ReportFolder+"/validation_"+report.mode.ToLowerInvariant()+".json",JsonUtility.ToJson(report,true));
        if(!report.success)Debug.LogError(string.Join("; ",report.errors));
    }
}
