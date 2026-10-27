using UnityEditor;
using UnityEngine;

/// <summary>
/// The one writer of import settings for the office's 3D art (BUG-63; FIX_PLAN
/// §0 rule 2): every texture under Assets/Art/Office/ imports with mipmaps,
/// trilinear and the high-quality compression (BC7 on PC), so a paper's,
/// a machine's decal's or the brass drawer's edges never alias or crawl
/// when minified and moving. It keeps each texture's type (a Sprite stays a
/// Sprite: switching it would orphan every reference to its sprite) and every
/// other setting. The art side's hall layers
/// (Assets/Art/Office/AnimeHallLayers/) are never touched: the importer
/// returns before reading them (ArtSlots.OfficeImports), so no .meta of
/// theirs ever changes. A track that needs another setting for its texture
/// asks DOC-LOOK for a rule here; nobody hand-edits a .meta under the folder.
/// </summary>
public sealed class OfficeArtImporter : AssetPostprocessor
{
    /// <summary>Its rules' version: a new one reimports the office's art once.</summary>
    public override uint GetVersion() => 1;

    private void OnPreprocessTexture()
    {
        if (!ArtSlots.OfficeImports(assetPath))
            return;
        var importer = (TextureImporter)assetImporter;
        if (importer.textureType == TextureImporterType.NormalMap || importer.textureType == TextureImporterType.Lightmap
            || importer.textureType == TextureImporterType.DirectionalLightmap || importer.textureType == TextureImporterType.Shadowmask
            || importer.textureType == TextureImporterType.Cursor)
            return;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
    }
}
