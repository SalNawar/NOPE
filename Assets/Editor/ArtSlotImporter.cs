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
/// pet's coats, otherwise imports as a cube map), the pet's pictures and frames
/// uncompressed (ArtSlots.IsPetPicture), so a delivered file is ready without
/// touching its import settings. DOC-LOOK (BUG-29, 63): the papers' art and
/// the pet's pictures with mipmaps and trilinear (ArtSlots.Mipmapped,
/// Trilinear), the papers' art in the high-quality compression (BC7 on PC);
/// every other slot bilinear without mipmaps, as before.
/// </summary>
public sealed class ArtSlotImporter : AssetPostprocessor
{
    /// <summary>Its rules' version: a new one reimports the slots' art once (DOC-LOOK: mipmaps and trilinear on the papers and the pets).</summary>
    public override uint GetVersion() => 2;
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
        importer.mipmapEnabled = ArtSlots.Mipmapped(slot);
        importer.filterMode = ArtSlots.Trilinear(slot) ? FilterMode.Trilinear : FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = ArtSlots.MaxSide(slot);
        // The hall's slots lie pixel for pixel over the uncompressed painting: the default block compression mottles
        // their large dark fields in play, so they take the high-quality compression (BC7 on PC).
        if (slot.StartsWith(ArtSlots.HallSlotsFolder, System.StringComparison.Ordinal) || ArtSlots.OnDeskPaper(slot))
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        // The pet's pictures and frames (tools/art/pets, 1024 px): uncompressed, whatever a fresh .meta would default
        // to (a block-compressed fur mottles; ArtSlots.IsPetPicture); mipmapped and trilinear above (BUG-29).
        if (ArtSlots.IsPetPicture(slot))
            importer.textureCompression = TextureImporterCompression.Uncompressed;

        importer.spriteBorder = Vector4.zero;
    }
}
