using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports every texture under Assets/Art/Characters/ as character art, on
/// every import (authoritative): a Single, FullRect sprite one unit tall (its
/// pixels per unit is its own height, so any resolution of the 2:3 canvas
/// lays out the same), pivoted at the feet (LookCanvas.FeetPivotY), readable
/// (the photo crop and alpha tests), mipmapped or not by
/// <see cref="Mipmaps"/> (trilinear filtering with mipmaps, bilinear
/// without), high-quality compression (BC7 on Standalone: no block artefacts
/// on the line art), no crunch, at most <see cref="MaxSize"/> px. A source
/// that is not 2:3 is reported. <see cref="ReimportAll"/> writes these
/// settings into every character texture's .meta after a change.
/// </summary>
public sealed class CharacterArtImporter : AssetPostprocessor
{
    /// <summary>The folder whose textures are character art.</summary>
    private const string Folder = "Assets/Art/Characters/";

    /// <summary>
    /// Whether character art has mipmaps. Off by the director's call D11
    /// (BLD-05, night of 2026-11-07; reversible by Saleh): the 1024 x 1536
    /// canvas is not a power of two, and Unity cannot block-compress a
    /// mipmapped texture that is not, so with mipmaps every character
    /// texture shipped as RGBA32 (10.5 MB each, 12 GB of a 13.9 GB player).
    /// Without them it ships as BC7.
    /// </summary>
    public const bool Mipmaps = false;

    /// <summary>
    /// The largest texture side kept: 1020, the director's call D11 (BLD-05,
    /// night of 2026-11-07; reversible by Saleh) of 1024 made compressible.
    /// Unity scales a larger source in proportion, and BC7 needs both sides
    /// in multiples of 4: at 1024 the 1024 x 1536 canvas imports at
    /// 683 x 1024 and stays RGBA32, at 1020 it imports at 680 x 1020 as BC7
    /// (keep it a multiple of 12 for the 2:3 canvas). Without mipmaps a
    /// smaller texture also keeps the outlines cleaner where the office draws
    /// the figure small (its canvas is about 550 px tall at 1080p). D11's
    /// fallback when the outlines alias is 504 (336 x 504).
    /// </summary>
    public const int MaxSize = 1020;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder))
            return;

        var importer = (TextureImporter)assetImporter;
        Apply(importer);

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width * LookCanvas.Height != height * LookCanvas.Width)
            Debug.LogWarning($"[CharacterArtImporter] '{assetPath}' is {width}x{height}; character art must be {LookCanvas.Width}x{LookCanvas.Height} (2:3). See docs/CHARACTER_ART_CONTRACT.md.");

        Vector2Int imported = ImportedSize(width, height);
        if (imported.x % 4 != 0 || imported.y % 4 != 0)
            Debug.LogWarning($"[CharacterArtImporter] '{assetPath}' imports at {imported.x}x{imported.y}: BC7 needs both sides in multiples of 4, so Unity keeps it uncompressed (RGBA32). See MaxSize.");
    }

    /// <summary>The filter the character art samples with: trilinear between the mip levels when it has them, else bilinear (<see cref="Mipmaps"/>).</summary>
    public static FilterMode Filter => Mipmaps ? FilterMode.Trilinear : FilterMode.Bilinear;

    /// <summary>
    /// The size Unity imports a <paramref name="width"/> x <paramref name="height"/>
    /// source at: scaled in proportion so its longer side is at most
    /// <see cref="MaxSize"/>, each side rounded (the 1024 x 1536 canvas: 680 x 1020).
    /// The sprite keeps its registration: its pixels per unit is this height and
    /// its pivot scales with it.
    /// </summary>
    public static Vector2Int ImportedSize(int width, int height)
    {
        float scale = Mathf.Min(1f, MaxSize / (float)Mathf.Max(1, Mathf.Max(width, height)));
        return new Vector2Int(Mathf.RoundToInt(width * scale), Mathf.RoundToInt(height * scale));
    }

    /// <summary>
    /// Whether <paramref name="importer"/> carries the character-art settings that
    /// <see cref="Apply"/> writes, the sprite's registration aside (the imported
    /// sprite shows that; CharacterLibraryValidation checks it).
    /// </summary>
    public static bool HasSettings(TextureImporter importer)
    {
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        return importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single &&
               settings.spriteMeshType == SpriteMeshType.FullRect && importer.isReadable &&
               importer.mipmapEnabled == Mipmaps && importer.filterMode == Filter && importer.alphaIsTransparency &&
               importer.textureCompression == TextureImporterCompression.CompressedHQ && !importer.crunchedCompression &&
               importer.maxTextureSize == MaxSize;
    }

    /// <summary>
    /// Writes the character-art settings into every texture under
    /// Assets/Art/Characters/ and reimports them in one batch (Tools &gt;
    /// TimeDesk &gt; Reimport Character Art). Run it after a change of
    /// <see cref="Mipmaps"/> or <see cref="MaxSize"/>: a change of this
    /// importer's code alone reimports nothing. Commit the .meta files it
    /// changes. It blocks the editor while it runs (measured: see
    /// docs/FEATURES.md's Characters line); a batch job runner that watches the
    /// editor's update loop needs its own heartbeat meanwhile.
    /// </summary>
    [MenuItem("Tools/TimeDesk/Reimport Character Art")]
    public static void ReimportAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Folder.TrimEnd('/') });
        int count = 0;
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (string guid in guids)
            {
                if (!(AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) is TextureImporter importer))
                    continue;
                Apply(importer);
                importer.SaveAndReimport();
                count++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }
        Debug.Log($"[CharacterArtImporter] Reimported {count} character textures (mipmaps {(Mipmaps ? "on" : "off")}, max size {MaxSize}).");
    }

    /// <summary>Sets <paramref name="importer"/> to the character-art settings (see the class).</summary>
    private static void Apply(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.isReadable = true;
        importer.mipmapEnabled = Mipmaps;
        importer.filterMode = Filter;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.crunchedCompression = false;
        importer.maxTextureSize = MaxSize;
        importer.alphaIsTransparency = true;

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, LookCanvas.FeetPivotY);
        settings.spritePixelsPerUnit = Mathf.Max(1, height);
        importer.SetTextureSettings(settings);
    }
}
