using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Art-only, reversible captures of the actual office-to-desk camera path.</summary>
public static class HallCompletionAuthoring
{
    const string Report = "ArtDeliverables/TimeDesk/HallLayers/Completion/FourState/Transitions";
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();

    [MenuItem("Tools/Terminal Art/Completion/Validate Scene")]
    public static void ValidateScene()
    {
        Directory.CreateDirectory(Report);
        var art=Find<AnimeHallPresentation>();
        var rig=Find<HallLightingRig>();
        var objects=UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        int missing=objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount);
        var lights=UnityEngine.Object.FindObjectsByType<HallLight>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        var text=$"Scene: {EditorSceneManager.GetActiveScene().path}\nRegistered layers: {art.layers.Length}\nMissing scripts: {missing}\nLighting enabled: {rig.Settings.lightingOn}\nFloor installed: {Find<HallForegroundFloor>()!=null}\n";
        foreach(var group in lights.GroupBy(l=>l.kind)) text+=$"{group.Key}: {group.Count()}\n";
        File.WriteAllText(Report+"/scene-validation.txt",text);
        if(missing>0 || art.layers.Length!=58 || Find<HallForegroundFloor>()==null) throw new Exception(text);
        Debug.Log(text);
    }

    [MenuItem("Tools/Terminal Art/Completion/Install Foreground Floor")]
    public static void InstallFloor()
    {
        const string folder="Assets/Art/Office/AnimeHallLayers/Completion";
        var texturePath=folder+"/TerracottaFloor.png";
        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
        importer.textureType=TextureImporterType.Default;
        importer.wrapMode=TextureWrapMode.Repeat;
        importer.sRGBTexture=true;
        importer.mipmapEnabled=true;
        importer.SaveAndReimport();
        var meshPath=folder+"/ForegroundFloor.asset";
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(mesh==null){mesh=new Mesh{name="Hall foreground floor"};AssetDatabase.CreateAsset(mesh,meshPath);}
        mesh.Clear();
        var art=Find<AnimeHallPresentation>();
        var reference=art.layers.First(l=>l.renderer!=null).renderer;
        var sprite=reference.sprite;
        float ppu=sprite.pixelsPerUnit, width=sprite.rect.width, height=sprite.rect.height;
        float left=-sprite.pivot.x/ppu, right=(width-sprite.pivot.x)/ppu;
        float bottom=-sprite.pivot.y/ppu, extension=1000/ppu;
        mesh.vertices=new[]{new Vector3(left,bottom-extension,0),new Vector3(left,bottom+.5f/ppu,0),new Vector3(right,bottom+.5f/ppu,0),new Vector3(right,bottom-extension,0)};
        mesh.uv=new[]{new Vector2(0,-1000/height),new Vector2(0,.5f/height),new Vector2(1,.5f/height),new Vector2(1,-1000/height)};
        mesh.triangles=new[]{0,1,2,0,2,3};
        mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var materialPath=folder+"/ForegroundFloor.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("NOPE/Hall Foreground Floor"));AssetDatabase.CreateAsset(material,materialPath);}
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        material.SetTexture("_PaintedReference",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/PaintedReference.png"));
        material.SetColor("_BaseColor",Color.white);EditorUtility.SetDirty(material);
        var go=GameObject.Find("Hall Foreground Floor");
        if(go==null){go=new GameObject("Hall Foreground Floor");Undo.RegisterCreatedObjectUndo(go,"Add hall floor");}
        go.layer=29;
        go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
        go.transform.localScale=Vector3.one;
        var filter=go.GetComponent<MeshFilter>();if(filter==null)filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
        var renderer=go.GetComponent<MeshRenderer>();if(renderer==null)renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
        renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        var floor=go.GetComponent<HallForegroundFloor>();if(floor==null)floor=go.AddComponent<HallForegroundFloor>();
        floor.Configure(art,Find<HallLightingRig>(),reference);
        EditorSceneManager.MarkSceneDirty(go.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(go.scene);
        Debug.Log("Installed perspective floor, preserving existing desk and gameplay hooks.");
    }

    [MenuItem("Tools/Terminal Art/Completion/Capture Camera Transition")]
    public static void CaptureTransition()
    {
        Directory.CreateDirectory(Report);
        var camera = FindCamera();
        var presentation = Find<AnimeHallPresentation>();
        var lighting = Find<HallLightingRig>();
        var backdrop = Find<HallBackdrop>();
        var settings = lighting.Settings;
        var startPosition = camera.transform.position;
        var startRotation = camera.transform.rotation;
        bool oldPreview = settings.previewHourOn;
        bool oldLighting = settings.lightingOn;
        float oldHour = settings.previewHour;
        float oldPan = presentation.lookLeft;
        float oldEvening = presentation.evening;
        var brain = camera.GetComponent<Unity.Cinemachine.CinemachineBrain>();
        bool brainOn = brain != null && brain.enabled;
        if (brain != null) brain.enabled = false;
        var mat = GameObject.Find("Clean_Blotter__DeskClean_Pad").GetComponent<Renderer>().bounds.center;
        var config = AssetDatabase.LoadAssetAtPath<DeskConfigSO>("Assets/Data/Config/Desk_Default.asset");
        var forward = Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
        var tuning = config.deskView;
        // The desk pose exactly as DeskView builds it (aim on the mat, back and up by the knobs' pitch and distance).
        var right = Vector3.Cross(Vector3.up,forward);
        var aim = mat + right*tuning.aimRight + forward*tuning.aimForward;
        (float back, float up) = DeskViewPose.Offset(tuning);
        var endPosition = aim - forward*back + Vector3.up*up;
        var endRotation = Quaternion.LookRotation(forward,Vector3.up)*Quaternion.Euler(DeskViewPose.Pitch(tuning),0,0);
        try
        {
            foreach(float pan in new[]{0f,1f})
            foreach(float hour in new[]{8f,12f,16.5f,22f})
            {
                presentation.SetPan(pan);
                settings.previewHourOn = true;
                settings.previewHour = hour;
                presentation.SetTime(lighting.Evening);
                for(int i=0;i<=4;i++)
                {
                    float t=i/4f;
                    camera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition,endPosition,t),Quaternion.Slerp(startRotation,endRotation,t));
                    lighting.GetType().GetMethod("LateUpdate",Private).Invoke(lighting,null);
                    Find<HallForegroundFloor>()?.Apply();
                    Find<HallGroundShadows>()?.Apply(); Find<HallBakedLighting>()?.Apply();
                    Capture(camera,backdrop,$"{(pan>0?"left-":"")}hour-{hour:00}-tilt-{i}.png");
                }
            }
            camera.transform.SetPositionAndRotation(startPosition,startRotation);
            presentation.SetPan(1);
            settings.previewHour = 12;
            presentation.SetTime(lighting.Evening);
            lighting.GetType().GetMethod("LateUpdate",Private).Invoke(lighting,null);
            Find<HallForegroundFloor>()?.Apply();
            Find<HallGroundShadows>()?.Apply(); Find<HallBakedLighting>()?.Apply();
            Capture(camera,backdrop,"left-pan.png");
            settings.lightingOn=false;
            presentation.SetTime(0);
            foreach(float pan in new[]{0f,1f})
            foreach(int i in new[]{0,4})
            {
                presentation.SetPan(pan);
                float t=i/4f;
                camera.transform.SetPositionAndRotation(Vector3.Lerp(startPosition,endPosition,t),Quaternion.Slerp(startRotation,endRotation,t));
                lighting.GetType().GetMethod("LateUpdate",Private).Invoke(lighting,null);
                Find<HallForegroundFloor>()?.Apply();
                Find<HallGroundShadows>()?.Apply(); Find<HallBakedLighting>()?.Apply();
                Capture(camera,backdrop,$"unlit-pan-{pan:0}-tilt-{i}.png");
            }
            File.WriteAllText(Report+"/camera-path.txt",$"Start: {startPosition}; rotation: {startRotation.eulerAngles}\nDesk: {endPosition}; rotation: {endRotation.eulerAngles}\nMat centre: {mat}\nCaptures: 5 transition positions at noon, sunset and night in both pans; lighting-off endpoints in both pans.\n");
        }
        finally
        {
            presentation.SetPan(oldPan);
            presentation.SetTime(oldEvening);
            settings.previewHourOn=oldPreview;
            settings.lightingOn=oldLighting;
            settings.previewHour=oldHour;
            camera.transform.SetPositionAndRotation(startPosition,startRotation);
            if(brain != null) brain.enabled=brainOn;
            lighting.GetType().GetMethod("LateUpdate",Private).Invoke(lighting,null);
            Find<HallForegroundFloor>()?.Apply();
            Find<HallGroundShadows>()?.Apply(); Find<HallBakedLighting>()?.Apply();
        }
        Debug.Log("Hall completion camera captures saved to "+Report);
    }

    static Camera FindCamera() => UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Anime hall player preview");

    static void Capture(Camera camera,HallBackdrop backdrop,string filename)
    {
        var oldTarget=camera.targetTexture;
        var oldActive=RenderTexture.active;
        float oldAspect=camera.aspect;
        var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
        var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        try
        {
            camera.targetTexture=target;
            camera.aspect=16f/9;
            if(backdrop!=null && backdrop.Active)
            {
                typeof(HallBackdrop).GetMethod("Sync",Private).Invoke(backdrop,null);
                var hall=(Camera)typeof(HallBackdrop).GetField("_camera",Private).GetValue(backdrop);
                hall.Render();
            }
            camera.Render();
            RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);
            pixels.Apply();
            File.WriteAllBytes(Report+"/"+filename,pixels.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture=oldTarget;
            camera.aspect=oldAspect;
            RenderTexture.active=oldActive;
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}

