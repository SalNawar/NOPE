using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Preview only: the existing drawing stays intact; no scene or asset assignments are saved.
public static class HallDetailedCalibrationAuthoring
{
    const string Folder="ArtDeliverables/TimeDesk/City/DeeperRoom/Calibration";
    [MenuItem("Tools/Terminal Art/City/Preview Detailed Horizon Calibration")]
    public static void Preview()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before the isolated preview.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var hall=art.layers.First(l=>l.id.StartsWith("62 ")).renderer;
        var cam=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Anime hall player preview");
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        var floor=UnityEngine.Object.FindFirstObjectByType<HallForegroundFloor>();
        var backdrop=UnityEngine.Object.FindFirstObjectByType<HallBackdrop>();
        var oldScale=art.transform.localScale;var oldForward=art.forwardLocalPosition;var oldLeft=art.leftLocalPosition;float oldPan=art.lookLeft;
        float oldHour=rig.Settings.previewHour;bool oldOn=rig.Settings.previewHourOn;
        GameObject extension=null;Sprite sprite=null;Material material=null;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        void Follow()
        {
            art.Apply();floor.Apply();
            foreach(var board in UnityEngine.Object.FindObjectsByType<DepartureBoardView>(FindObjectsSortMode.None))typeof(DepartureBoardView).GetMethod("LateUpdate",flags).Invoke(board,null);
            foreach(var effect in UnityEngine.Object.FindObjectsByType<PortalEffect>(FindObjectsSortMode.None))typeof(PortalEffect).GetMethod("LateUpdate",flags).Invoke(effect,null);
            foreach(var registration in UnityEngine.Object.FindObjectsByType<HallDeepPortalRegistration>(FindObjectsSortMode.None))typeof(HallDeepPortalRegistration).GetMethod("LateUpdate",flags).Invoke(registration,null);
        }
        Vector3 Pixel(float x,float y)=>hall.transform.TransformPoint(new Vector3((x-1086)/100f,(362-y)/100f,0));
        void Capture(string name)
        {
            Follow();
            var previous=cam.targetTexture;var active=RenderTexture.active;float aspect=cam.aspect;
            var rt=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            try
            {
                cam.targetTexture=rt;cam.aspect=16f/9;
                if(backdrop.Active){typeof(HallBackdrop).GetMethod("Sync",flags).Invoke(backdrop,null);((Camera)typeof(HallBackdrop).GetField("_camera",flags).GetValue(backdrop)).Render();}
                cam.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();
                File.WriteAllBytes(Folder+"/"+name+".png",pixels.EncodeToPNG());
            }
            finally{cam.targetTexture=previous;cam.aspect=aspect;RenderTexture.active=active;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(pixels);}
        }
        try
        {
            Directory.CreateDirectory(Folder);rig.Settings.previewHourOn=true;rig.Settings.previewHour=12;
            typeof(HallLightingRig).GetMethod("LateUpdate",flags).Invoke(rig,null);UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
            art.SetPan(0);Capture("before");
            // Fit the complete original panorama at its painted horizon, then register that horizon
            // to the player's real horizontal plane. No drawing pixels or object identities change.
            float horizon=cam.WorldToViewportPoint(cam.transform.position+Vector3.forward*10000).y;
            for(int iteration=0;iteration<3;iteration++)
            {
                var left=cam.WorldToViewportPoint(Pixel(0,300));var right=cam.WorldToViewportPoint(Pixel(2172,300));
                art.transform.localScale*=1f/(right.x-left.x);
                art.Apply();var vp=Pixel(1090,300);float depth=cam.WorldToViewportPoint(vp).z;
                var desired=cam.ViewportToWorldPoint(new Vector3(.5f,horizon,depth));
                var delta=art.transform.parent!=null?art.transform.parent.InverseTransformVector(desired-vp):desired-vp;
                art.forwardLocalPosition+=delta;art.leftLocalPosition=art.forwardLocalPosition;art.Apply();
            }
            var tex=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Office/AnimeHallLayers/Completion/DeepRoom/CeilingExtensionDraft.png");
            if(tex!=null)
            {
                float cropHeight=228;
                sprite=Sprite.Create(tex,new Rect(0,tex.height-cropHeight,tex.width,cropHeight),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect);
                extension=new GameObject("Ceiling extension preview",typeof(SpriteRenderer)){hideFlags=HideFlags.DontSave,layer=hall.gameObject.layer};
                extension.transform.SetParent(hall.transform,false);
                float factor=2172f/tex.width;extension.transform.localScale=new Vector3(factor,factor,1);
                extension.transform.localPosition=new Vector3(0,3.62f+cropHeight*factor/200f,0);
                var renderer=extension.GetComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingLayerID=hall.sortingLayerID;renderer.sortingOrder=hall.sortingOrder;
                material=new Material(hall.sharedMaterial){hideFlags=HideFlags.DontSave};
                material.SetTexture("_MainTex",tex);material.SetTexture("_Masks",Texture2D.blackTexture);material.SetFloat("_Region",0);
                material.SetVector("_CanvasSize",new Vector4(tex.width,tex.height,0,0));material.SetFloat("_CeilingCutoff",tex.height);
                renderer.sharedMaterial=material;
            }
            Capture("complete-hall-horizon-preview");
            var actual=cam.WorldToViewportPoint(Pixel(1090,300));
            File.WriteAllText(Folder+"/registration.txt",$"PREVIEW ONLY, scene restored afterward. Complete original HallDeepMorning texture retained. Painted VP viewport {actual}; camera level horizon {horizon:F6}; root scale {art.transform.localScale}; pan endpoints collapsed only during preview. Ceiling draft is a separate sprite. No camera or desk changes. Generated whole-scene draft rejected. Requires seam and pan authoring before installation.");
        }
        finally
        {
            if(extension!=null)UnityEngine.Object.DestroyImmediate(extension);
            if(sprite!=null)UnityEngine.Object.DestroyImmediate(sprite);
            if(material!=null)UnityEngine.Object.DestroyImmediate(material);
            art.transform.localScale=oldScale;art.forwardLocalPosition=oldForward;art.leftLocalPosition=oldLeft;art.SetPan(oldPan);
            rig.Settings.previewHour=oldHour;rig.Settings.previewHourOn=oldOn;
            typeof(HallLightingRig).GetMethod("LateUpdate",flags).Invoke(rig,null);UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();Follow();
        }
    }
}
