using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports every texture under Assets/Art/UI/Resources/ (the by-name art
/// slots, ArtSlots; the wheel icons too) on every import (authoritative): a
/// Single sprite (SlotArt loads sprites), transparent, clamped, at most 2048
/// px (4096 for the hall's slots, whose paintings span the hall's 2172 px
/// canvas: ArtSlots.MaxSide; those with the high-quality compression, so their
/// dark fields never mottle over the uncompressed painting); the desk papers' art (ArtSlots.OnDeskPaper: the faces, the photo
/// frame, the ink marks) with mipmaps, UI art without, never sliced (the
/// sliced faces are the UI kit's since run 7: UiKitImporter), always a 2D
/// texture (a delivered file whose .meta holds only its fixed GUID, like the
/// pet's coats, otherwise imports as a cube map), so a delivered file is
/// ready without touching its import settings.
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
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = ArtSlots.OnDeskPaper(slot);
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = ArtSlots.MaxSide(slot);
        // The hall's slots lie pixel for pixel over the uncompressed painting: the default block compression mottles
        // their large dark fields in play, so they take the high-quality compression (BC7 on PC).
        if (slot.StartsWith(ArtSlots.HallSlotsFolder, System.StringComparison.Ordinal))
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

        importer.spriteBorder = Vector4.zero;
    }
}
