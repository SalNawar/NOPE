using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class DeskAnimationReview
{
    const string Folder="ArtDeliverables/TimeDesk/DeskAnimationRepair";
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static string Path(Transform t)=>t==null?"NULL":t.parent==null?t.name:Path(t.parent)+"/"+t.name;
    [MenuItem("Tools/Terminal Art/Desk Animation/Inspect")]
    public static void Inspect()
    {
        Directory.CreateDirectory(Folder); var b=new StringBuilder();
        b.AppendLine("Playing: "+Application.isPlaying);
        foreach(var r in UnityEngine.Object.FindObjectsByType<DeskReaction>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            var t=(Transform)typeof(DeskReaction).GetField("_target",Private).GetValue(r);
            b.AppendLine($"REACTION {Path(r.transform)} active={r.isActiveAndEnabled} TARGET {Path(t)}");
            foreach(var mesh in r.GetComponent<Clickable>().Outline)
                if(mesh!=null)b.AppendLine($"  MESH {Path(mesh.transform)} static={mesh.gameObject.isStatic} flags={GameObjectUtility.GetStaticEditorFlags(mesh.gameObject)} batch={mesh.isPartOfStaticBatch} childOfTarget={(t!=null && mesh.transform.IsChildOf(t))} pos={mesh.transform.position} matrixPos={mesh.localToWorldMatrix.GetColumn(3)}");
        }
        foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name.Contains("Till")))
            b.AppendLine($"TILL {Path(t)} active={t.gameObject.activeInHierarchy} static={GameObjectUtility.GetStaticEditorFlags(t.gameObject)} components={string.Join(",",t.GetComponents<Component>().Select(c=>c.GetType().Name))}");
        File.WriteAllText(Folder+"/inspection.txt",b.ToString());
    }

    static DeskReaction[] reactions;
    static Transform[] members;
    static Vector3[] positions,scales;
    static Quaternion[] rotations;
    static float[] maxMove;
    static float start,duration,oldTimeScale;
    static int index;
    static bool peak;
    static string phase;
    static StringBuilder results;
    [MenuItem("Tools/Terminal Art/Desk Animation/Test Before")]
    public static void TestBefore()=>Begin("before");
    [MenuItem("Tools/Terminal Art/Desk Animation/Test After")]
    public static void TestAfter()=>Begin("after");
    static void Begin(string label)
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play the scene first.");
        if(reactions!=null)throw new InvalidOperationException("Review already running.");
        phase=label;results=new StringBuilder();oldTimeScale=Time.timeScale;Time.timeScale=.15f;
        var all=UnityEngine.Object.FindObjectsByType<DeskReaction>(FindObjectsSortMode.None);
        reactions=DeskPropAssemblyAuthoring.Props.Select(p=>all.First(r=>r.name==p.id.ToString())).ToArray();
        index=0;Next();EditorApplication.update+=Tick;
    }
    static void Next()
    {
        var spec=DeskPropAssemblyAuthoring.Props[index];var r=reactions[index];
        members=SceneManager.GetSceneByPath(AnimeHallHooks.HallPath).GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>(t.name.StartsWith(spec.prefix) && t.GetComponent<MeshRenderer>()!=null)||(spec.readout!=null && t.name==spec.readout && t.GetComponent<TMPro.TMP_Text>()!=null)).ToArray();
        positions=members.Select(t=>t.position).ToArray();rotations=members.Select(t=>t.rotation).ToArray();scales=members.Select(t=>t.lossyScale).ToArray();maxMove=new float[members.Length];
        duration=((DeskReactionSO)typeof(DeskReaction).GetField("reaction",Private).GetValue(r)).seconds;peak=false;
        if(index==0)HallFocusAlignmentAuthoring.Capture(phase+"-till-rest",false);
        r.GetComponent<Clickable>().onClick.Invoke();start=Time.time;
    }
    static void Tick()
    {
        try
        {
            float elapsed=Time.time-start;
            for(int i=0;i<members.Length;i++)maxMove[i]=Mathf.Max(maxMove[i],Vector3.Distance(members[i].position,positions[i])+Quaternion.Angle(members[i].rotation,rotations[i])*.01f+Vector3.Distance(members[i].lossyScale,scales[i]));
            if(index==0 && !peak && elapsed>=duration*.3f){HallFocusAlignmentAuthoring.Capture(phase+"-till-motion",false);peak=true;}
            if(elapsed<duration+.1f)return;
            bool expectsMotion=((DeskReactionSO)typeof(DeskReaction).GetField("reaction",Private).GetValue(reactions[index])).kind!=ReactionKind.None;
            bool moved=expectsMotion?maxMove.All(d=>d>.0001f):maxMove.All(d=>d<.0001f),rest=true;
            for(int i=0;i<members.Length;i++)rest &= Vector3.Distance(members[i].position,positions[i])<.00001f && Quaternion.Angle(members[i].rotation,rotations[i])<.01f && Vector3.Distance(members[i].lossyScale,scales[i])<.00001f;
            results.AppendLine($"{reactions[index].name}: moved {maxMove.Count(d=>d>.0001f)}/{members.Length}, expectsMotion={expectsMotion}, restored={rest}, PASS={moved&&rest}");
            for(int i=0;i<members.Length;i++)results.AppendLine($"  {members[i].name}: max pose delta={maxMove[i]:F6}");
            if(index==0)HallFocusAlignmentAuthoring.Capture(phase+"-till-restored",false);
            index++;if(index<reactions.Length){Next();return;}
            File.WriteAllText(Folder+"/"+phase+"-motion-test.txt",results.ToString());Finish();
        }
        catch(Exception e){File.WriteAllText(Folder+"/"+phase+"-test-error.txt",e.ToString());Finish();Debug.LogException(e);}
    }
    static void Finish(){Time.timeScale=oldTimeScale;EditorApplication.update-=Tick;reactions=null;}
}
