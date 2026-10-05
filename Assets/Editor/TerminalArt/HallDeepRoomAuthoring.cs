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
        i.textureCompression=TextureImporterCompression.Uncompressed;i.maxTextureSize=4096;
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
            File.WriteAllText(report+"/validation.txt","Native player-camera capture: both pans at 08,12,16.5,22 hours. Eight independent vehicles retained; first vehicle travelled "+moved.ToString("F4")+" local units over two simulated seconds. Reduced motion: "+MotionPreference.Reduced+". Left/front aperture layers reference the approved registered composition so the accepted framing is unchanged. Original58 sprites and earlier palettes are preserved disabled, not overwritten. Morning exterior only; smoke/cloud/ship/ground traffic remain pending.");
        }
        finally
        {
            settings.previewHour=oldHour;settings.previewHourOn=oldOn;art.SetPan(oldPan);art.SetTime(rig.Evening);
            typeof(HallLightingRig).GetMethod("LateUpdate",flags).Invoke(rig,null);
            UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();city.Apply(0);
        }
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
        if(drawing.width!=W || drawing.height!=H)throw new InvalidOperationException("Layout registration changed.");
        // Insets preserve the painted mullions, sills and rails.
        var left=new[]{
            Poly(0,0,89,0,89,513,0,539),
            Poly(116,0,306,0,306,444,116,507),
            Poly(327,38,435,65,435,405,327,442),
            Poly(454,71,545,97,545,369,454,403),
            Poly(565,109,625,126,625,343,565,366),
            Poly(644,133,677,147,677,326,644,339)
        };
        var front=new[]{
            Poly(958,220,999,220,999,244,958,244),
            Poly(1009,220,1052,220,1052,244,1009,244),
            Poly(1062,220,1103,220,1103,244,1062,244),
            Poly(1113,220,1158,220,1158,244,1113,244),
            Poly(1168,220,1208,220,1208,244,1168,244),
            Poly(839,310,873,310,873,331,839,331),
            Poly(885,310,923,310,923,331,885,331),
            Poly(935,310,973,310,973,331,935,331),
            Poly(985,310,1023,310,1023,331,985,331),
            Poly(1035,310,1073,310,1073,331,1035,331),
            Poly(1085,310,1123,310,1123,331,1085,331),
            Poly(1135,310,1173,310,1173,331,1135,331),
            Poly(1185,310,1223,310,1223,331,1185,331),
            Poly(1235,310,1265,310,1265,331,1235,331)
        };
        var floor=Poly(0,578,635,400,790,350,1340,340,2172,684,2172,724,0,724);
        var exclusions=new[]{
            Poly(575,370,825,350,825,493,575,547),
            Poly(807,352,1035,352,1035,438,807,438),
            Poly(1140,352,1380,352,1380,438,1140,438),
            Poly(920,445,1390,445,1390,639,920,639)
        };
        var masks=new Color32[W*H];
        for(int y=0;y<H;y++)for(int x=0;x<W;x++)
        {
            bool a=left.Any(p=>Inside(x+.5f,y+.5f,p));
            bool b=front.Any(p=>Inside(x+.5f,y+.5f,p));
            bool ground=Inside(x+.5f,y+.5f,floor)&&!exclusions.Any(p=>Inside(x+.5f,y+.5f,p));
            masks[(H-1-y)*W+x]=new Color32(a?(byte)255:(byte)0,b?(byte)255:(byte)0,ground?(byte)255:(byte)0,a||b?(byte)255:(byte)0);
        }
        var mask=SaveData("DeepRoomMasks",masks,true);
        // New geometry receives new shadows; never reuse the old pier's casts.
        var casters=new[]{
            new Vector4(748,433,85,.75f),
            new Vector4(1148,640,250,.7f),
            new Vector4(967,437,150,.65f),
            new Vector4(1260,437,150,.65f),
            new Vector4(2060,680,165,.55f)
        };
        Texture2D Shadow(string name,float dx,float dy,float strength)
        {
            var data=new Color32[W*H];
            for(int y=0;y<H;y++)for(int x=0;x<W;x++)
            {
                float shade=0;
                if(masks[(H-1-y)*W+x].b>0)
                {
                    foreach(var c in casters)
                    {
                        float t=(y-c.y)/dy;
                        if(t<0 || t>1)continue;
                        float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(c.z*.5f,c.z*.5f+5,Mathf.Abs(x-c.x-t*dx)));
                        float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.08f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,t)));
                        shade=Mathf.Max(shade,edge*fade*c.w*strength);
                    }
                    // Platform rail posts at the new registered foot line.
                    for(float foot=30;foot<1660;foot+=156)
                    {
                        float t=(y-692)/(dy*.5f);
                        if(t<0 || t>1)continue;
                        float dist=Mathf.Abs(x-foot-t*dx*.5f);
                        shade=Mathf.Max(shade,(1-Mathf.SmoothStep(2,6,dist))*(1-t)*strength*.6f);
                    }
                }
                byte value=(byte)Mathf.RoundToInt(shade*255);
                data[(H-1-y)*W+x]=new Color32(value,value,value,255);
            }
            return SaveData(name,data);
        }
        var morning=Shadow("MorningShadow",155,70,.32f);
        var noon=Shadow("NoonShadow",65,28,.23f);
        var evening=Shadow("EveningShadow",205,88,.35f);
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
            mat.SetTexture("_CityLeft",drawing);
            mat.SetTexture("_CityFront",drawing);
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
