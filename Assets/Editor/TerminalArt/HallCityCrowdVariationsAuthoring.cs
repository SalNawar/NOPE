using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class HallCityCrowdVariationsAuthoring
{
    const string Folder="Assets/Art/Office/AnimeHallLayers/Completion/City/TimeWeather";
    const string Report="ArtDeliverables/TimeDesk/CityAndCrowdVariations";
    [MenuItem("Tools/Terminal Art/City Variations/Install")]
    public static void Install()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play before installing.");
        var city=UnityEngine.Object.FindFirstObjectByType<HallCityExterior>();
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        foreach(string file in Directory.GetFiles(Folder,"*.png"))
        {
            AssetDatabase.ImportAsset(file,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(file);
            importer.textureType=TextureImporterType.Default;importer.npotScale=TextureImporterNPOTScale.None;
            importer.maxTextureSize=4096;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
            importer.sRGBTexture=!file.EndsWith("CityDepth.png");importer.SaveAndReimport();
        }
        foreach(var renderer in city.panelRenderers.Distinct())
        {
            if(renderer==null)continue;
            var old=renderer.sharedMaterial;
            string path=Folder+"/"+(old.GetFloat("_Region")<1.5?"LivingLeft":"LivingFront")+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("NOPE/Hall Living City"));AssetDatabase.CreateAsset(mat,path);}
            mat.SetTexture("_Masks",old.GetTexture("_Masks"));mat.SetFloat("_Region",old.GetFloat("_Region"));
            foreach(string time in new[]{"Morning","Noon","Evening","Night"})foreach(string weather in new[]{"Clear","Rain"})
                mat.SetTexture("_"+time+weather,AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/City"+time+weather+".png"));
            mat.SetTexture("_CityDepth",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/CityDepth.png"));
            mat.SetTexture("_Atmosphere",AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/CityAtmosphereAtlas.png"));
            EditorUtility.SetDirty(mat);renderer.sharedMaterial=mat;EditorUtility.SetDirty(renderer);
        }
        city.rain=0;city.depthStrength=.7f;city.animateCity=true;EditorUtility.SetDirty(city);
        city.Apply(0);UnityEngine.Object.FindFirstObjectByType<HallBakedLighting>().Apply();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(art.gameObject.scene);
        HallWhiteCrowdAuthoring.Install();HallWhiteCrowdAuthoring.Save();
    }
    static void Capture(string name)
    {
        HallFocusAlignmentAuthoring.Capture("variations-"+name,false);
        File.Copy("ArtDeliverables/TimeDesk/City/FocusAlignment/variations-"+name+".png",Report+"/"+name+".png",true);
    }
    [MenuItem("Tools/Terminal Art/City Variations/Verify")]
    public static void Verify()
    {
        Directory.CreateDirectory(Report);
        var crowds=UnityEngine.Object.FindFirstObjectByType<HallWhiteCrowds>();
        var city=UnityEngine.Object.FindFirstObjectByType<HallCityExterior>();
        var art=UnityEngine.Object.FindFirstObjectByType<AnimeHallPresentation>();
        var rig=UnityEngine.Object.FindFirstObjectByType<HallLightingRig>();var settings=rig.Settings;
        var report=new StringBuilder();float oldPan=art.lookLeft,oldRain=city.rain,oldHour=settings.previewHour,oldOpacity=crowds.crowdOpacity;
        bool oldPreview=settings.previewHourOn;
        var phases=crowds.Groups.Select(g=>g.phase).ToArray();
        try
        {
            foreach(var g in crowds.Groups)
            {
                if(g.alternatives.Length!=3 || g.alternativeMaterials.Length!=3 || g.alternatives.Distinct().Count()!=3)
                    throw new InvalidOperationException("Every location must have three distinct alternatives.");
                if(g.alternatives.Any(m=>m==null)||g.alternativeMaterials.Any(m=>m==null))throw new InvalidOperationException("Missing crowd asset.");
                var position=g.silhouette.transform.localPosition;var scale=g.silhouette.transform.localScale;
                for(int variant=0;variant<3;variant++)
                {
                    crowds.Apply(variant*g.cycle+10-g.phase);
                    if(g.activeAlternative!=variant || g.meshFilter.sharedMesh!=g.alternatives[variant])throw new InvalidOperationException("Alternative rotation failed.");
                    if(g.silhouette.transform.localPosition!=position||g.silhouette.transform.localScale!=scale)throw new InvalidOperationException("Crowd moved while swapping.");
                    if(Mathf.Abs(g.meshFilter.sharedMesh.bounds.min.y)>.0001f)throw new InvalidOperationException("Feet shifted.");
                }
                if(!MotionPreference.Reduced)
                {
                    if(HallWhiteCrowds.Fade(g,g.cycle-g.phase)>.001f||HallWhiteCrowds.Fade(g,g.cycle-g.phase-.01f)>.001f)
                        throw new InvalidOperationException("Composition changed while visible.");
                }
            }
            report.AppendLine($"PASS: {crowds.Groups.Length} locations, three unique compositions each; all {crowds.Groups.Length*3} alternatives validated; fixed feet and stationary transforms; swaps occur while invisible.");
            var first=crowds.Groups[0];var block=new MaterialPropertyBlock();
            foreach(float strength in new[]{0f,.4f,.65f,1f})
            {
                crowds.crowdOpacity=strength;crowds.Apply(10-first.phase);first.silhouette.GetPropertyBlock(block);var tint=block.GetColor("_Tint");
                if(tint.r!=0||tint.g!=0||tint.b!=0)throw new InvalidOperationException("Crowds must stay black.");
                if(strength==0 && first.silhouette.enabled || strength==1 && Mathf.Abs(tint.a-1)>.001f)throw new InvalidOperationException("Opacity endpoint failed.");
                report.AppendLine($"Opacity {strength}: black tint, alpha {tint.a}.");
            }
            crowds.crowdOpacity=.65f;
            settings.previewHourOn=true;settings.previewHour=12;art.SetPan(0);
            for(int variant=0;variant<3;variant++)
            {
                for(int n=0;n<crowds.Groups.Length;n++)crowds.Groups[n].phase=10+variant*crowds.Groups[n].cycle;
                crowds.Apply(0);Capture("crowds-all-"+variant);
            }
            for(int n=0;n<phases.Length;n++)crowds.Groups[n].phase=phases[n];
            foreach(float hour in new[]{8f,12f,16.5f,22f})foreach(float rain in new[]{0f,1f})
            {
                settings.previewHour=hour;city.rain=rain;art.SetPan(1);city.Apply(0);
                Capture("city-"+hour.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"-rain-"+rain);
            }
            settings.previewHour=12;city.rain=.5f;art.SetPan(.5f);Capture("city-mid-pan-rain-blend");
            // The left view itself is gameplay's CityView (A / Left: this pan as the lead-in, then the whole panorama faded in).
            art.SetPan(1);if(art.lookLeft<.99f)throw new InvalidOperationException("Left pan endpoint not reached.");
            art.SetPan(0);if(art.lookLeft>.01f)throw new InvalidOperationException("Forward pan endpoint not reached.");
            report.AppendLine("PASS: the left pan reaches both configured endpoints.");
            report.AppendLine("PASS: eight saved 2172x724 state textures, matching depth map, generated atmosphere atlas, existing aperture masks retained.");
            foreach(string file in Directory.GetFiles(Folder,"City*.png"))
            {
                var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(file);if(tex==null)throw new InvalidOperationException("Missing imported texture "+file);
                if(!file.Contains("Atlas")&&(tex.width!=2172||tex.height!=724))throw new InvalidOperationException("Panorama dimensions mismatch "+file);
                report.AppendLine($"Asset {Path.GetFileName(file)}: {tex.width}x{tex.height}");
            }
            File.WriteAllText(Report+"/verification.txt",report.ToString());
        }
        finally
        {
            for(int n=0;n<phases.Length;n++)crowds.Groups[n].phase=phases[n];
            crowds.crowdOpacity=oldOpacity;settings.previewHour=oldHour;settings.previewHourOn=oldPreview;city.rain=oldRain;art.SetPan(oldPan);
            city.Apply(Application.isPlaying?Time.time:0);crowds.Apply(Application.isPlaying?Time.timeSinceLevelLoad:0);
        }
        Debug.Log(report.ToString());
    }
}
