using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports the cel UI kit (Assets/Art/UI/Kit): every PNG there becomes a
/// single, full-rect, mipmapped Sprite (it is drawn smaller than its 2x pixels), and its 9-slice border and pixels
/// per unit come from the kit's <c>kit_manifest.json</c> (written by the kit
/// exporter), so the sprites are ready for Image.Type.Sliced without hand setup.
/// </summary>
public sealed class UiKitImporter : AssetPostprocessor
{
    /// <summary>The folder the kit's sprites and manifest live in.</summary>
    public const string Folder = "Assets/Art/UI/Kit/";

    private const string ManifestPath = Folder + "kit_manifest.json";

    /// <summary>One sprite's entry in the manifest.</summary>
    [Serializable]
    public sealed class Entry
    {
        /// <summary>The sprite's file name without extension.</summary>
        public string name;
        /// <summary>True when the sprite stretches as a 9-slice.</summary>
        public bool sliced;
        /// <summary>The 9-slice border in pixels: left, bottom, right, top.</summary>
        public int[] border;
        /// <summary>Pixels per unit (the kit is exported at twice its design size).</summary>
        public int pixelsPerUnit;
        /// <summary>The sprite's pivot (0..1).</summary>
        public float[] pivot;
        /// <summary>What the piece is for.</summary>
        public string use;
    }

    /// <summary>The manifest file's shape.</summary>
    [Serializable]
    public sealed class Manifest
    {
        /// <summary>Exported pixels per design pixel.</summary>
        public int scale;
        /// <summary>Every sprite in the kit.</summary>
        public List<Entry> sprites = new List<Entry>();
    }

    /// <summary>The manifest read from disk, or null when it is missing or unreadable.</summary>
    public static Manifest Load()
    {
        string path = Path.GetFullPath(ManifestPath);
        return File.Exists(path) ? JsonUtility.FromJson<Manifest>(File.ReadAllText(path)) : null;
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder, StringComparison.Ordinal) || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.textureShape = TextureImporterShape.Texture2D; // a minimal meta defaults the shape to a cube map, which loads no sprite
        importer.spriteImportMode = SpriteImportMode.Single;
        // Mipmapped: the 2x sprites are drawn smaller than their pixels (half at 1080p, a third at 720p), and without mips they shimmer and blur.
        importer.mipmapEnabled = true;
        importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        Entry entry = Load()?.sprites.Find(e => e.name == Path.GetFileNameWithoutExtension(assetPath));
        if (entry == null)
            return;
        importer.spritePixelsPerUnit = entry.pixelsPerUnit > 0 ? entry.pixelsPerUnit : 100;
        if (entry.pivot != null && entry.pivot.Length == 2)
            importer.spritePivot = new Vector2(entry.pivot[0], entry.pivot[1]);
        if (entry.sliced && entry.border != null && entry.border.Length == 4)
            importer.spriteBorder = new Vector4(entry.border[0], entry.border[1], entry.border[2], entry.border[3]);
    }
}
