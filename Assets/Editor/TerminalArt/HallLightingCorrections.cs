using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class HallLightingCorrections
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion";
    [MenuItem("Tools/Terminal Art/Lighting/Repair Sources And Shadows")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play first.");
        var art=Object.FindFirstObjectByType<AnimeHallPresentation>();
        var rig=Object.FindFirstObjectByType<HallLightingRig>();
        var reference=art.layers.First(l=>l.renderer!=null).renderer;
        var contacts=art.transform.Find("Painted contact shadows");
        // Remove unsupported patches instead of projecting them onto the wall.
        foreach(Transform child in contacts)
            if(child.name=="Contact 1" || child.name=="Contact 4" || child.name=="Contact 5")child.gameObject.SetActive(false);
        string path=Folder+"/GalleryFixture.png";
        var texture=new Texture2D(64,16,TextureFormat.RGBA32,false);
        var pixels=new Color32[64*16];
        for(int y=0;y<16;y++)for(int x=0;x<64;x++)
        {
            Color32 c=new Color32(0,0,0,0);
            if(x>=1&&x<=62&&y>=2&&y<=13)c=new Color32(32,39,46,255);
            if(x>=4&&x<=59&&y>=4&&y<=11)c=new Color32(99,106,108,255);
            if(x>=9&&x<=54&&y>=5&&y<=10)c=new Color32(255,245,213,255);
            if((x==5||x==58)&&(y==7||y==8))c=new Color32(185,191,189,255);
            pixels[y*64+x]=c;
        }
        texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=100;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
        var lampMaterial=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/GalleryFixture.mat");
        if(lampMaterial==null){lampMaterial=new Material(Shader.Find("NOPE/Hall Mounted Fixture"));AssetDatabase.CreateAsset(lampMaterial,Folder+"/GalleryFixture.mat");}
        lampMaterial.SetTexture("_MainTex",sprite.texture);EditorUtility.SetDirty(lampMaterial);
        Vector2[] positions={new(1535,121),new(1815,117),new(2050,127)};
        for(int i=0;i<positions.Length;i++)
        {
            string name="Gallery mounted fixture "+i;
            var child=art.transform.Find(name);
            if(child==null){var g=new GameObject(name);g.transform.SetParent(art.transform,false);child=g.transform;}
            child.gameObject.layer=reference.gameObject.layer;
            child.localRotation=reference.transform.localRotation*Quaternion.Euler(0,0,90);child.localScale=reference.transform.localScale;
            var p=positions[i];
            child.position=reference.transform.TransformPoint(new Vector3((p.x-reference.sprite.pivot.x)/100,(724-p.y-reference.sprite.pivot.y)/100,-.003f));
            var renderer=child.GetComponent<SpriteRenderer>();if(renderer==null)renderer=child.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite=sprite;renderer.sharedMaterial=lampMaterial;renderer.sortingLayerID=reference.sortingLayerID;renderer.sortingOrder=99;
            var source=child.GetComponent<HallLight>();if(source==null)source=child.gameObject.AddComponent<HallLight>();
            source.kind=HallLightKind.Fixture;source.order=6+i;
            var mounted=child.GetComponent<HallMountedFixture>();if(mounted==null)mounted=child.gameObject.AddComponent<HallMountedFixture>();
            mounted.rig=rig;mounted.drawing=renderer;mounted.floorPool=new Vector4(p.x/2172,1-282f/724,.07f,.026f);
        }
        HallGestureReview.Apply();
        var material=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/ContactShadows.mat");
        material.SetFloat("_Strength",.3f);EditorUtility.SetDirty(material);
        EditorSceneManager.MarkSceneDirty(art.gameObject.scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(art.gameObject.scene);
        HallFourStateBaker.Bake();
    }
}



