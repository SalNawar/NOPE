using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports every texture under Assets/Art/Characters/ as character art, on
/// every import (authoritative): a Single, FullRect sprite one unit tall (its
/// pixels per unit is its own height, so any resolution of the 2:3 canvas
/// lays out the same), pivoted at the feet (LookCanvas.FeetPivotY), readable
/// (the photo crop and alpha tests), no mipmaps, no crunch, at most 2048 px.
/// A source that is not 2:3 is reported.
/// </summary>
public sealed class CharacterArtImporter : AssetPostprocessor
{
    /// <summary>The folder whose textures are character art.</summary>
    private const string Folder = "Assets/Art/Characters/";

    /// <summary>The largest texture side kept (the canvas is 1024 x 1536).</summary>
    private const int MaxSize = 2048;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Folder))
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.isReadable = true;
        importer.mipmapEnabled = false;
        importer.crunchedCompression = false;
        importer.maxTextureSize = MaxSize;
        importer.alphaIsTransparency = true;

        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width * LookCanvas.Height != height * LookCanvas.Width)
            Debug.LogWarning($"[CharacterArtImporter] '{assetPath}' is {width}x{height}; character art must be {LookCanvas.Width}x{LookCanvas.Height} (2:3). See docs/CHARACTER_ART_CONTRACT.md.");

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, LookCanvas.FeetPivotY);
        settings.spritePixelsPerUnit = Mathf.Max(1, height);
        importer.SetTextureSettings(settings);
    }
}
