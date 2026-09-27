using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

// Narrow, local authoring trigger so art revisions can be reviewed in the open editor.
[InitializeOnLoad]
public static class TerminalArtPreviewBridge
{
    const string Request="Temp/TerminalArtPreview.request";
    static TerminalArtPreviewBridge(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Request))return;
        string action=File.ReadAllText(Request).Trim();File.Delete(Request);
        if(action!="rebuild")return;
        try{
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            bool ownScratch=scene.path=="" && Array.Exists(scene.GetRootGameObjects(),g=>g.name=="Dynamically lit grand terminal");
            if(scene.path!="Assets/Art/Office/TerminalRelit/DynamicTerminal.unity" && !ownScratch)throw new InvalidOperationException("Art preview must be the active scene.");
            if(scene.isDirty)EditorSceneManager.SaveScene(scene,"Temp/TerminalArtBeforeRevision_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity",true);
            TerminalRelitProduction.Build();
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            File.WriteAllText("Temp/TerminalArtPreview.result","Built at "+DateTime.Now);
        }catch(Exception e){UnityEngine.Debug.LogException(e);File.WriteAllText("Temp/TerminalArtPreview.result",e.ToString());}
    }
}
