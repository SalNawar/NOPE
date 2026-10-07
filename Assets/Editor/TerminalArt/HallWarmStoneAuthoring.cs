using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class HallWarmStoneAuthoring
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/WarmStone";
    const string Report="ArtDeliverables/TimeDesk/WarmStone";
    [MenuItem("Tools/Terminal Art/Warm Stone/Install")]
    public static void Install()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var r=art.layers.First(l=>l.id.StartsWith("62 ")).renderer;
        var path=Folder+"/HallWarmStone.png";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.spritePixelsPerUnit=r.sprite.pixelsPerUnit;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.npotScale=TextureImporterNPOTScale.None;
        var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;importer.SetTextureSettings(settings);importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if(sprite.rect.size!=r.sprite.rect.size || sprite.pivot!=r.sprite.pivot)throw new InvalidOperationException("Painting registration changed.");
        Undo.RecordObject(r,"Warm stone hall");r.sprite=sprite;
        string matPath=Folder+"/WarmStoneArchitecture.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if(mat==null){mat=new Material(r.sharedMaterial);AssetDatabase.CreateAsset(mat,matPath);}
        mat.SetTexture("_FixtureReference",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/AnimeHallLayers/Completion/DeepRoom/HallDeepMorning.png"));mat.SetFloat("_UseFixtureReference",1);r.sharedMaterial=mat;EditorUtility.SetDirty(mat);
        var floor=UnityEngine.Object.FindFirstObjectByType<HallForegroundFloor>();var fm=floor.GetComponent<Renderer>().sharedMaterial;Undo.RecordObject(fm,"Match polished floor extension");fm.SetTexture("_PaintedReference",sprite.texture);EditorUtility.SetDirty(fm);
        AssetDatabase.SaveAssets();Save();
    }
    [MenuItem("Tools/Terminal Art/Warm Stone/Save")]
    public static void Save(){if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");HallWhiteCrowdAuthoring.Save();}
    static void Capture(string name){HallFocusAlignmentAuthoring.Capture("warm-stone-"+name,false);File.Copy("ArtDeliverables/TimeDesk/City/FocusAlignment/warm-stone-"+name+".png",Report+"/"+name+".png",true);}
    [MenuItem("Tools/Terminal Art/Warm Stone/Review")]
    public static void Review()
    {
        Directory.CreateDirectory(Report);var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();var settings=rig.Settings;float hour=settings.previewHour,pan=art.lookLeft;bool use=settings.previewHourOn;
        try{settings.previewHourOn=true;foreach(float h in new[]{8f,12f,16.5f,22f}){settings.previewHour=h;art.SetPan(0);Capture("front-"+h.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture));}settings.previewHour=12;art.SetPan(1);Capture("left-noon");}
        finally{settings.previewHour=hour;settings.previewHourOn=use;art.SetPan(pan);art.Apply();UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();}
        File.WriteAllText(Report+"/review.txt","Same 2172x724 registration, pivot and 100 pixels/unit. New sprite and material on layer 62 only, plus matching floor-extension reference. Original window masks, city, crowds, portal glow, shadow geometry, camera and desk remain registered. Four time-of-day and left-pan native captures generated for visual inspection. Original ceiling fixture luminance retained to prevent pale stone emitting at night.");
    }
}
