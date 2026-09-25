using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Writes generated placeholder PNGs into the project (the office builder's
/// placeholders and cursors, Generate World's culture wallpapers) and creates
/// asset folders. Callers apply their own importer settings after Write.
/// </summary>
internal static class PlaceholderPng
{
    /// <summary>Writes pixels from a function (row 0 = bottom) as a PNG at an asset path and imports it.</summary>
    public static void Write(string assetPath, int w, int h, Func<int, int, Color32> pixel)
    {
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = pixel(x, y);
        Write(assetPath, w, h, pixels);
    }

    /// <summary>Writes RGBA32 bytes (row 0 = bottom, e.g. a CulturePlaceholders painter's) as a PNG at an asset path and imports it.</summary>
    public static void Write(string assetPath, int w, int h, byte[] rgba)
    {
        var pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(rgba[i * 4], rgba[i * 4 + 1], rgba[i * 4 + 2], rgba[i * 4 + 3]);
        Write(assetPath, w, h, pixels);
    }

    /// <summary>Creates an asset folder and its missing parents.</summary>
    public static void EnsureFolderTree(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolderTree(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }

    /// <summary>Encodes, writes and imports the pixels.</summary>
    private static void Write(string assetPath, int w, int h, Color32[] pixels)
    {
        EnsureFolderTree(Path.GetDirectoryName(assetPath).Replace('\\', '/'));

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        File.WriteAllBytes(Path.Combine(projectRoot, assetPath), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }
}
