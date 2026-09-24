using UnityEngine;

/// <summary>
/// Turns a sprite into its hover-outline sprite: reads the sprite's pixels
/// through a temporary RenderTexture (so the texture needs no Read/Write),
/// builds the ring with OutlineMask, and wraps it in a Sprite whose pivot keeps
/// it aligned with the source. The caller owns the result and its texture.
/// </summary>
public static class SpriteOutlineBuilder
{
    /// <summary>Builds the outline sprite (white; tint with SpriteRenderer.color). Null if the sprite has no texture.</summary>
    /// <exception cref="System.InvalidOperationException">The sprite is tightly packed or rotated in an atlas.</exception>
    public static Sprite Build(Sprite source, int ringWidthPx)
    {
        if (source == null || source.texture == null)
            return null;

        // Tight or rotated atlas packing has no rectangular source region to read.
        if (source.packed && (source.packingMode == SpritePackingMode.Tight || source.packingRotation != SpritePackingRotation.None))
            throw new System.InvalidOperationException(
                $"Sprite '{source.name}' is tightly packed or rotated in an atlas; hover outlines need Rectangle packing with no rotation for clickable sprites.");

        Rect region = source.textureRect;         // where the sprite sits in its (possibly atlased) texture
        Vector2 trimOffset = source.textureRectOffset;

        int width = Mathf.Max(1, Mathf.RoundToInt(region.width));
        int height = Mathf.Max(1, Mathf.RoundToInt(region.height));
        byte[] alpha = ReadAlpha(source.texture, region, width, height);
        byte[] ring = OutlineMask.BuildRing(alpha, width, height, ringWidthPx, out int outWidth, out int outHeight);

        var pixels = new Color32[ring.Length];
        for (int i = 0; i < ring.Length; i++)
            pixels[i] = new Color32(255, 255, 255, ring[i]);

        var texture = new Texture2D(outWidth, outHeight, TextureFormat.RGBA32, false)
        {
            name = source.name + "_HoverOutline",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = source.texture.filterMode,
        };
        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        Vector2 pivot = source.pivot - trimOffset;
        (float pivotX, float pivotY) = OutlineMask.ShiftPivot(pivot.x, pivot.y, ringWidthPx);
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, outWidth, outHeight),
            new Vector2(pivotX / outWidth, pivotY / outHeight), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = texture.name;
        return sprite;
    }

    /// <summary>Reads the alpha of a texture region via a GPU blit (row 0 = bottom).</summary>
    private static byte[] ReadAlpha(Texture2D texture, Rect region, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
        RenderTexture previous = RenderTexture.active;
        Texture2D readback = null;
        try
        {
            Graphics.Blit(texture, rt);
            RenderTexture.active = rt;
            readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
            readback.ReadPixels(new Rect(region.x, region.y, width, height), 0, 0, false);
            readback.Apply(false);

            Color32[] pixels = readback.GetPixels32();
            var alpha = new byte[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                alpha[i] = pixels[i].a;
            return alpha;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            if (readback != null)
                Object.Destroy(readback);
        }
    }
}
