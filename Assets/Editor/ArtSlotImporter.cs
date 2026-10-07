using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports every texture under Assets/Art/UI/Resources/ (the by-name art
/// slots, ArtSlots; the wheel icons too) on every import (authoritative): a
/// Single sprite (SlotArt loads sprites), transparent, clamped, at most 2048
/// px (4096 for the hall's slots, whose paintings span the hall's 2172 px
/// canvas: ArtSlots.MaxSide); the desk papers' art (ArtSlots.OnDeskPaper: the faces, the photo
/// frame, the ink marks) with mipmaps, UI art without, never sliced (the
/// sliced faces are the UI kit's since run 7: UiKitImporter), so a
/// delivered file is ready without touching its import settings.
/// </summary>
public sealed class ArtSlotImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        string slot = ArtSlots.SlotOf(assetPath);
        if (slot == null)
            return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = ArtSlots.OnDeskPaper(slot);
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = ArtSlots.MaxSide(slot);

        importer.spriteBorder = Vector4.zero;
    }
}
