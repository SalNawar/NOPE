using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CharacterPoseSources
{
    [MenuItem("Tools/Terminal Art/Review/Capture Current Figure Source")]
    public static void Capture()
    {
        if(!EditorApplication.isPlaying) throw new InvalidOperationException("Present a traveller first.");
        var view=UnityEngine.Object.FindFirstObjectByType<TravellerView>();
        var field=BindingFlags.Instance|BindingFlags.NonPublic;
        var original=(LookSpriteStack)typeof(TravellerView).GetField("figure",field).GetValue(view);
        var look=(TravellerLook)typeof(LookSpriteStack).GetField("_look",field).GetValue(original);
        if(look==null) throw new InvalidOperationException("No traveller at counter.");
        var actor=new GameObject("Pose source capture"){layer=31,hideFlags=HideFlags.DontSave};
        actor.AddComponent<SortingGroup>();
        foreach(var source in (SpriteRenderer[])typeof(LookSpriteStack).GetField("layers",field).GetValue(original))
        {
            if(source==null||source.sprite==null) continue;
            var child=new GameObject(source.name){layer=31};child.transform.SetParent(actor.transform,false);
            var sprite=child.AddComponent<SpriteRenderer>();sprite.sprite=source.sprite;sprite.sharedMaterial=source.sharedMaterial;sprite.sortingOrder=source.sortingOrder;sprite.color=Color.white;
        }
        var cameraObject=new GameObject("Source camera"){hideFlags=HideFlags.DontSave};
        var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=.5f;
        camera.transform.position=new Vector3(0,.47f,-3);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.aspect=2f/3;
        var sourceCamera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Anime hall player preview");
        int index=(int)typeof(UniversalAdditionalCameraData).GetField("m_RendererIndex",field).GetValue(sourceCamera.GetUniversalAdditionalCameraData());camera.GetUniversalAdditionalCameraData().SetRenderer(index);
        var target=new RenderTexture(1024,1536,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var pixels=new Texture2D(1024,1536,TextureFormat.RGBA32,false);var old=RenderTexture.active;
        try
        {
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1024,1536),0,0);pixels.Apply();
            string folder="ArtDeliverables/TimeDesk/Characters/Poses/Sources";Directory.CreateDirectory(folder);
            string id=Hash128.Compute(CharacterPoseLibrary.Signature(look)).ToString();
            var png=pixels.EncodeToPNG();
            File.WriteAllBytes(folder+"/current.png",png);
            File.WriteAllBytes(folder+"/"+id+".png",png);
            File.WriteAllText(folder+"/"+id+".keys.txt",look.Describe()+"\n"+string.Join("\n",look.Keys.Select(k=>k.Name)));
            File.WriteAllText(folder+"/current-look.txt",look.Describe()+"\n"+string.Join("\n",look.Keys.Select(k=>k.Name)));
        }
        finally
        {
            RenderTexture.active=old;camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
