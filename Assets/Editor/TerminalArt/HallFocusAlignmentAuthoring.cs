using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class HallFocusAlignmentAuthoring
{
    const string Folder="ArtDeliverables/TimeDesk/City/FocusAlignment";
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static Camera Camera()=>UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Anime hall player preview");
    static Transform[] All()=>EditorSceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
    static Transform Find(string name)=>All().First(t=>t.name==name);
    static string Path(Transform t)=>t.parent==null?t.name:Path(t.parent)+"/"+t.name;
    [MenuItem("Tools/Terminal Art/Composition/Measure Focus Alignment")]
    public static void Measure()
    {
        Directory.CreateDirectory(Folder);var camera=Camera();var report=new StringBuilder();
        report.AppendLine($"Camera {camera.transform.position} rotation {camera.transform.eulerAngles} FOV {camera.fieldOfView}");
        foreach(var t in All().Where(t=>t.name.StartsWith("Clean_Next") || t.name.StartsWith("Clean_Blotter") || t.name.StartsWith("Finish_Board") || t.name=="NextLabel" || t.name.StartsWith("Anchor_") || t.name=="Desk"))
        {
            var r=t.GetComponent<Renderer>();var point=r!=null?r.bounds.center:t.position;
            report.AppendLine($"{Path(t)} active {t.gameObject.activeInHierarchy} world {t.position} centre {point} viewport {camera.WorldToViewportPoint(point)}");
        }
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var hall=art.layers.First(l=>l.id.StartsWith("62 ")).renderer;
        var focus=hall.transform.TransformPoint(new Vector3((1092-1086)/100f,(362-479)/100f,0));
        report.AppendLine($"Hall main portal centre {camera.WorldToViewportPoint(focus)} pan {art.lookLeft} forward {art.forwardLocalPosition} left {art.leftLocalPosition}");
        File.WriteAllText(Folder+"/measurements.txt",report.ToString());
    }
    static void Follow()
    {
        UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>().Apply();
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        typeof(HallLightingRig).GetMethod("LateUpdate",Private).Invoke(rig,null);
        UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
        UnityEngine.Object.FindFirstObjectByType<HallCityExterior>().Apply(0);
        UnityEngine.Object.FindFirstObjectByType<HallForegroundFloor>().Apply();
        foreach(var board in UnityEngine.Object.FindObjectsByType<DepartureBoardView>(FindObjectsSortMode.None))typeof(DepartureBoardView).GetMethod("LateUpdate",Private).Invoke(board,null);
        foreach(var effect in UnityEngine.Object.FindObjectsByType<PortalEffect>(FindObjectsSortMode.None))typeof(PortalEffect).GetMethod("LateUpdate",Private).Invoke(effect,null);
        foreach(var registration in UnityEngine.Object.FindObjectsByType<HallDeepPortalRegistration>(FindObjectsSortMode.None))typeof(HallDeepPortalRegistration).GetMethod("LateUpdate",Private).Invoke(registration,null);
    }
    public static void Capture(string file,bool traveller)
    {
        Directory.CreateDirectory(Folder);var cam=Camera();var backdrop=UnityEngine.Object.FindFirstObjectByType<HallBackdrop>();int oldMask=cam.cullingMask;
        var previous=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;
        var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
        GameObject preview=null;CharacterArt characterArt=null;
        try
        {
            if(traveller)
            {
                cam.cullingMask|=OfficeLayers.GameplayMask;
                var config=AssetDatabase.LoadAssetAtPath<DeskConfigSO>("Assets/Data/Config/Desk_Default.asset");
                preview=new GameObject("Focus alignment traveller preview"){hideFlags=HideFlags.DontSave};preview.transform.position=Find("Anchor_Traveller").position;
                preview.transform.localScale=Vector3.one*(config.travellerHeight/LookCanvas.LocalY(LookCanvas.HeadTop));preview.AddComponent<SortingGroup>().sortingLayerName=OfficeLayers.SortingLayer;
                var stack=preview.AddComponent<LookSpriteStack>();var so=new SerializedObject(stack);var slots=so.FindProperty("layers");slots.arraySize=Enum.GetValues(typeof(LookLayer)).Length;
                for(int i=0;i<slots.arraySize;i++){var child=new GameObject("Preview layer "+i);child.transform.SetParent(preview.transform,false);var sprite=child.AddComponent<SpriteRenderer>();sprite.sortingOrder=i;sprite.color=config.travellerTint;slots.GetArrayElementAtIndex(i).objectReferenceValue=sprite;}
                so.ApplyModifiedPropertiesWithoutUndo();characterArt=new CharacterArt(null);stack.Show(Looks.Whole("socrates",null,null),characterArt);
            }
            Follow();cam.targetTexture=rt;cam.aspect=16f/9;
            if(backdrop.Active){typeof(HallBackdrop).GetMethod("Sync",Private).Invoke(backdrop,null);((Camera)typeof(HallBackdrop).GetField("_camera",Private).GetValue(backdrop)).Render();}
            cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();File.WriteAllBytes(Folder+"/"+file+".png",pixels.EncodeToPNG());
        }
        finally{if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);characterArt?.Dispose();cam.cullingMask=oldMask;cam.targetTexture=previous;cam.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(pixels);}
    }
    [MenuItem("Tools/Terminal Art/Composition/Capture Before Alignment")]
    public static void Before(){Capture("before-empty",false);Capture("before-traveller",true);Measure();}
    [MenuItem("Tools/Terminal Art/Composition/Apply Focus Alignment")]
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before saving composition.");
        var scene=EditorSceneManager.GetActiveScene();if(scene.path!="Assets/Art/Office/AnimeHallLayers/AnimeHall.unity")throw new InvalidOperationException("Open AnimeHall.");
        var cam=Camera();var all=All();var traveller=Find("Anchor_Traveller");
        // The guest's original distance and height remain the composition anchor.
        float axis=traveller.position.x;
        var glass=all.First(t=>t.name=="Clean_Next__DeskClean_Glass").GetComponent<Renderer>();
        var signShift=new Vector3(axis-glass.bounds.center.x,0,0);
        var sign=all.Where(t=>t.name.StartsWith("Clean_Next__") || (t.name=="NextLabel" && t.GetComponent<TMPro.TMP_Text>()!=null)).ToArray();
        foreach(var t in sign.Where(t=>!sign.Any(p=>p!=t && t.IsChildOf(p)))){Undo.RecordObject(t,"Centre AVAILABLE sign");t.position+=signShift;}
        var blotter=all.First(t=>t.name=="Clean_Blotter__DeskClean_Pad").GetComponent<Renderer>();
        var padShift=new Vector3(axis-blotter.bounds.center.x,0,0);
        foreach(var t in all.Where(t=>t.name.StartsWith("Clean_Blotter__"))){Undo.RecordObject(t,"Centre desk work surface");t.position+=padShift;}
        var handover=all.FirstOrDefault(t=>t.name=="Anchor_HandOver");if(handover!=null){Undo.RecordObject(handover,"Centre document hand-over");handover.position=new Vector3(axis,handover.position.y,handover.position.z);}
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();Undo.RecordObject(art,"Frame hall around traveller");
        var originalPan=art.lookLeft;art.SetPan(0);var hall=art.layers.First(l=>l.id.StartsWith("62 ")).renderer;
        var point=hall.transform.TransformPoint(new Vector3((1092-1086)/100f,(362-479)/100f,0));var viewport=cam.WorldToViewportPoint(point);
        float targetX=cam.WorldToViewportPoint(traveller.position).x;
        var desired=cam.ViewportToWorldPoint(new Vector3(targetX,viewport.y,viewport.z));var worldDelta=desired-point;
        var delta=art.transform.parent!=null?art.transform.parent.InverseTransformVector(worldDelta):worldDelta;
        art.forwardLocalPosition+=delta;art.leftLocalPosition+=delta;art.SetPan(0);
        Follow();EditorUtility.SetDirty(art);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Folder+"/changes.txt",$"Traveller distance/height unchanged. Sign world delta {signShift}; blotter world delta {padShift}; hall horizontal world delta {worldDelta}. Root scale, camera, source artwork and lighting retained. Focus is the guest, not a vanishing-point target. Normal view centred; existing left-pan travel retained. Original pan {originalPan}.");
        Capture("after-empty",false);Capture("after-traveller",true);Measure();EditorApplication.ExecuteMenuItem("Window/General/Game");
    }
    [MenuItem("Tools/Terminal Art/Composition/Finish Focus Framing")]
    public static void FinishFraming()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before saving framing.");
        var cam=Camera();var scene=EditorSceneManager.GetActiveScene();var all=All();
        var model=all.First(t=>t.name=="Clean_Next__DeskClean_Glass").parent;
        foreach(int side in new[]{-1,1})
        {
            string name=side<0?"Left board assembly":"Right board assembly";
            var assembly=model.Find(name);
            if(assembly==null)
            {
                var wood=all.First(t=>t.name=="Finish_Board__Finish_Wood" && Mathf.Sign(t.position.x)==side).GetComponent<Renderer>();
                var go=new GameObject(name);Undo.RegisterCreatedObjectUndo(go,"Frame corkboards");assembly=go.transform;assembly.SetParent(model,false);assembly.position=wood.bounds.center;
                var members=all.Where(t=>Mathf.Sign(t.position.x)==side && (t.name.StartsWith("Finish_Board__") || t.name.StartsWith("Finish_LeftEphemera__") || t.name.StartsWith("Finish_RightEphemera__") || t.name.StartsWith("Office_Clock__") || t.name.StartsWith("Office_Calendar__") || t.name.StartsWith("Office_Stability__") || t.name.StartsWith("Office_BoardBracket__") || new[]{"DayNumber","StabilityPercent","ShiftClockDisplay"}.Contains(t.name))).ToArray();
                foreach(var t in members.Where(t=>!members.Any(p=>p!=t && t.IsChildOf(p))))Undo.SetTransformParent(t,assembly,"Keep board parts together");
            }
            Undo.RecordObject(assembly,"Open hall sightlines");assembly.localScale=Vector3.one*.8f;
            var viewport=cam.WorldToViewportPoint(assembly.position);float x=side<0?.07f:.93f;
            assembly.position=cam.ViewportToWorldPoint(new Vector3(x,viewport.y,viewport.z));
        }
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();Undo.RecordObject(art,"Keep pan inside source canvas");
        var hall=art.layers.First(l=>l.id.StartsWith("62 ")).renderer;art.SetPan(1);
        var edge=hall.transform.TransformPoint(new Vector3(-10.86f,0,0));var uv=cam.WorldToViewportPoint(edge);
        if(uv.x>-.01f)
        {
            var delta=cam.ViewportToWorldPoint(new Vector3(-.01f,uv.y,uv.z))-edge;
            art.leftLocalPosition+=art.transform.parent!=null?art.transform.parent.InverseTransformVector(delta):delta;
        }
        art.SetPan(0);
        var display=Find("Anchor_DepartureBoard") as RectTransform;
        if(display!=null)
        {
            Undo.RecordObject(display,"Fit live departure text to display");
            display.localPosition=new Vector3((1060-1086)/100f,(362-126)/100f,-.002f);
            display.sizeDelta=new Vector2(2.6f,.9f);
        }
        Follow();EditorUtility.SetDirty(art);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        Capture("after-empty",false);Capture("after-traveller",true);art.SetPan(1);Capture("after-left-pan",false);art.SetPan(0);Follow();Measure();
        File.AppendAllText(Folder+"/changes.txt","\nCorkboard assemblies retain every mesh/readout at 80% scale, positioned at viewport x0.07/0.93 to uncover stairs and right doors. Left-pan endpoint bounded to original source edge with 1% overscan; no unpainted edge exposed. Normal-view focus column remains x0.5.");
    }
    [MenuItem("Tools/Terminal Art/Composition/Capture Aligned Composition")]
    public static void Aligned(){Capture("after-empty",false);Capture("after-traveller",true);Measure();}
    [MenuItem("Tools/Terminal Art/Composition/Capture Live Alignment")]
    public static void Live(){Capture("live-traveller",false);Measure();}
}



