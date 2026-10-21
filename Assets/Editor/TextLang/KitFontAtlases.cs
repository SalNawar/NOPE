using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// The UI kit's font atlases cover what the game prints (BUG-38a, CODE-83:
/// the kit's SDF assets were static 120-glyph ASCII atlases, so "VISAS
/// D'ENTRÉE" drew its É in LiberationSans): Tools &gt; TimeDesk &gt; Text &gt;
/// Extend Kit Font Atlases adds Latin-1, Latin Extended-A and Greek, and
/// every other character today's strings print (the minus sign, German
/// quotes, arrows, bullets: TextLangAudit.TodaysLatinText), to each
/// kit face (UiKitFonts) in place, keeping each asset (its GUID, the scenes'
/// references) and its static atlas (a play never rewrites it); a glyph a
/// face's font file lacks (the kit's fonts have no Greek) stays its
/// fallback's. <see cref="Missing"/> lists what a face still lacks, for the
/// font audit and its gate.
/// </summary>
public static class KitFontAtlases
{
    /// <summary>The kit's faces (UiKitFonts' assets).</summary>
    public static readonly string[] Faces = { UiKitFonts.Label, UiKitFonts.Readout, UiKitFonts.Masthead, UiKitFonts.Body, UiKitFonts.BodyBold };

    /// <summary>The characters every kit face should carry beyond ASCII: Latin-1 (U+00A0–U+00FF), Latin Extended-A (U+0100–U+017F) and Greek (U+0386–U+03CE, the letters with their accents).</summary>
    public static string Charset()
    {
        var sb = new StringBuilder();
        for (int c = 0xA0; c <= 0x17F; c++)
            sb.Append((char)c);
        for (int c = 0x386; c <= 0x3CE; c++)
            if (c != 0x387 && c != 0x38B && c != 0x38D && c != 0x3A2)
                sb.Append((char)c);
        return sb.ToString();
    }

    /// <summary>The charset plus every character beyond ASCII that <paramref name="todaysText"/> prints, each once.</summary>
    public static string CharsetFor(string todaysText)
    {
        string charset = Charset();
        string extra = new string((todaysText ?? string.Empty).Where(c => c > '~' && !char.IsControl(c) && !char.IsWhiteSpace(c) && charset.IndexOf(c) < 0).Distinct().ToArray());
        return charset + extra;
    }

    /// <summary>Adds the charset and today's strings' characters (<see cref="CharsetFor"/>) to each kit face's static atlas (more atlas pages when one is full), saved in place; logs what each face's font file lacks.</summary>
    [MenuItem("Tools/TimeDesk/Text/Extend Kit Font Atlases")]
    public static void Extend()
    {
        var config = Resources.Load<RunConfigSO>(RunManager.ConfigResourcePath);
        string characters = CharsetFor(config != null && config.contentLibrary != null ? TextLangAudit.TodaysLatinText(config.contentLibrary) : string.Empty);
        foreach (string path in Faces)
        {
            TMP_FontAsset face = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (face == null)
            {
                Debug.LogError($"[TimeDesk] The kit face {path} is missing: run Tools > TimeDesk > Build UI Kit Fonts.");
                continue;
            }
            int before = face.characterTable.Count;
            face.isMultiAtlasTexturesEnabled = true;
            face.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            face.TryAddCharacters(characters, out string lacking);
            face.atlasPopulationMode = AtlasPopulationMode.Static;
            int page = 0;
            foreach (Texture2D atlas in face.atlasTextures)
            {
                if (atlas != null && !AssetDatabase.Contains(atlas))
                {
                    atlas.name = $"{face.name} Atlas {page}";
                    AssetDatabase.AddObjectToAsset(atlas, face);
                }
                page++;
            }
            EditorUtility.SetDirty(face);
            Debug.Log($"[TimeDesk] {face.name}: {before} -> {face.characterTable.Count} characters on {face.atlasTextures.Length} atlas page(s); its font has no glyph for {(lacking ?? string.Empty).Length} (its fallback draws them): {Codes(lacking)}");
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary>The characters of <paramref name="text"/> (letters, digits, punctuation; tags and placeholders aside) a kit face's atlas lacks, distinct, as U+XXXX codes.</summary>
    public static string Missing(TMP_FontAsset face, string text)
    {
        if (face == null)
            return "(no face)";
        var lookup = face.characterLookupTable;
        return Codes(new string((text ?? string.Empty).Where(c => !char.IsControl(c) && !char.IsWhiteSpace(c) && !lookup.ContainsKey(c)).Distinct().ToArray()));
    }

    /// <summary>Characters as U+XXXX codes.</summary>
    private static string Codes(string chars) => string.Join(" ", (chars ?? string.Empty).Select(c => $"U+{(int)c:X4}"));
}
