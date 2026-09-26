using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Play-mode capture helpers for the Game-view evidence (runtime.png).
public static partial class OfficeDebtReliefArt
{
    /// <summary>The overlay canvases HideRuntimeOverlays switched off, for RestoreRuntimeOverlays.</summary>
    static readonly List<Canvas> hiddenOverlays=new();
    /// <summary>
    /// Play mode only: switches off every active screen-space overlay canvas (the
    /// gameplay HUD included) so the Game view shows the art alone, and lists the
    /// enabled renderers outside the art roots in runtime_extra_renderers.txt (what the
    /// gameplay layer adds). Undo it with Restore Runtime Overlays before leaving play mode.
    /// </summary>
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
    /// <summary>Switches the canvases HideRuntimeOverlays hid back on.</summary>
    [MenuItem("Tools/Office Art/Debt Relief/Restore Runtime Overlays")]
    public static void RestoreRuntimeOverlays()
    {
        foreach(var c in hiddenOverlays)if(c)c.enabled=true;
        hiddenOverlays.Clear();
    }
}
