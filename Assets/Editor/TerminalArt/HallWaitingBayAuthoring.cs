using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HallWaitingBayAuthoring
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/Palette";
    [MenuItem("Tools/Terminal Art/Palette/Install Clear Window Waiting Bay")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        if(art==null || art.gameObject.scene.path!="Assets/Art/Office/AnimeHallLayers/AnimeHall.unity")throw new InvalidOperationException("Open AnimeHall.");
        string path=Folder+"/WaitingBayClearWindows.png";
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;
        importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
        var guide=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(guide.width!=2172 || guide.height!=724)throw new InvalidOperationException("Unexpected waiting bay registration.");
        var seat=art.layers.First(l=>l.id.StartsWith("27 ")).renderer;
        var water=art.layers.First(l=>l.id.StartsWith("28 ")).renderer;
        var baseline=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Office/AnimeHallLayers/Completion/FourState/HallFourState.mat");
        void Patch(string name,string id,bool restoration)
        {
            string matPath=Folder+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){mat=new Material(baseline);AssetDatabase.CreateAsset(mat,matPath);}
            mat.shader=Shader.Find("NOPE/Hall Waiting Bay Repair");
            mat.SetTexture("_WaitingGuide",guide);mat.SetTexture("_RemovedMask",water.sprite.texture);
            mat.SetTexture("_CityMorning",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/AnimeHallLayers/Completion/City/CityMorningConnected.png"));
            mat.SetFloat("_RestoreRemovedOnly",restoration?1:0);
            mat.SetVector("_RepairRect",restoration?new Vector4(935,345,1080,398):new Vector4(1920,475,2172,724));EditorUtility.SetDirty(mat);
            var existing=art.transform.Find(name);
            var go=existing!=null?existing.gameObject:new GameObject(name);
            go.transform.SetParent(seat.transform.parent,false);
            go.transform.localPosition=seat.transform.localPosition;
            go.transform.localRotation=seat.transform.localRotation;go.transform.localScale=seat.transform.localScale;
            go.layer=seat.gameObject.layer;
            var renderer=go.GetComponent<SpriteRenderer>();if(renderer==null)renderer=go.AddComponent<SpriteRenderer>();
            renderer.sprite=seat.sprite;renderer.sharedMaterial=mat;
            renderer.sortingLayerID=seat.sortingLayerID;
            renderer.sortingOrder=restoration?seat.sortingOrder:59;
            if(!art.layers.Any(l=>l.id==id))art.layers=art.layers.Concat(new[]{new AnimeHallPresentation.Layer{id=id,renderer=renderer}}).ToArray();
        }
        Patch("Cleared rear waiting area","58 Cleared rear waiting area",true);
        Patch("Wall-side waiting services","59 Wall-side waiting services",false);
        seat.gameObject.SetActive(false);water.gameObject.SetActive(false);
        var contact=art.transform.Find("Painted contact shadows/Contact 6");if(contact!=null)contact.gameObject.SetActive(false);
        EditorUtility.SetDirty(art);UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
        EditorSceneManager.MarkSceneDirty(art.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(art.gameObject.scene);
        Debug.Log("Cleared rear seating/dispenser; waiting services are placed against the solid right wall.");
    }
}
