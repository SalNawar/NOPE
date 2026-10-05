using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Bakes source-aligned light/occlusion/emission maps without regenerating or flattening the painting.</summary>
public static class HallFourStateBaker
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/FourState";
    const string Report="ArtDeliverables/TimeDesk/HallLayers/Completion/FourState";
    static readonly string[] Names={"Morning","Noon","Evening","Night"};
    [MenuItem("Tools/Terminal Art/Lighting/Bake Four States")]
    public static void Bake()
    {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop preview before baking.");
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        if(art==null || rig==null || art.gameObject.scene.path!="Assets/Art/Office/AnimeHallLayers/AnimeHall.unity")
            throw new InvalidOperationException("Open AnimeHall first.");
        Directory.CreateDirectory(Folder);Directory.CreateDirectory(Report);
        var reference=art.layers.First(l=>l.renderer!=null).renderer;
        var sprite=reference.sprite;int width=(int)sprite.rect.width,height=(int)sprite.rect.height;
        Vector2 UV(Vector3 world)
        {
            var local=reference.transform.InverseTransformPoint(world);
            return new Vector2((local.x*sprite.pixelsPerUnit+sprite.pivot.x)/width,(local.y*sprite.pixelsPerUnit+sprite.pivot.y)/height);
        }
        Color32[] Mask(string prefix)
        {
            var layer=art.layers.First(l=>l.id.StartsWith(prefix)).renderer;
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
            texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(layer.sprite)));
            var data=texture.GetPixels32();UnityEngine.Object.DestroyImmediate(texture);return data;
        }
        var ground=Mask("06 ");var platform=Mask("12 ");var gallery=Mask("07 ");var sky=Mask("03 ");var fixtures=Mask("51 ");
        var lamps=UnityEngine.Object.FindObjectsByType<HallLight>(FindObjectsSortMode.None)
            .Where(l=>l.kind==HallLightKind.Fixture||l.kind==HallLightKind.Screen||l.kind==HallLightKind.Sign).ToArray();
                var paintingTexture=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
        paintingTexture.LoadImage(File.ReadAllBytes("Assets/Art/Office/AnimeHallLayers/Completion/PaintedReference.png"));
        var painting=paintingTexture.GetPixels32();UnityEngine.Object.DestroyImmediate(paintingTexture);
        var points=new Vector4[lamps.Length];var colors=new Color[lamps.Length];var isFixture=new bool[lamps.Length];
        string sources="Four state native 2D art bake: registered "+width+"x"+height+" light and emission maps.\n";
        for(int i=0;i<lamps.Length;i++)
        {
            var uv=UV(lamps[i].transform.position);bool fixture=lamps[i].kind==HallLightKind.Fixture;isFixture[i]=fixture;
            bool mounted=lamps[i].GetComponent<HallMountedFixture>()!=null;
            points[i]=new Vector4(uv.x,uv.y,mounted?.045f:fixture?.075f:.035f,mounted?.20f:fixture?.17f:.08f);
            var light=lamps[i].GetComponent<Light2D>();
            colors[i]=fixture?new Color(1,.79f,.54f):light!=null?light.color:new Color(.45f,.75f,1);
            sources+=$"{lamps[i].name}: {lamps[i].kind}, UV {uv}, colour {colors[i]}\n";
        }
        var shadow=art.transform.Find("Ground cast shadows");
        var feet=new Vector4[64];var ends=new Vector2[64];int count=0;
        if(shadow!=null)
        {
            var mesh=shadow.GetComponent<MeshFilter>().sharedMesh;var vertices=mesh.vertices;var shares=mesh.colors;
            count=Math.Min(vertices.Length/4,64);
            for(int i=0;i<count;i++)
            {
                var a=UV(shadow.TransformPoint(vertices[i*4]));var b=UV(shadow.TransformPoint(vertices[i*4+1]));var c=UV(shadow.TransformPoint(vertices[i*4+3]));
                feet[i]=new Vector4((a.x+b.x)*.5f,a.y,b.x-a.x,shares[i*4].a);ends[i]=c-a;
            }
        }
        Vector3[] ambient={new Vector3(.76f,.70f,.62f),new Vector3(.86f,.87f,.88f),new Vector3(.62f,.43f,.37f),new Vector3(.19f,.24f,.39f)};
        Vector3[] sunColor={new Vector3(1,.82f,.58f),new Vector3(1,.98f,.91f),new Vector3(1,.60f,.30f),Vector3.zero};
        Vector3[] skyColor={new Vector3(1,.81f,.67f),Vector3.one,new Vector3(.85f,.47f,.32f),new Vector3(.12f,.17f,.33f)};
        float[] daylight={.32f,.24f,.35f,0},strength={.32f,.24f,.36f,0},length={1.35f,.58f,1.65f,1},fixtureLevels={.18f,.12f,.65f,1};
        int pixels=width*height;
        var receivers=new float[pixels];var sunlight=new float[pixels];
        for(int y=0;y<height;y++)for(int x=0;x<width;x++)
        {
            int p=y*width+x;float u=(x+.5f)/width,v=(y+.5f)/height;
            receivers[p]=Mathf.Max(ground[p].a,Mathf.Max(platform[p].a,gallery[p].a))/255f;
            float distance=(u-(.20f+(1-v)*.25f))/.39f;
            sunlight[p]=receivers[p]*Mathf.Exp(-distance*distance);
        }
        for(int state=0;state<4;state++)
        {
            var shadows=new float[pixels];var illumination=new Vector3[pixels];var emission=new Vector3[pixels];
            // Rasterize each registered parallelogram only inside its receiver bounds.
            if(strength[state]>0)for(int n=0;n<count;n++)
            {
                var foot=feet[n];
                // The rear bench has been removed; its registered caster must
                // disappear from every baked time state as well.
                var rearBench=art.layers.FirstOrDefault(l=>l.id.StartsWith("27 "))?.renderer;
                if(rearBench!=null && !rearBench.gameObject.activeInHierarchy &&
                    Mathf.Abs(foot.x*width-999)<2 && Mathf.Abs((1-foot.y)*height-361)<2)continue;
                var delta=ends[n]*length[state];if(delta.y>=0)continue;
                int y0=Mathf.Max(0,Mathf.FloorToInt((foot.y+delta.y)*height)),y1=Mathf.Min(height-1,Mathf.CeilToInt(foot.y*height));
                for(int y=y0;y<=y1;y++)
                {
                    float t=((y+.5f)/height-foot.y)/delta.y;if(t<0||t>1)continue;
                    float center=foot.x+t*delta.x,half=foot.z*.5f,feather=.003f;
                    int x0=Mathf.Max(0,Mathf.FloorToInt((center-half-feather)*width)),x1=Mathf.Min(width-1,Mathf.CeilToInt((center+half+feather)*width));
                    for(int x=x0;x<=x1;x++)
                    {
                        int p=y*width+x;float distance=Mathf.Abs((x+.5f)/width-center);
                        float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(half,half+feather,distance));
                        float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(0,.06f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.8f,1,t)));
                        shadows[p]=Mathf.Max(shadows[p],edge*fade*foot.w*receivers[p]*strength[state]);
                    }
                }
            }
            for(int p=0;p<pixels;p++)
            {
                illumination[p]=(Vector3.Lerp(ambient[state],skyColor[state],sky[p].a/255f)+sunColor[state]*sunlight[p]*daylight[state])*(1-shadows[p]);
                emission[p]=Vector3.zero;
            }
            void Pool(Vector4 shape,Vector3 colour,float level,bool floor,bool fixture)
            {
                int x0=Mathf.Max(0,Mathf.FloorToInt((shape.x-shape.z)*width)),x1=Mathf.Min(width-1,Mathf.CeilToInt((shape.x+shape.z)*width));
                int y0=Mathf.Max(0,Mathf.FloorToInt((shape.y-shape.w)*height)),y1=Mathf.Min(height-1,Mathf.CeilToInt((shape.y+shape.w)*height));
                for(int y=y0;y<=y1;y++)for(int x=x0;x<=x1;x++)
                {
                    int p=y*width+x;float dx=((x+.5f)/width-shape.x)/shape.z,dy=((y+.5f)/height-shape.y)/shape.w;
                    float radius=dx*dx+dy*dy;if(radius>1)continue;
                    float value=Mathf.Exp(-radius*3)*(1-radius)*(1-radius)*level*(floor?receivers[p]:1-sky[p].a/255f);
                    illumination[p]+=colour*value*(floor?.35f:.30f);
                                        if(!floor)
                    {
                        float luminance=(painting[p].r*.2126f+painting[p].g*.7152f+painting[p].b*.0722f)/255f;
                        float emitter=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.72f,.94f,luminance));
                        emission[p]+=colour*value*(fixture?emitter*.25f:.013f);
                    }
                }
            }
            for(int l=0;l<lamps.Length;l++)
            {
                var color=new Vector3(colors[l].r,colors[l].g,colors[l].b);
                float level=isFixture[l]?fixtureLevels[state]:1;
                if(lamps[l].GetComponent<HallMountedFixture>()!=null)level*=2;
                Pool(points[l],color,level,false,isFixture[l]);
                if(isFixture[l]){var mounted=lamps[l].GetComponent<HallMountedFixture>();Pool(mounted!=null?mounted.floorPool:new Vector4(points[l].x,.2f+(.95f-points[l].y)*1.45f,.11f,.17f),color,level,true,true);}
            }
            byte Byte(float value)=>(byte)Mathf.RoundToInt(Mathf.Clamp01(value)*255);
            for(int pass=0;pass<2;pass++)
            {
                var data=new Color32[pixels];
                for(int p=0;p<pixels;p++)
                {
                    var c=pass==0?illumination[p]:emission[p];
                    data[p]=new Color32(Byte(c.x),Byte(c.y),Byte(c.z),pass==0?Byte(sunlight[p]*daylight[state]):(byte)255);
                }
                var image=new Texture2D(width,height,TextureFormat.RGBA32,false,true);
                string path=Folder+"/"+Names[state]+(pass==0?"Light":"Glow")+".png";
                try {image.SetPixels32(data);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
                finally {UnityEngine.Object.DestroyImmediate(image);}
                AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.alphaSource=TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency=false;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
                importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;importer.SaveAndReimport();
            }
        }
        string materialPath=Folder+"/HallFourState.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("NOPE/Hall Four State"));AssetDatabase.CreateAsset(material,materialPath);}
        for(int i=0;i<4;i++)
        {
            material.SetTexture("_"+Names[i],AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+Names[i]+"Light.png"));
            material.SetTexture("_"+Names[i]+"Glow",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+Names[i]+"Glow.png"));
        }
        material.SetVector("_StateWeights",new Vector4(0,1,0,0));material.SetFloat("_LightingAmount",1);
        foreach(var layer in art.layers)
            if(layer.renderer!=null && layer.renderer.sharedMaterial.shader.name!="NOPE/Hall City Exterior" && layer.renderer.sharedMaterial.shader.name!="NOPE/Hall Waiting Bay Repair")layer.renderer.sharedMaterial=material;
        if(shadow!=null)shadow.gameObject.SetActive(false); // Casts are now in the maps, never double-darkened.
        var controller=rig.GetComponent<HallBakedLighting>();if(controller==null)controller=rig.gameObject.AddComponent<HallBakedLighting>();
        controller.Configure(rig,art,material);
        rig.Settings.fixtureColour=new Color(1,.79f,.54f);EditorUtility.SetDirty(rig.Settings);
        foreach(var light in UnityEngine.Object.FindObjectsByType<HallLight>(FindObjectsSortMode.None))
        {
            var actual=light.GetComponent<Light>();
            if(actual==null)continue;
            if(light.kind==HallLightKind.DeskLamp)actual.color=new Color(1,.79f,.54f);
            if(light.kind==HallLightKind.DeskScreen)actual.color=new Color(.45f,.72f,1);
        }
        EditorUtility.SetDirty(material);EditorSceneManager.MarkSceneDirty(art.gameObject.scene);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(art.gameObject.scene);
        File.WriteAllText(Report+"/bake-sources.txt",sources+"Ground shadow shapes: "+count+"\n58 independent painted layers retained.\n");
        Debug.Log("Baked and installed four registered lighting states; source art and gameplay layers preserved.");
    }

    [MenuItem("Tools/Terminal Art/Lighting/Capture Four States")]
    public static void Capture()
    {
        Directory.CreateDirectory(Report);
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        float old=rig.Settings.previewHour;bool wasPreview=rig.Settings.previewHourOn;
        float[] hours={8,12,16.5f,22,10,18,0};
        int step=0;bool prepared=false;double next=0;
        void Tick()
        {
            if(EditorApplication.timeSinceStartup<next)return;
            if(!prepared)
            {
                rig.Settings.previewHourOn=true;rig.Settings.previewHour=hours[step];
                art.SetTime(rig.Evening);rig.GetComponent<HallBakedLighting>().Apply();
                prepared=true;next=EditorApplication.timeSinceStartup+.6;
            }
            else
            {
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Report+"/live-"+hours[step].ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png"));
                step++;prepared=false;next=EditorApplication.timeSinceStartup+.6;
                if(step==hours.Length){rig.Settings.previewHour=old;rig.Settings.previewHourOn=wasPreview;art.SetTime(rig.Evening);EditorApplication.update-=Tick;}
            }
        }
        EditorApplication.update+=Tick;
    }
}











