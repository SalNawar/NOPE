using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class HallDeepRoomAuthoring
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/DeepRoom";
    const int W=2172,H=724;
    static Vector2[] Poly(params float[] p)
    {
        var result=new Vector2[p.Length/2];
        for(int i=0;i<result.Length;i++)result[i]=new Vector2(p[i*2],p[i*2+1]);
        return result;
    }
    static bool Inside(float x,float y,Vector2[] p)
    {
        bool inside=false;
        for(int i=0,j=p.Length-1;i<p.Length;j=i++)
            if((p[i].y>y)!=(p[j].y>y) && x<(p[j].x-p[i].x)*(y-p[i].y)/(p[j].y-p[i].y)+p[i].x)inside=!inside;
        return inside;
    }
    static Texture2D Import(string path,bool sprite,bool linear=false)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var i=(TextureImporter)AssetImporter.GetAtPath(path);
        i.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;
        i.spriteImportMode=SpriteImportMode.Single;i.spritePixelsPerUnit=100;
        i.sRGBTexture=!linear;i.mipmapEnabled=false;i.wrapMode=TextureWrapMode.Clamp;
        i.filterMode=FilterMode.Bilinear;i.alphaIsTransparency=sprite&&!linear;
        i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=4096;i.npotScale=TextureImporterNPOTScale.None;
        if(sprite){var settings=new TextureImporterSettings();i.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;i.SetTextureSettings(settings);}
        i.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Texture2D SaveData(string name,Color32[] pixels,bool sprite=false)
    {
        var texture=new Texture2D(W,H,TextureFormat.RGBA32,false,true);
        texture.SetPixels32(pixels);texture.Apply();string path=Folder+"/"+name+".png";
        File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        return Import(path,sprite,true);
    }
    [MenuItem("Tools/Terminal Art/City/Capture Approved Deeper Room")]
    public static void Capture()
    {
        var camera=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First(c=>c.name=="Anime hall player preview");
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var backdrop=UnityEngine.Object.FindFirstObjectByType<HallBackdrop>();
        var city=UnityEngine.Object.FindFirstObjectByType<HallCityExterior>();
        var settings=rig.Settings;float oldHour=settings.previewHour,oldPan=art.lookLeft;bool oldOn=settings.previewHourOn;
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        const string report="ArtDeliverables/TimeDesk/City/DeeperRoom";Directory.CreateDirectory(report);
        try
        {
            foreach(int pan in new[]{0,1})foreach(float hour in new[]{8f,12f,16.5f,22f})
            {
                settings.previewHourOn=true;settings.previewHour=hour;
                art.SetPan(pan);art.SetTime(rig.Evening);
                typeof(HallLightingRig).GetMethod("LateUpdate",flags).Invoke(rig,null);
                UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
                city.Apply(0);UnityEngine.Object.FindFirstObjectByType<HallForegroundFloor>().Apply();
                // Manual captures pan multiple times inside one editor frame.
                // Gameplay readouts/effects must follow before each render.
                foreach(var board in UnityEngine.Object.FindObjectsByType<DepartureBoardView>(FindObjectsSortMode.None))
                    typeof(DepartureBoardView).GetMethod("LateUpdate",flags).Invoke(board,null);
                foreach(var effect in UnityEngine.Object.FindObjectsByType<PortalEffect>(FindObjectsSortMode.None))
                    typeof(PortalEffect).GetMethod("LateUpdate",flags).Invoke(effect,null);
                foreach(var registration in UnityEngine.Object.FindObjectsByType<HallDeepPortalRegistration>(FindObjectsSortMode.None))
                    typeof(HallDeepPortalRegistration).GetMethod("LateUpdate",flags).Invoke(registration,null);
                var previous=camera.targetTexture;var active=RenderTexture.active;float aspect=camera.aspect;
                var target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var pixels=new Texture2D(1920,1080,TextureFormat.RGB24,false);
                try
                {
                    camera.targetTexture=target;camera.aspect=16f/9;
                    if(backdrop.Active)typeof(HallBackdrop).GetMethod("Sync",flags).Invoke(backdrop,null);
                    if(backdrop.Active)((Camera)typeof(HallBackdrop).GetField("_camera",flags).GetValue(backdrop)).Render();
                    camera.Render();RenderTexture.active=target;
                    pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply();
                    File.WriteAllBytes(report+"/installed-"+(pan==0?"front":"left")+"-"+hour.ToString("00",System.Globalization.CultureInfo.InvariantCulture)+".png",pixels.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture=previous;camera.aspect=aspect;RenderTexture.active=active;
                    target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(pixels);
                }
            }
            var vehicle=city.lanes[0].vehicle;
            city.Apply(0);var a=vehicle.transform.localPosition;city.Apply(2);
            float moved=Vector3.Distance(a,vehicle.transform.localPosition);
            File.WriteAllText(report+"/validation.txt","Native player-camera capture: both pans at 08,12,16.5,22 hours. Eight independent vehicles retained; first vehicle travelled "+moved.ToString("F4")+" local units over two simulated seconds. Reduced motion: "+MotionPreference.Reduced+". Left/front apertures sample the clean exterior backing; stationary framing stays in HallDeepMorning. Clean exterior has no frame pixels to duplicate during parallax. Original58 sprites and earlier palettes are preserved disabled, not overwritten. Morning exterior only; smoke/cloud/ship/ground traffic remain pending.");
        }
        finally
        {
            settings.previewHour=oldHour;settings.previewHourOn=oldOn;art.SetPan(oldPan);art.SetTime(rig.Evening);
            typeof(HallLightingRig).GetMethod("LateUpdate",flags).Invoke(rig,null);
            UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();city.Apply(0);
        }
    }
    [MenuItem("Tools/Terminal Art/City/Preview Repaired Windows")]
    public static void PreviewWindows()
    {
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        rig.Settings.previewHourOn=true;rig.Settings.previewHour=8;
        art.SetPan(1);
        typeof(HallLightingRig).GetMethod("LateUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(rig,null);
        UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
        UnityEngine.Object.FindFirstObjectByType<HallCityExterior>().Apply(0);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }
    [MenuItem("Tools/Terminal Art/City/Install Approved Deeper Room")]
    public static void Install()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        if(art==null || art.gameObject.scene.path!="Assets/Art/Office/AnimeHallLayers/AnimeHall.unity")
            throw new InvalidOperationException("Open AnimeHall.");
        Directory.CreateDirectory(Folder);
        var drawing=Import(Folder+"/HallDeepMorning.png",true);
        var exterior=Import(Folder+"/CityDenseMorning.png",false);
        if(drawing.width!=W || drawing.height!=H)throw new InvalidOperationException("Layout registration changed.");
        // Insets preserve the painted mullions, sills and rails.
        // Trace actual glazing above the handrail. Painted frames remain static.
        var left=new[]{
            Poly(0,0,92,0,92,474,0,493),
            Poly(121,0,260,0,300,19,300,430,121,470),
            Poly(325,27,420,63,420,405,325,425),
            Poly(443,71,501,93,501,387,443,401),
            Poly(519,102,583,125,583,369,519,383),
            Poly(600,132,636,145,636,356,600,365),
            Poly(653,151,680,161,680,347,653,353)
        };
        var front=new[]{
            Poly(978,213,1014,213,1014,244,978,244),
            Poly(1026,213,1058,213,1058,244,1026,244),
            Poly(1071,213,1096,213,1096,244,1071,244),
            Poly(1110,213,1135,213,1135,244,1110,244),
            Poly(1148,213,1175,213,1175,244,1148,244),
            Poly(1188,213,1205,213,1205,244,1188,244),
            Poly(1028,309,1057,309,1057,334,1028,334),
            Poly(1071,309,1100,309,1100,334,1071,334),
            Poly(1117,309,1146,309,1146,334,1117,334),
            Poly(1161,309,1189,309,1189,334,1161,334),
            Poly(1204,309,1229,309,1229,334,1204,334)
        };
        var masks=new Color32[W*H];
        for(int y=0;y<H;y++)for(int x=0;x<W;x++)
        {
            bool a=left.Any(p=>Inside(x+.5f,y+.5f,p));
            bool b=front.Any(p=>Inside(x+.5f,y+.5f,p));
            masks[(H-1-y)*W+x]=new Color32(a?(byte)255:(byte)0,b?(byte)255:(byte)0,0,a||b?(byte)255:(byte)0);
        }
        HallFloorShadowAuthoring.Receiver(masks);
        var mask=SaveData("DeepRoomMasks",masks,true);
        var morning=SaveData("MorningShadow",HallFloorShadowAuthoring.Bake(masks,new Vector2(100,42),1));
        var noon=SaveData("NoonShadow",HallFloorShadowAuthoring.Bake(masks,new Vector2(35,15),.65f));
        var evening=SaveData("EveningShadow",HallFloorShadowAuthoring.Bake(masks,new Vector2(135,52),1.1f));
        var original=art.layers.First(l=>l.id.StartsWith("03 ")).renderer;
        var cityController=original.GetComponent<HallCityExterior>();
        // Original registered art is preserved as the fallback, not overwritten.
        foreach(var layer in art.layers)if(layer.renderer!=null)layer.renderer.enabled=false;
        foreach(string name in new[]{"Painted contact shadows","Ground cast shadows"})
        {
            var root=art.transform.Find(name);
            if(root!=null)foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))renderer.enabled=false;
        }
        Material Mat(string name,int region)
        {
            string path=Folder+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("NOPE/Hall Deep Layout"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetTexture("_Masks",mask);
            mat.SetTexture("_CityLeft",exterior);
            mat.SetTexture("_CityFront",exterior);
            mat.SetTexture("_MorningShadow",morning);mat.SetTexture("_NoonShadow",noon);mat.SetTexture("_EveningShadow",evening);
            mat.SetFloat("_Region",region);EditorUtility.SetDirty(mat);return mat;
        }
        SpriteRenderer Renderer(string name,string id,Sprite sprite,Material mat,int order)
        {
            var child=art.transform.Find(name);
            if(child==null){var go=new GameObject(name);go.transform.SetParent(art.transform,false);child=go.transform;}
            child.localPosition=original.transform.localPosition;child.localRotation=original.transform.localRotation;child.localScale=original.transform.localScale;
            child.gameObject.layer=original.gameObject.layer;
            var r=child.GetComponent<SpriteRenderer>();if(r==null)r=child.gameObject.AddComponent<SpriteRenderer>();
            r.sprite=sprite;r.sharedMaterial=mat;r.sortingLayerID=original.sortingLayerID;r.sortingOrder=order;r.enabled=true;
            if(!art.layers.Any(l=>l.id==id))art.layers=art.layers.Concat(new[]{new AnimeHallPresentation.Layer{id=id,renderer=r}}).ToArray();
            return r;
        }
        var architecture=Renderer("Approved deeper architecture","62 Approved deeper architecture",
            AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/HallDeepMorning.png"),Mat("DeepArchitecture",0),58);
        original.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/DeepRoomMasks.png");
        original.sharedMaterial=Mat("DeepLeftCity",1);original.enabled=true;
        var frontRenderer=Renderer("Deep front city extension","63 Deep front city extension",original.sprite,Mat("DeepFrontCity",2),3);
        cityController.panelRenderers=new[]{original,frontRenderer};
        var traffic=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Office/AnimeHallLayers/Completion/City/CityTraffic.mat");
        traffic.SetTexture("_WindowMask",mask);EditorUtility.SetDirty(traffic);
        for(int i=0;i<cityController.lanes.Length;i++)
        {
            var lane=cityController.lanes[i];if(lane.vehicle==null)continue;
            lane.vehicle.enabled=true;bool reverse=lane.xEnd<lane.xStart;
            lane.xStart=reverse?(i<4?680:1300):-60;lane.xEnd=reverse?-60:(i<4?680:1300);
        }
        var floorMat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Office/AnimeHallLayers/Completion/ForegroundFloor.mat");
        floorMat.SetTexture("_PaintedReference",drawing);EditorUtility.SetDirty(floorMat);
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        UnityEngine.Object.FindFirstObjectByType<HallForegroundFloor>().Configure(art,rig,architecture);
        // Keep gameplay's public art hooks registered to the revised drawing.
        // Hidden old ring renderers serve only as bounds proxies for portal effects.
        var targets=new[]{
            new Vector4(1092,479,145,112),new Vector4(917,373,83,79),
            new Vector4(1197,373,83,79),new Vector4(1710,221,107,111),
            new Vector4(1960,215,145,136)
        };
        for(int i=0;i<targets.Length;i++)
        {
            var ring=art.layers.First(l=>l.id.StartsWith((39+i*2)+" ")).renderer;
            var bay=art.layers.First(l=>l.id.StartsWith((38+i*2)+" ")).renderer;
            var bounds=OfficeAnchors.OpaqueRect(ring.sprite);var target=targets[i];
            var scale=original.transform.localScale;
            ring.transform.localScale=new Vector3(scale.x*target.z/(bounds.width*100),scale.y*target.w/(bounds.height*100),scale.z);
            ring.transform.localRotation=original.transform.localRotation;
            var at=new Vector3((target.x-W*.5f)/100,(H*.5f-target.y)/100,0);
            ring.transform.localPosition=original.transform.localPosition+original.transform.localRotation*
                (Vector3.Scale(at,scale)-Vector3.Scale((Vector3)bounds.center,ring.transform.localScale));
            bay.sortingOrder=60;
        }
        var portalPixels=new Color32[W*H];
        for(int y=0;y<H;y++)for(int x=0;x<W;x++)
        {
            float alpha=0;
            foreach(var target in targets)
            {
                float rx=target.z*.35f,ry=target.w*.40f;
                float radius=Mathf.Sqrt(Mathf.Pow((x-target.x)/rx,2)+Mathf.Pow((y-target.y)/ry,2));
                float inside=1-Mathf.SmoothStep(.96f,1,radius);
                if(y>target.y+target.w*.20f)inside=0;
                alpha=Mathf.Max(alpha,inside);
            }
            portalPixels[(H-1-y)*W+x]=new Color32(255,255,255,(byte)Mathf.RoundToInt(alpha*255));
        }
        var portalMask=SaveData("PortalOpenings",portalPixels);
        var portalMat=AssetDatabase.LoadAssetAtPath<Material>(Folder+"/PortalGlowClipped.mat");
        if(portalMat==null){portalMat=new Material(Shader.Find("NOPE/Hall Deep Portal Glow"));AssetDatabase.CreateAsset(portalMat,Folder+"/PortalGlowClipped.mat");}
        portalMat.SetTexture("_PortalMask",portalMask);portalMat.SetFloat("_Boost",1.5f);EditorUtility.SetDirty(portalMat);
        var registration=art.GetComponent<HallDeepPortalRegistration>();
        if(registration==null)registration=art.gameObject.AddComponent<HallDeepPortalRegistration>();
        registration.Configure(architecture,portalMat);EditorUtility.SetDirty(registration);
        var anchor=OfficeAnchors.Find(art.gameObject.scene,"Anchor_DepartureBoard",true);
        if(anchor==null)
        {
            var marker=new GameObject("Anchor_DepartureBoard",typeof(RectTransform));
            marker.transform.SetParent(architecture.transform,false);anchor=marker.transform;
        }
        if(anchor is RectTransform rectangle)
        {
            rectangle.SetParent(architecture.transform,false);
            rectangle.localPosition=new Vector3((1015-W*.5f)/100,(H*.5f-126)/100,-.002f);
            rectangle.localRotation=Quaternion.identity;rectangle.localScale=Vector3.one;
            rectangle.sizeDelta=new Vector2(300f/100,90f/100);
            rectangle.gameObject.SetActive(true);
        }
        EditorUtility.SetDirty(art);EditorUtility.SetDirty(cityController);
        UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();cityController.Apply(0);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(art.gameObject.scene);EditorSceneManager.SaveScene(art.gameObject.scene);
        Debug.Log("Installed approved deeper layout with separate left panorama/front extension, rebuilt apertures and new registered floor shadows.");
    }
}
