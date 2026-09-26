using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Art-side editor tool (Tools > Office Art > Hall Crowds): previews and validates
/// the hall's painted crowd groups (the OfficeHallCrowds root and its
/// OfficeHallCrowdPalette). The groups are authored scene content: the Debt Relief
/// pass owns their positions and colours. Reports go to ArtDeliverables/TimeDesk/HallCrowds.
/// </summary>
public static class OfficeHallCrowds
{
    const string Root = "OfficeHallCrowds";
    const string ReportFolder = "ArtDeliverables/TimeDesk/HallCrowds";
    const int ExpectedGroups = 19;
    const string FloorPath = "HybridOffice/Hall/Blender_HallFloor/Hall_Floor__Hall_Floor";

    /// <summary>Shows the morning palette (in edit mode an undoable scene change: set Follow Shift again before saving).</summary>
    [MenuItem("Tools/Office Art/Hall Crowds/Preview Morning")]
    public static void Morning()=>Preview(OfficeHallCrowdPalette.PreviewMode.Morning);
    /// <summary>Shows the evening palette.</summary>
    [MenuItem("Tools/Office Art/Hall Crowds/Preview Evening")]
    public static void Evening()=>Preview(OfficeHallCrowdPalette.PreviewMode.Evening);
    /// <summary>Follows the gameplay shift clock: the mode to leave selected for play.</summary>
    [MenuItem("Tools/Office Art/Hall Crowds/Follow Shift")]
    public static void Automatic()=>Preview(OfficeHallCrowdPalette.PreviewMode.Automatic);
    /// <summary>Sets the palette's mode; refuses when the open scene has no crowds.</summary>
    static void Preview(OfficeHallCrowdPalette.PreviewMode mode)
    {
        var root=GameObject.Find(Root);
        var palette=root?root.GetComponent<OfficeHallCrowdPalette>():null;
        if(!palette)throw new InvalidOperationException("No "+Root+" with an OfficeHallCrowdPalette in the open scene: open OfficeScene.");
        if(!EditorApplication.isPlaying)Undo.RecordObject(palette,"Crowd palette preview");
        palette.SetPreview(mode);EditorUtility.SetDirty(palette);SceneView.RepaintAll();
    }
    /// <summary>The report (validation_{mode}.json).</summary>
    [Serializable] sealed class Check
    {
        /// <summary>True when there are no errors.</summary>
        public bool success;

        /// <summary>Groups under the root (19 expected).</summary>
        public int groups;

        /// <summary>Distinct silhouette meshes (the six authored compositions).</summary>
        public int uniqueCompositions;

        /// <summary>Silhouettes inside the office camera's view.</summary>
        public int visibleGroups;

        /// <summary>Colliders under the root (0 expected: the crowds never take clicks).</summary>
        public int colliders;

        /// <summary>Renderers under the root (silhouettes and contacts).</summary>
        public int renderers;

        /// <summary>The hall floor's top; every silhouette's feet must be within 1.5 cm of it.</summary>
        public float floorTop;

        /// <summary>The palette's current blend (0 morning, 1 evening).</summary>
        public float eveningBlend;

        /// <summary>The palette's mode (Automatic, Morning or Evening).</summary>
        public string mode;

        /// <summary>True when the gameplay layer's shift clock is published (play mode with OfficeGameplay loaded).</summary>
        public bool shiftClockLive;

        /// <summary>What failed.</summary>
        public List<string> errors=new();
    }

    /// <summary>
    /// Read-only check of the crowds: count, compositions, no colliders or shadows, feet on
    /// the floor, behind the desk, every renderer tinted as the current blend says, and in
    /// play mode a live shift clock for Follow Shift. Writes validation_{mode}.json and logs
    /// an error on failure.
    /// </summary>
    [MenuItem("Tools/Office Art/Hall Crowds/Validate")]
    public static void Validate()
    {
        var report=new Check();var root=GameObject.Find(Root);
        if(!root)throw new InvalidOperationException("Crowds are not installed.");
        // "Main Camera" is the scene contract's OfficeCamera: the view the gameplay layer plays through.
        var cameraObject=GameObject.Find("Main Camera");
        var camera=cameraObject?cameraObject.GetComponent<Camera>():null;
        if(!camera)throw new InvalidOperationException("No Main Camera (the scene contract's OfficeCamera) in the open scene.");
        var floor=GameObject.Find(FloorPath);
        var floorRenderer=floor?floor.GetComponent<Renderer>():null;
        if(!floorRenderer)throw new InvalidOperationException("Missing the hall floor "+FloorPath+".");
        var palette=root.GetComponent<OfficeHallCrowdPalette>();
        if(!palette)throw new InvalidOperationException(Root+" has no OfficeHallCrowdPalette.");
        var planes=GeometryUtility.CalculateFrustumPlanes(camera);
        var cards=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name=="Merged silhouettes").ToArray();
        report.groups=root.transform.childCount;report.renderers=root.GetComponentsInChildren<Renderer>().Length;
        report.uniqueCompositions=cards.Select(r=>r.GetComponent<MeshFilter>().sharedMesh).Distinct().Count();
        report.visibleGroups=cards.Count(r=>GeometryUtility.TestPlanesAABB(planes,r.bounds));
        report.colliders=root.GetComponentsInChildren<Collider>(true).Length;
        report.floorTop=floorRenderer.bounds.max.y;
        report.eveningBlend=palette.EveningBlend;report.mode=palette.Preview.ToString();
        // Follow Shift reads the gameplay layer's shift hook; in play mode it must be there, or the crowds stay in the morning all day.
        report.shiftClockLive=ShiftClockDriver.Live!=null;
        if(EditorApplication.isPlaying && palette.Preview==OfficeHallCrowdPalette.PreviewMode.Automatic && !report.shiftClockLive)
            report.errors.Add("No gameplay shift clock (ShiftClockDriver.Live): Follow Shift stays in the morning palette.");
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
