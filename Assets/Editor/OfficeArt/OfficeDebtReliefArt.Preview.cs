using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class OfficeDebtReliefArt
{
    static readonly List<Canvas> hiddenOverlays=new();
    [MenuItem("Tools/Office Art/Debt Relief/Hide Runtime Overlays For Capture")]
    public static void HideRuntimeOverlays()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Runtime capture only.");
        hiddenOverlays.Clear();
        foreach(var c in Resources.FindObjectsOfTypeAll<Canvas>())
            if(c.gameObject.scene.IsValid() && c.gameObject.scene.isLoaded && c.isActiveAndEnabled && c.renderMode==RenderMode.ScreenSpaceOverlay)
            {hiddenOverlays.Add(c);c.enabled=false;}
        var s=new System.Text.StringBuilder();
        for(int i=0;i<SceneManager.sceneCount;i++)
        foreach(var root in SceneManager.GetSceneAt(i).GetRootGameObjects())
        foreach(var r in root.GetComponentsInChildren<Renderer>(false).Where(IsArt))
            if(!PathOf(r.transform).StartsWith("HybridOffice/")&&!PathOf(r.transform).StartsWith("ImportedOfficeDress/")&&!PathOf(r.transform).StartsWith("OfficeHallCrowds/"))
                s.AppendLine(PathOf(r.transform)+" | "+string.Join(",",r.sharedMaterials.Where(m=>m).Select(m=>m.name+" = "+AssetDatabase.GetAssetPath(m))));
        File.WriteAllText(ReportFolder+"/runtime_extra_renderers.txt",s.ToString());
    }
    [MenuItem("Tools/Office Art/Debt Relief/Restore Runtime Overlays")]
    public static void RestoreRuntimeOverlays()
    {
        foreach(var c in hiddenOverlays)if(c)c.enabled=true;
        hiddenOverlays.Clear();
    }
}
