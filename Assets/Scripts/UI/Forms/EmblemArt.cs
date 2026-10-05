using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The nations' passport emblems as pictures (the travel documents spec,
/// TD3, TD5): the art's emblem when delivered (ArtSlots.Emblem), else the
/// code-drawn stand-in (EmblemShapes) painted once into a white texture whose
/// alpha is the ink. Either is tinted by the cover's colour, so the art should
/// be white (or one light ink) on clear. Both the desk paper (DeskDocument)
/// and the PC (FormView) draw from here, as SealArt for the seals.
/// </summary>
public static class EmblemArt
{
    /// <summary>A painted emblem's side in pixels.</summary>
    private const int Size = 128;

    /// <summary>Samples per pixel along each axis (the edges are antialiased).</summary>
    private const int Samples = 3;

    private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();
    private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

    /// <summary>The emblem's texture: the art's when delivered, else the stand-in painted on first use; null for an unknown emblem without art.</summary>
    public static Texture2D Texture(string emblem)
    {
        Texture2D art = SlotArt.Texture(new[] { ArtSlots.Emblem(emblem) });
        if (art != null)
            return art;
        if (!EmblemShapes.Has(emblem))
            return null;
        string key = emblem.ToLowerInvariant();
        if (Textures.TryGetValue(key, out Texture2D texture) && texture != null)
            return texture;
        texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = "Emblem_" + emblem, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        var pixels = new Color32[Size * Size];
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                int inked = 0;
                for (int sy = 0; sy < Samples; sy++)
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        float x = ((px + (sx + 0.5f) / Samples) / Size) * 2f - 1f;
                        float y = ((py + (sy + 0.5f) / Samples) / Size) * 2f - 1f;
                        if (EmblemShapes.Inked(emblem, x, y))
                            inked++;
                    }
                // Textures run bottom-up; the shapes are drawn with y down.
                pixels[(Size - 1 - py) * Size + px] = new Color32(255, 255, 255, (byte)(255 * inked / (Samples * Samples)));
            }
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        Textures[key] = texture;
        return texture;
    }

    /// <summary>The emblem as a UI sprite (the PC's), else null.</summary>
    public static Sprite Sprite(string emblem)
    {
        Sprite art = SlotArt.Sprite(ArtSlots.Emblem(emblem));
        if (art != null)
            return art;
        if (!EmblemShapes.Has(emblem))
            return null;
        string key = emblem.ToLowerInvariant();
        if (Sprites.TryGetValue(key, out Sprite sprite) && sprite != null)
            return sprite;
        Texture2D texture = Texture(emblem);
        sprite = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.height);
        sprite.name = texture.name;
        Sprites[key] = sprite;
        return sprite;
    }

    /// <summary>A cover's colour ("#RRGGBB") as a Unity colour; <paramref name="fallback"/> for a blank or unreadable one.</summary>
    public static Color Ink(string cover, Color fallback) => ColorUtility.TryParseHtmlString(cover, out Color c) ? c : fallback;

    /// <summary>A watermark's ink: its mark's colour at this alpha over the page.</summary>
    public const float WatermarkAlpha = 0.09f;
}
