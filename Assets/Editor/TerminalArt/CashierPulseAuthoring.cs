using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CashierPulseAuthoring
{
    const string Folder="ArtDeliverables/TimeDesk/CashierPulse";
    const string Config="Assets/Data/Config/DeskReactions/Reaction_Till.asset";
    static Transform[] Parts()=>SceneManager.GetSceneByPath(AnimeHallHooks.HallPath).GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.StartsWith("Clean_Till__") && t.GetComponent<MeshRenderer>()!=null).ToArray();
    static Bounds BoundsOf(Transform[] parts){var b=parts[0].GetComponent<Renderer>().bounds;foreach(var t in parts.Skip(1))b.Encapsulate(t.GetComponent<Renderer>().bounds);return b;}
    [MenuItem("Tools/Terminal Art/Cashier Pulse/Apply")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var scene=SceneManager.GetActiveScene();if(scene.path!=AnimeHallHooks.HallPath)throw new InvalidOperationException("Open AnimeHall.");
        var parts=Parts();if(parts.Length!=7)throw new InvalidOperationException("Expected seven cashier mesh pieces.");
        var all=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
        var text=all.First(t=>t.name=="CreditsNumber" && t.GetComponent<TMPro.TMP_Text>()!=null);
        var group=all.FirstOrDefault(t=>t.name=="Anchor_Till");
        if(group==null){var go=new GameObject("Anchor_Till");Undo.RegisterCreatedObjectUndo(go,"Cashier pulse");group=go.transform;group.SetParent(parts[0].parent,false);var b=BoundsOf(parts);group.position=new Vector3(b.center.x,b.min.y,b.center.z);}
        foreach(var t in parts.Append(text))Undo.SetTransformParent(t,group,"Cashier pulse");
        var config=AssetDatabase.LoadAssetAtPath<DeskReactionSO>(Config);Undo.RecordObject(config,"Cashier pulse");config.kind=ReactionKind.Pulse;config.amplitude=.08f;config.seconds=.35f;EditorUtility.SetDirty(config);AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
    static DeskReaction reaction;static Transform target;static Transform[] meshes;static Vector3 restScale,restPosition;static Bounds restBounds;static float start,oldSpeed,maxRatio,minBottom;static bool captured;static int cycle;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    [MenuItem("Tools/Terminal Art/Cashier Pulse/Verify")]
    public static void Verify()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
        reaction=UnityEngine.Object.FindObjectsByType<DeskReaction>(FindObjectsSortMode.None).First(r=>r.name=="Till");
        target=(Transform)typeof(DeskReaction).GetField("_target",Private).GetValue(reaction);meshes=Parts();
        if(target==null || meshes.Any(t=>!t.IsChildOf(target)))throw new InvalidOperationException("Cashier mesh binding incomplete.");
        restScale=target.localScale;restPosition=target.position;restBounds=BoundsOf(meshes);maxRatio=1;minBottom=restBounds.min.y;captured=false;cycle=0;
        oldSpeed=Time.timeScale;Time.timeScale=.2f;Directory.CreateDirectory(Folder);Capture("rest");
        reaction.GetComponent<Clickable>().onClick.Invoke();start=Time.time;EditorApplication.update+=Tick;EditorApplication.playModeStateChanged+=OnPlayState;
    }
    static void Capture(string name){HallFocusAlignmentAuthoring.Capture("cashier-pulse-"+name,false);File.Copy("ArtDeliverables/TimeDesk/City/FocusAlignment/cashier-pulse-"+name+".png",Folder+"/"+name+".png",true);}
    static void Tick()
    {
        try{
            if(target==null){Finish();return;}
            float elapsed=Time.time-start;var b=BoundsOf(meshes);float ratio=b.size.y/restBounds.size.y;maxRatio=Mathf.Max(maxRatio,ratio);minBottom=Mathf.Min(minBottom,b.min.y);
            if(Vector3.Distance(target.position,restPosition)>.00001f)throw new InvalidOperationException("Cashier translated instead of scaling.");
            if(Mathf.Abs(b.size.x/restBounds.size.x-ratio)>.0001f || Mathf.Abs(b.size.z/restBounds.size.z-ratio)>.0001f)throw new InvalidOperationException("Nonuniform pulse.");
            if(!captured && elapsed>=.16f){Capture("peak");captured=true;}
            if(elapsed<.48f)return;
            if(Vector3.Distance(target.localScale,restScale)>.00001f || Vector3.Distance(b.size,restBounds.size)>.00001f)throw new InvalidOperationException("Cashier did not return to rest.");
            if(cycle==0){cycle++;reaction.GetComponent<Clickable>().onClick.Invoke();start=Time.time;return;}
            bool passed=maxRatio>1.075f && minBottom>=restBounds.min.y-.00001f;
            Capture("returned");File.WriteAllText(Folder+"/verification.txt",$"PASS={passed}\nTwo click cycles. Peak uniform scale ratio={maxRatio:F6}\nLowest base displacement={minBottom-restBounds.min.y:F8}m\nRoot translation=0\nExact rest scale and bounds restored.\nSeven meshes and CreditsNumber share the animated root.\n");
            if(!passed)throw new InvalidOperationException("Pulse shape check failed.");Finish();
        }catch(Exception e){File.WriteAllText(Folder+"/error.txt",e.ToString());Finish();Debug.LogException(e);}
    }
    static void OnPlayState(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingPlayMode)Finish();}
    static void Finish(){Time.timeScale=oldSpeed;EditorApplication.update-=Tick;EditorApplication.playModeStateChanged-=OnPlayState;reaction=null;}
}
