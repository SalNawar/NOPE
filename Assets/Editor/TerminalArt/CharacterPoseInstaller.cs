using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CharacterPoseInstaller
{
    [Serializable] public sealed class Manifest {public Item[] items;}
    [Serializable] public sealed class Item {public string id;public string[] keys;public string neutral;public string explaining;public string guarded;}
    const string Folder="ArtDeliverables/TimeDesk/Characters/Poses";
    [MenuItem("Tools/Terminal Art/Review/Install Complete Character Poses")]
    public static void Install()
    {
        // Only persistent art assets change; safe while previewing a shift.
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Folder+"/pose-manifest.json"));
        var library=AssetDatabase.LoadAssetAtPath<CharacterPoseLibrary>("Assets/Resources/CharacterPoseLibrary.asset");
        if(library==null) {library=ScriptableObject.CreateInstance<CharacterPoseLibrary>();AssetDatabase.CreateAsset(library,"Assets/Resources/CharacterPoseLibrary.asset");}
        string report="Complete character pose registration\n";
        foreach(var item in manifest.items)
        {
            string signature=string.Join("|",item.keys.OrderBy(k=>k,StringComparer.Ordinal));
            var neutral=new Texture2D(2,2,TextureFormat.RGBA32,false);neutral.LoadImage(File.ReadAllBytes(item.neutral));
            var baseline=Landmarks(neutral);
            CharacterPoseLibrary.Pose Pose(string path)
            {
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(sprite==null) throw new InvalidOperationException("Missing pose sprite: "+path);
                var landmarks=Landmarks(sprite.texture);
                float scale=(baseline.feet-baseline.top)/(float)(landmarks.feet-landmarks.top);
                float offset=((1490-baseline.feet)-(1490-landmarks.feet)*scale)/1536f;
                report+=$"{item.id} {Path.GetFileName(path)}: head {landmarks.top}, feet {landmarks.feet}, scale {scale:F5}, foot offset {offset:F5}\n";
                return new CharacterPoseLibrary.Pose{sprite=sprite,scale=scale,footOffset=offset};
            }
            var entry=new CharacterPoseLibrary.Entry{signature=signature,explaining=Pose(item.explaining),guarded=Pose(item.guarded)};
            library.entries=library.entries.Where(e=>e.signature!=signature).Concat(new[]{entry}).ToArray();
            UnityEngine.Object.DestroyImmediate(neutral);
        }
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();File.WriteAllText(Folder+"/registration.txt",report);Debug.Log(report);
    }
    [MenuItem("Tools/Terminal Art/Review/Refresh Current Complete Pose")]
    public static void RefreshCurrent()
    {
        var field=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var view=UnityEngine.Object.FindFirstObjectByType<TravellerView>();
        var stack=(LookSpriteStack)typeof(TravellerView).GetField("figure",field).GetValue(view);
        var look=(TravellerLook)typeof(LookSpriteStack).GetField("_look",field).GetValue(stack);
        var art=(CharacterArt)typeof(LookSpriteStack).GetField("_art",field).GetValue(stack);
        stack.Show(look,art);
        Debug.Log("Complete pose refreshed for exact current character: "+look.Describe());
    }
    [MenuItem("Tools/Terminal Art/Review/Capture Live Complete Poses")]
    public static void CaptureLive()
    {
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Run a shift first.");
        var view=UnityEngine.Object.FindFirstObjectByType<TravellerView>();
        string folder=Folder+"/Live";Directory.CreateDirectory(folder);
        int step=0;double next=0;bool prepared=false;
        void Tick()
        {
            if(!EditorApplication.isPlaying) {EditorApplication.update-=Tick;return;}
            if(EditorApplication.timeSinceStartup<next) return;
            if(!prepared) {view.SetExpression(step==0?"neutral":"worried");prepared=true;next=EditorApplication.timeSinceStartup+.2;}
            else
            {
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(folder+"/current-"+(step==0?"explaining":"guarded")+".png"));
                step++;prepared=false;next=EditorApplication.timeSinceStartup+.6;
                if(step==2) EditorApplication.update-=Tick;
            }
        }
        EditorApplication.update+=Tick;
    }
    static (int top,int feet) Landmarks(Texture2D texture)
    {
        var pixels=texture.GetPixels32();int w=texture.width,h=texture.height;int top=h,feet=0;
        for(int y=0;y<h;y++) for(int x=w/3;x<w*2/3;x++)
        {
            if(pixels[y*w+x].a<240) continue;
            int row=h-1-y;
            if(row<h/3) top=Mathf.Min(top,row);
            if(row>h*2/3) feet=Mathf.Max(feet,row);
        }
        if(top==h || feet==0) throw new InvalidOperationException("Pose has no registered head or feet.");
        return (top,feet);
    }
}
