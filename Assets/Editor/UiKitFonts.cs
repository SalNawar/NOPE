using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Makes the UI kit's TMP font assets (docs/UI_KIT.md) from the kit's OFL
/// fonts in <see cref="Folder"/>: BigShoulders Bold (plates, tabs, headings),
/// GeistMono Bold (the phosphor readouts), UnifrakturMaguntia (the paper's
/// masthead) and Outfit Regular and Bold (bubbles, slips, body lines on kit
/// panels). Each is a static SDF asset beside its font (ASCII and the
/// punctuation baked in, so playing never rewrites it) with the
/// project's LiberationSans SDF as its fallback for anything else. An asset
/// that exists is kept (its GUID is what the scenes reference).
/// </summary>
public static class UiKitFonts
{
    /// <summary>Where the kit's fonts and their assets live.</summary>
    public const string Folder = "Assets/Fonts/Kit/";

    /// <summary>The label face (plates, tabs, headings).</summary>
    public const string Label = Folder + "BigShoulders-Bold SDF.asset";

    /// <summary>The readout face (the phosphor glass).</summary>
    public const string Readout = Folder + "GeistMono-Bold SDF.asset";

    /// <summary>The masthead face (The Temporal Times).</summary>
    public const string Masthead = Folder + "UnifrakturMaguntia-Book SDF.asset";

    /// <summary>The body face on kit surfaces.</summary>
    public const string Body = Folder + "Outfit-Regular SDF.asset";

    /// <summary>The bold body face.</summary>
    public const string BodyBold = Folder + "Outfit-Bold SDF.asset";

    /// <summary>The characters baked into every kit asset: ASCII and the punctuation the English strings use (a culture's labels take the culture's own font; anything else falls back to LiberationSans).</summary>
    private static string Charset()
    {
        var chars = new System.Text.StringBuilder();
        for (int c = 0x20; c < 0x7F; c++)
            chars.Append((char)c);
        chars.Append("–—‘’“”•…‹›€£¥←→↑↓▲▼▶◀✓✗×·°№");
        return chars.ToString();
    }

    /// <summary>Creates the missing kit font assets (Tools &gt; TimeDesk &gt; Build UI Kit Fonts).</summary>
    [MenuItem("Tools/TimeDesk/Build UI Kit Fonts")]
    public static void Build()
    {
        TMP_FontAsset fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        Make("BigShoulders-Bold.ttf", Label, fallback);
        Make("GeistMono-Bold.ttf", Readout, fallback);
        Make("UnifrakturMaguntia-Book.ttf", Masthead, fallback);
        Make("Outfit-Regular.ttf", Body, fallback);
        Make("Outfit-Bold.ttf", BodyBold, fallback);
        AssetDatabase.SaveAssets();
    }

    /// <summary>One static SDF asset from <paramref name="file"/> at <paramref name="assetPath"/>, unless it exists.</summary>
    private static void Make(string file, string assetPath, TMP_FontAsset fallback)
    {
        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath) != null)
            return;
        Font font = AssetDatabase.LoadAssetAtPath<Font>(Folder + file);
        if (font == null)
        {
            Debug.LogError($"[TimeDesk] The kit font {Folder + file} is missing: merge the UI kit (feat/ui-kit-sprites).");
            return;
        }

        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 56, 5, GlyphRenderMode.SDFAA, 512, 512, AtlasPopulationMode.Dynamic, false);
        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        asset.TryAddCharacters(Charset(), out string missing);
        if (!string.IsNullOrEmpty(missing))
            Debug.Log($"[TimeDesk] {asset.name} has no glyph for {missing.Length} kit characters; its fallback draws them.");
        asset.atlasPopulationMode = AtlasPopulationMode.Static;
        if (fallback != null)
            asset.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset> { fallback };

        AssetDatabase.CreateAsset(asset, assetPath);
        asset.material.name = asset.name + " Material";
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (Texture2D atlas in asset.atlasTextures)
        {
            atlas.name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);
        }
        EditorUtility.SetDirty(asset);
        Debug.Log($"[TimeDesk] Created the kit font {assetPath}.");
    }
}
