using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class DeskPropAssemblyAuthoring
{
    public static readonly (OfficeAnchorId id,string prefix,string readout)[] Props={
        (OfficeAnchorId.Till,"Clean_Till__","CreditsNumber"),
        (OfficeAnchorId.Intercom,"Clean_Phone__",null),
        (OfficeAnchorId.Stamp,"Clean_Stamp__",null),
        (OfficeAnchorId.Calculator,"Clean_Calculator__",null),
        (OfficeAnchorId.PenPot,"Clean_PenPot__",null),
        (OfficeAnchorId.Stapler,"Clean_Stapler__",null),
        (OfficeAnchorId.Clock,"Office_Clock__","ShiftClockDisplay"),
        (OfficeAnchorId.Calendar,"Office_Calendar__","DayNumber"),
        (OfficeAnchorId.StabilityMonitor,"Office_Stability__","StabilityPercent")};
    [MenuItem("Tools/Terminal Art/Desk Animation/Restore Prop Assemblies")]
    public static void Restore()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before repairing assemblies.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=AnimeHallHooks.HallPath)throw new InvalidOperationException("Open AnimeHall scene.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var report=new StringBuilder();
        foreach(var spec in Props)
        {
            var meshes=all.Where(t=>t.name.StartsWith(spec.prefix) && t.GetComponent<MeshRenderer>()!=null).ToArray();
            if(meshes.Length==0)throw new InvalidOperationException("Missing "+spec.prefix);
            var group=all.FirstOrDefault(t=>t.name=="Anchor_"+spec.id);
            if(group==null){var go=new GameObject("Anchor_"+spec.id);Undo.RegisterCreatedObjectUndo(go,"Restore whole prop reactions");group=go.transform;group.SetParent(meshes[0].parent,false);group.position=meshes[0].position;}
            var members=meshes.Concat(all.Where(t=>spec.readout!=null && t.name==spec.readout && t.GetComponent<TMPro.TMP_Text>()!=null)).ToArray();
            foreach(var t in members)
            {
                var p=t.position;var r=t.rotation;var s=t.lossyScale;
                Undo.SetTransformParent(t,group,"Restore whole prop reactions");
                if(Vector3.Distance(p,t.position)>0.00001f || Quaternion.Angle(r,t.rotation)>0.001f || Vector3.Distance(s,t.lossyScale)>0.00001f)throw new InvalidOperationException("Changed art pose: "+t.name);
            }
            report.AppendLine($"{spec.id}: {meshes.Length} mesh parts, {members.Length-meshes.Length} readouts; all world poses retained.");
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("ArtDeliverables/TimeDesk/DeskAnimationRepair");File.WriteAllText("ArtDeliverables/TimeDesk/DeskAnimationRepair/assembly-repair.txt",report.ToString());
    }
}
