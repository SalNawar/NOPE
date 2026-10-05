using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HallPaletteAuthoring
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/Palette";
    [MenuItem("Tools/Terminal Art/Palette/Install Petrol Graphite")]
    public static void Install()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before installing a palette.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        if(art==null || art.gameObject.scene.path!="Assets/Art/Office/AnimeHallLayers/AnimeHall.unity")
            throw new InvalidOperationException("Open AnimeHall first.");
        string path=Folder+"/PetrolGraphiteGuide.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
        importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
        importer.SaveAndReimport();
        var guide=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var original=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/AnimeHallLayers/Completion/PaintedReference.png");
        if(guide.width!=2172 || guide.height!=724) throw new InvalidOperationException("Palette registration mismatch.");
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Office/AnimeHallLayers/Completion/FourState/HallFourState.mat");
        material.SetTexture("_PaletteGuide",guide);material.SetTexture("_PaletteSource",original);
        EditorUtility.SetDirty(material);
        var floor=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Office/AnimeHallLayers/Completion/ForegroundFloor.mat");
        floor.SetTexture("_PaintedReference",guide);EditorUtility.SetDirty(floor);
        UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(art.gameObject.scene);
        EditorSceneManager.SaveScene(art.gameObject.scene);
        Debug.Log("Installed selected petrol walls / graphite floor palette on independent registered layers.");
    }
    [MenuItem("Tools/Terminal Art/Palette/Capture Selected Palette")]
    public static void Capture()
    {
        var camera=Array.Find(UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None),c=>c.name=="Anime hall player preview");
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var backdrop=UnityEngine.Object.FindFirstObjectByType<HallBackdrop>();
        var settings=rig.Settings;float oldHour=settings.previewHour,oldPan=art.lookLeft;
        bool oldOn=settings.previewHourOn;
        string report="ArtDeliverables/TimeDesk/Palette";Directory.CreateDirectory(report);
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        try
        {
            foreach(float hour in new[]{8f,12f,16.5f,22f})
            {
                settings.previewHourOn=true;settings.previewHour=hour;
                art.SetPan(0);art.SetTime(rig.Evening);
                typeof(HallLightingRig).GetMethod("LateUpdate",flags).Invoke(rig,null);
                UnityEngine.Object.FindFirstObjectByType<HallCityExterior>()?.Apply(0);
                UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
                UnityEngine.Object.FindFirstObjectByType<HallForegroundFloor>().Apply();
                var previous=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
                var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                try
                {
                    camera.targetTexture=target;camera.aspect=16f/9;
                    if(backdrop.Active) typeof(HallBackdrop).GetMethod("Sync",flags).Invoke(backdrop,null);
                    if(backdrop.Active) ((Camera)typeof(HallBackdrop).GetField("_camera",flags).GetValue(backdrop)).Render();
                    camera.Render();RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();
                    File.WriteAllBytes(report+"/petrol-graphite-"+hour.ToString("00",System.Globalization.CultureInfo.InvariantCulture)+".png",pixels.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture=previous;camera.aspect=aspect;RenderTexture.active=active;
                    target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
                }
            }
        }
        finally {settings.previewHour=oldHour;settings.previewHourOn=oldOn;art.SetPan(oldPan);art.SetTime(rig.Evening);UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();}
    }
}