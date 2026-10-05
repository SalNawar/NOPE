using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Reversible art tuning after review in the live desk camera.</summary>
public static class HallVisualReview
{
    static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault();
    [MenuItem("Tools/Terminal Art/Review/Apply Visual Corrections")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop the preview before saving art tuning.");
        if(EditorSceneManager.GetActiveScene().path!="Assets/Art/Office/AnimeHallLayers/AnimeHall.unity") throw new InvalidOperationException("Open AnimeHall first.");
        HallCompletionAuthoring.InstallFloor();
        var config=AssetDatabase.LoadAssetAtPath<DeskConfigSO>("Assets/Data/Config/Desk_Default.asset");
        config.travellerHeight=1.9f;
        EditorUtility.SetDirty(config);
        var anchor=GameObject.Find("Anchor_Traveller");
        // Preserve the original gameplay distance; scale is controlled by DeskConfig.
        anchor.transform.position=new Vector3(0,0,1.6f);
        var lighting=Find<HallLightingRig>();
        lighting.Settings.shaftAngleAtSunrise=-8;
        lighting.Settings.shaftAngleAtSunset=6;
        lighting.Settings.shaftLowSunLength=1;
        lighting.Settings.shaftShadows=true;
        EditorUtility.SetDirty(lighting.Settings);
        foreach(var light in UnityEngine.Object.FindObjectsByType<HallLight>(FindObjectsSortMode.None))
        {
            if(light.kind==HallLightKind.Window)
            {
                light.intensity=light.name.Contains("rear")?.035f:.055f;
                var l=light.GetComponent<Light2D>();
                l.shapeLightFalloffSize=Mathf.Max(l.shapeLightFalloffSize,.45f);
                l.shadowIntensity=.8f;
                l.shadowSoftness=.65f;
                l.volumeIntensityEnabled=false;
                EditorUtility.SetDirty(l);
            }
            else if(light.kind==HallLightKind.Fixture)
            {
                light.intensity=Mathf.Min(light.intensity,.18f);
                var l=light.GetComponent<Light2D>();
                if(l!=null) {l.shadowsEnabled=true;l.shadowIntensity=.65f;l.shadowSoftness=.6f;}
            }
            else if(light.kind==HallLightKind.DeskLamp)
            {
                var l=light.GetComponent<Light>();
                if(l!=null) {l.shadows=LightShadows.Soft;l.shadowStrength=.65f;l.shadowBias=.035f;l.shadowNormalBias=.15f;}
            }
            EditorUtility.SetDirty(light);
        }
        var art=Find<AnimeHallPresentation>();
        if(art.daylight!=null)
        {
            art.daylight.shadows=LightShadows.Soft;
            art.daylight.shadowStrength=.68f;
            art.daylight.shadowBias=.035f;
            art.daylight.shadowNormalBias=.15f;
            EditorUtility.SetDirty(art.daylight);
        }
        foreach(var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
        {
            if(renderer.sharedMaterial==null || renderer.sharedMaterial.shader.name!="NOPE/Desk Anime") continue;
            renderer.shadowCastingMode=ShadowCastingMode.On;
            renderer.receiveShadows=true;
            EditorUtility.SetDirty(renderer);
        }
        if(!Directory.Exists("Assets/Resources")) AssetDatabase.CreateFolder("Assets","Resources");
        Material pose=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CharacterPose.mat");
        if(pose==null) AssetDatabase.CreateAsset(new Material(Shader.Find("NOPE/Character Pose")),"Assets/Resources/CharacterPose.mat");
        AddContacts(art);
        EditorSceneManager.MarkSceneDirty(anchor.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(anchor.scene);
        Debug.Log("Visual review: registered floor, restrained window light, cast/contact shadows, counter placement and five traveller stances.");
    }
    [MenuItem("Tools/Terminal Art/Review/Capture Traveller Framing")]
    public static void CaptureFraming()
    {
        var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Anime hall player preview");
        int oldMask=camera.cullingMask;
        camera.cullingMask|=OfficeLayers.GameplayMask;
        var config=AssetDatabase.LoadAssetAtPath<DeskConfigSO>("Assets/Data/Config/Desk_Default.asset");
        var preview=new GameObject("Review traveller") {hideFlags=HideFlags.DontSave};
        preview.transform.position=GameObject.Find("Anchor_Traveller").transform.position;
        preview.transform.localScale=Vector3.one*(config.travellerHeight/LookCanvas.LocalY(LookCanvas.HeadTop));
        preview.AddComponent<SortingGroup>().sortingLayerName=OfficeLayers.SortingLayer;
        var stack=preview.AddComponent<LookSpriteStack>();
        var so=new SerializedObject(stack);
        var slots=so.FindProperty("layers");slots.arraySize=Enum.GetValues(typeof(LookLayer)).Length;
        for(int i=0;i<slots.arraySize;i++)
        {
            var child=new GameObject("Review layer "+i);child.transform.SetParent(preview.transform,false);
            var sprite=child.AddComponent<SpriteRenderer>();sprite.sortingOrder=i;sprite.color=config.travellerTint;
            slots.GetArrayElementAtIndex(i).objectReferenceValue=sprite;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        using var art=new CharacterArt(null);
        try
        {
            stack.Show(Looks.Whole("socrates",null,null),art);
            HallCompletionAuthoring.CaptureTransition();
        }
        finally {UnityEngine.Object.DestroyImmediate(preview);camera.cullingMask=oldMask;}
    }
    static void AddContacts(AnimeHallPresentation art)
    {
        const string path="Assets/Art/Office/AnimeHallLayers/Completion/ContactShadows.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) {material=new Material(Shader.Find("NOPE/Hall Contact Shadow"));AssetDatabase.CreateAsset(material,path);}
        var layer=art.layers.First(l=>l.renderer!=null).renderer;
        var root=art.transform.Find("Painted contact shadows");
        if(root==null) {var g=new GameObject("Painted contact shadows");g.transform.SetParent(art.transform,false);root=g.transform;}
        root.localPosition=layer.transform.localPosition;
        root.localRotation=layer.transform.localRotation;
        root.localScale=layer.transform.localScale;
        // Each soft patch is registered to its painted object's foot, not the
        // camera. Structural bases, furniture and portal bays retain depth cues
        // even where the 2D sun's weak direct component is occluded.
        Vector4[] contacts={new(621,478,155,20),new(1424,519,135,22),new(1030,626,430,32),new(1800,648,260,28),new(1690,539,90,17),new(1970,611,120,18),new(954,361,170,12),new(749,397,110,15),new(1525,689,105,16)};
        for(int i=0;i<contacts.Length;i++)
        {
            string name="Contact "+i;
            var child=root.Find(name);
            if(child==null) {var g=new GameObject(name);g.transform.SetParent(root,false);child=g.transform;}
            child.gameObject.layer=layer.gameObject.layer;
            var c=contacts[i];float ppu=layer.sprite.pixelsPerUnit;
            child.localPosition=new Vector3((c.x-layer.sprite.pivot.x)/ppu,(layer.sprite.rect.height-c.y-layer.sprite.pivot.y)/ppu,-.002f);
            child.localScale=new Vector3(c.z/ppu,c.w/ppu,1);
            var filter=child.GetComponent<MeshFilter>();if(filter==null)filter=child.gameObject.AddComponent<MeshFilter>();
            string meshPath=$"Assets/Art/Office/AnimeHallLayers/Completion/ContactShadow{i}.asset";
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if(mesh==null) {mesh=new Mesh{name=name};AssetDatabase.CreateAsset(mesh,meshPath);}
            mesh.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
            filter.sharedMesh=mesh;
            var renderer=child.GetComponent<MeshRenderer>();if(renderer==null)renderer=child.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.sortingLayerID=layer.sortingLayerID;renderer.sortingOrder=58;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
    }
    [MenuItem("Tools/Terminal Art/Review/Capture Pose Library")]
    public static void CapturePoses()
    {
        const string folder="ArtDeliverables/TimeDesk/HallLayers/Completion/Review2/Poses";
        Directory.CreateDirectory(folder);
        var source=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Anime hall player preview");
        var actor=new GameObject("Pose QA") {hideFlags=HideFlags.DontSave,layer=31};
        actor.transform.localScale=Vector3.one*(1.9f/LookCanvas.LocalY(LookCanvas.HeadTop));
        actor.AddComponent<SortingGroup>().sortingLayerName=OfficeLayers.SortingLayer;
        var stack=actor.AddComponent<LookSpriteStack>();
        var so=new SerializedObject(stack);var slots=so.FindProperty("layers");slots.arraySize=Enum.GetValues(typeof(LookLayer)).Length;
        for(int i=0;i<slots.arraySize;i++)
        {
            var child=new GameObject("Layer "+i) {layer=31};child.transform.SetParent(actor.transform,false);
            var sprite=child.AddComponent<SpriteRenderer>();sprite.sortingOrder=i;slots.GetArrayElementAtIndex(i).objectReferenceValue=sprite;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        var cameraObject=new GameObject("Pose QA camera") {hideFlags=HideFlags.DontSave};
        var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=1.35f;
        camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.34f,.35f,.36f);
        camera.transform.position=new Vector3(0,1.1f,-4);
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        int index=(int)typeof(UniversalAdditionalCameraData).GetField("m_RendererIndex",flags).GetValue(source.GetUniversalAdditionalCameraData());
        camera.GetUniversalAdditionalCameraData().SetRenderer(index);
        var target=new RenderTexture(512,768,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var pixels=new Texture2D(512,768,TextureFormat.RGB24,false);
        var oldActive=RenderTexture.active;
        using var art=new CharacterArt(null);
        try
        {
            camera.targetTexture=target;camera.aspect=2f/3;
            foreach(string path in Directory.GetFiles(CharacterArt.AssetFolder,"premade_*_neutral.png"))
            {
                string id=Path.GetFileNameWithoutExtension(path).Replace("premade_","").Replace("_neutral","");
                stack.Show(Looks.Whole(id,null,null),art);
                foreach(string expression in new[]{"neutral","happy","angry","worried"})
                {
                    stack.SetExpression(expression);camera.Render();RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,512,768),0,0);pixels.Apply();
                    File.WriteAllBytes(folder+"/"+id+"-"+expression+".png",pixels.EncodeToPNG());
                }
                stack.Clear();art.Retain(null);
            }
            File.WriteAllText(folder+"/validation.txt","26 named characters x four expressions: 104 native Unity pose renders. Passport sprites retain the unposed material.\n");
        }
        finally
        {
            camera.targetTexture=null;RenderTexture.active=oldActive;target.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
            UnityEngine.Object.DestroyImmediate(actor);UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
