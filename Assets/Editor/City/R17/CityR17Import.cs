using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The r17 city's editor side (CITY-R17).
/// <list type="bullet">
/// <item>Import settings for <c>Assets/Art/City/R17</c>, enforced on every import: no sRGB decode (the city composites
/// in the look-dev's gamma space), alpha is transparency (no dark fringes on the cut-outs), no mipmaps (drawn about
/// 1:1), clamp, bilinear, BC7 on Standalone, max size 4096 (every sheet fits; the import pads to multiples of 4).</item>
/// <item>Tools &gt; TimeDesk &gt; City R17 &gt; Check: the config's data parses, every texture the scene names is in
/// the config, the shaders are set, and the order covers every system; prints what it found.</item>
/// <item>Tools &gt; TimeDesk &gt; City R17 &gt; Rebuild Config Art: refills the config's art list from the folder.</item>
/// </list>
/// </summary>
public sealed class CityR17Import : AssetPostprocessor
{
    private const string ArtFolder = "Assets/Art/City/R17/";
    private const string ConfigPath = "Assets/Resources/CityR17/CityR17Config.asset";

    private void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').StartsWith(ArtFolder))
            return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.textureShape = TextureImporterShape.Texture2D;
        ti.sRGBTexture = false;
        ti.alphaSource = TextureImporterAlphaSource.FromInput;
        ti.alphaIsTransparency = true;
        ti.mipmapEnabled = false;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.filterMode = FilterMode.Bilinear;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.isReadable = false;
        ti.maxTextureSize = 4096;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        TextureImporterPlatformSettings standalone = ti.GetPlatformTextureSettings("Standalone");
        standalone.overridden = true;
        standalone.maxTextureSize = 4096;
        standalone.format = TextureImporterFormat.BC7;
        standalone.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SetPlatformTextureSettings(standalone);
    }

    [MenuItem("Tools/TimeDesk/City R17/Check")]
    public static void CheckMenu()
    {
        List<string> problems = Check();
        if (problems.Count == 0)
            Debug.Log("City R17: config, data, art and shaders check out.");
        else
            Debug.LogError("City R17: " + problems.Count + " problem(s):\n" + string.Join("\n", problems));
    }

    /// <summary>Every problem with the shipped config (empty when it is complete).</summary>
    public static List<string> Check()
    {
        var problems = new List<string>();
        var config = AssetDatabase.LoadAssetAtPath<CityR17ConfigSO>(ConfigPath);
        if (config == null)
        {
            problems.Add("no config at " + ConfigPath);
            return problems;
        }
        if (config.sceneJson == null || config.trafficJson == null || config.vehiclesJson == null || config.artJson == null)
        {
            problems.Add("the config lacks a data file");
            return problems;
        }
        foreach ((Shader s, string name) in new[] { (config.spriteShader, "sprite"), (config.vehicleShader, "vehicle"), (config.lampShader, "lamp"), (config.windowShader, "window"), (config.presentShader, "present") })
            if (s == null || !s.isSupported)
                problems.Add("the " + name + " shader is missing or unsupported");
        CityR17Scene scene;
        CityR17Art art;
        try
        {
            scene = CityR17Scene.Parse(config.sceneJson.text);
            CityR17TrafficData.Parse(config.trafficJson.text);
            CityR17Models.Parse(config.vehiclesJson.text);
            art = CityR17Art.Parse(config.artJson.text);
        }
        catch (System.Exception e)
        {
            problems.Add("the data does not parse: " + e.Message);
            return problems;
        }
        foreach (KeyValuePair<string, CityR17Art.Entry> kv in art.Files)
            if (config.Art(Path.GetFileName(kv.Key)) == null)
                problems.Add("the config has no texture for " + kv.Key);
        foreach (string s in CityR17Order.Unported(scene))
            Debug.Log("City R17: the page's " + s + " is not ported (named, not lost).");
        return problems;
    }

    [MenuItem("Tools/TimeDesk/City R17/Rebuild Config Art")]
    public static void RebuildArt()
    {
        var config = AssetDatabase.LoadAssetAtPath<CityR17ConfigSO>(ConfigPath);
        if (config == null)
        {
            Debug.LogError("City R17: no config at " + ConfigPath);
            return;
        }
        var list = new List<Texture2D>();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder.TrimEnd('/') }))
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(guid));
            if (t != null)
                list.Add(t);
        }
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        config.art = list.ToArray();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        Debug.Log("City R17: the config lists " + list.Count + " textures.");
    }
}
