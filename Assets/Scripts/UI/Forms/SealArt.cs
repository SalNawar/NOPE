using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The seals' pictures (the document design spec, D4): each outline painted
/// once (SealOutlines) into a white texture whose alpha is the ink, which the
/// renderers tint with the seal's ink; the legend is theirs to print over it.
/// Both the desk paper (DeskDocument) and the PC (FormView, the Seal Register)
/// draw from here, so a seal looks the same everywhere. Code-drawn: no seal
/// art is authored (ArtSlots.AgencySeal stays the agency's faint seal).
/// </summary>
public static class SealArt
{
    /// <summary>A seal texture's side in pixels.</summary>
    private const int Size = 128;

    /// <summary>Samples per pixel along each axis (the edges are antialiased).</summary>
    private const int Samples = 3;

    private static readonly Dictionary<SealShape, Texture2D> Textures = new Dictionary<SealShape, Texture2D>();
    private static readonly Dictionary<SealShape, Sprite> Sprites = new Dictionary<SealShape, Sprite>();

    /// <summary>The outline's texture (white, its alpha the ink), painted on first use.</summary>
    public static Texture2D Texture(SealShape shape)
    {
        if (Textures.TryGetValue(shape, out Texture2D texture) && texture != null)
            return texture;
        texture = new Texture2D(Size, Size, TextureFormat.RGBA32, true) { name = "Seal_" + shape, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        var pixels = new Color32[Size * Size];
        string name = shape.ToString();
        for (int py = 0; py < Size; py++)
            for (int px = 0; px < Size; px++)
            {
                int inked = 0;
                for (int sy = 0; sy < Samples; sy++)
                    for (int sx = 0; sx < Samples; sx++)
                    {
                        float x = ((px + (sx + 0.5f) / Samples) / Size) * 2f - 1f;
                        float y = ((py + (sy + 0.5f) / Samples) / Size) * 2f - 1f;
                        if (SealOutlines.Inked(name, x, y))
                            inked++;
                    }
                // Textures run bottom-up; the outlines are drawn with y down.
                pixels[(Size - 1 - py) * Size + px] = new Color32(255, 255, 255, (byte)(255 * inked / (Samples * Samples)));
            }
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        Textures[shape] = texture;
        return texture;
    }

    /// <summary>The outline as a UI sprite (the PC's seals).</summary>
    public static Sprite Sprite(SealShape shape)
    {
        if (Sprites.TryGetValue(shape, out Sprite sprite) && sprite != null)
            return sprite;
        Texture2D texture = Texture(shape);
        sprite = UnityEngine.Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size);
        sprite.name = texture.name;
        Sprites[shape] = sprite;
        return sprite;
    }

    /// <summary>The seal's ink as a colour (Seals.InkHex).</summary>
    public static Color Ink(SealInk ink) => ColorUtility.TryParseHtmlString(Seals.InkHex(ink), out Color c) ? c : Color.black;

    /// <summary>A legend's size inside its seal: this share of the seal's side (two bold capitals fill the hairline's middle, 14 px on a paper held at 720p).</summary>
    public const float LegendShare = 0.42f;
}
