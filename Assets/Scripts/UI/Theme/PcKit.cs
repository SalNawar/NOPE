using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The cyberpunk PC's kit (D2): a copy of the cel kit (UiKitSO: its fonts,
/// type scale and every sprite name) whose PC pieces are drawn from a
/// PcThemeSO's tokens (PcSkinRules picks the piece for each sprite name,
/// PcChromeArt paints it) or are the theme's neon glyphs, and whose label
/// inks are the theme's ink. Each drawn sprite keeps its cel sprite's name,
/// pad, 9-slice border and pixels per unit, so every control keeps its size and
/// place and the cel look comes back by the same names. A sliced piece is
/// drawn only as large as its border needs (its middle stretches). Built at
/// run time by PcSkin; the textures and sprites made go in the caller's list,
/// which destroys them with the kit.
/// </summary>
public static class PcKit
{
    /// <summary>
    /// The PC kit for <paramref name="theme"/> with the accent
    /// <paramref name="accent"/> (the culture's tint applied): every object it
    /// makes (the kit, its textures, its sprites) is added to
    /// <paramref name="made"/>. Null without a base kit.
    /// </summary>
    public static UiKitSO Build(PcThemeSO theme, Color accent, List<Object> made)
    {
        if (theme == null || theme.baseKit == null)
            return null;
        UiKitSO kit = Object.Instantiate(theme.baseKit);
        kit.name = "UiKit_PC";
        made.Add(kit);
        var textures = new Dictionary<string, Texture2D>();
        var sprites = new List<UiKitSO.KitSprite>(theme.baseKit.sprites.Count);
        foreach (UiKitSO.KitSprite cel in theme.baseKit.sprites)
        {
            if (cel == null || cel.sprite == null)
                continue;
            Sprite drawn = Draw(theme, cel.name, cel.sprite, accent, textures, made);
            sprites.Add(new UiKitSO.KitSprite { name = cel.name, sprite = drawn != null ? drawn : cel.sprite });
        }
        kit.sprites = sprites;
        kit.inkOnDark = theme.ink;
        kit.inkOnLight = theme.ink;
        kit.inkAlert = theme.danger;
        kit.signalRed = theme.danger;
        kit.phosphorInk = theme.phosphor;
        kit.ClearCache();
        return kit;
    }

    /// <summary>The cyberpunk sprite for the cel sprite <paramref name="cel"/> named <paramref name="name"/>, or null to keep the cel one.</summary>
    private static Sprite Draw(PcThemeSO theme, string name, Sprite cel, Color accent, Dictionary<string, Texture2D> textures, List<Object> made)
    {
        PcPart part = PcSkinRules.Of(name);
        switch (part.Piece)
        {
            case PcPiece.Keep:
                return null;
            case PcPiece.Glyph:
            case PcPiece.Tile:
                return theme.Glyph(name);
        }

        Vector4 border = cel.border;
        bool sliced = border.sqrMagnitude > 0f;
        int pad = theme.baseKit.spritePad;
        int w = sliced ? Mathf.Max((int)(border.x + border.z) + 4, 2 * pad + 4) : (int)cel.rect.width;
        int h = sliced ? Mathf.Max((int)(border.y + border.w) + 4, 2 * pad + 4) : (int)cel.rect.height;
        PcPaint paint = Paint(theme, part, accent, pad, Mathf.Min(w, h));
        string key = $"{part}/{w}x{h}";
        if (!textures.TryGetValue(key, out Texture2D texture))
        {
            texture = Paint(paint, w, h, key);
            textures.Add(key, texture);
            made.Add(texture);
        }
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), cel.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        sprite.name = name; // the same name as the cel sprite: the skin finds either look's sprite by it
        made.Add(sprite);
        return sprite;
    }

    /// <summary>A texture <paramref name="w"/> x <paramref name="h"/> painted pixel by pixel (PcChromeArt.Pixel), clamped, filtered, not readable.</summary>
    private static Texture2D Paint(PcPaint paint, int w, int h, string name)
    {
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "PcKit " + name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                pixels[y * w + x] = ToColor(PcChromeArt.Pixel(paint, x, y, w, h));
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    /// <summary>
    /// How a part is painted (D2): panels and the deck in glass with a quiet
    /// edge and a lit top edge; title bars raised with a phosphor top edge;
    /// plates raised with their tone's edge, glowing under the pointer, sunk
    /// when pressed, dim when locked; rows clear until the pointer tints them;
    /// fields sunk with an underline; badges and checkboxes in their tone.
    /// </summary>
    private static PcPaint Paint(PcThemeSO t, PcPart part, Color accent, float pad, float size)
    {
        Color tone = t.Tone(part.Tone, accent);
        bool quiet = part.Tone == PcTone.Quiet;
        Color glow = quiet ? t.phosphor : tone;
        float r = t.cornerRadius, e = t.edgeWidth, halo = Mathf.Min(t.haloWidth, pad);
        Color none = Color.clear;
        switch (part.Piece)
        {
            case PcPiece.TitleBar:
                return Make(t.glassRaised, t.glassRaised * Shade(0.82f), t.edge, t.phosphor, none, pad, r, e, 0f);
            case PcPiece.Deck:
                return Make(t.glassDeep, t.glassDeep, t.edge, Alpha(t.phosphor, 0.85f), none, pad, r, e, 0f);
            case PcPiece.Panel:
                Color body = quiet ? t.glass : Color.Lerp(t.glass, tone, 0.1f);
                return Make(body, body * Shade(0.8f), quiet ? t.edge : tone, quiet ? Alpha(t.phosphor, 0.55f) : tone, none, pad, r, e, 0f);
            case PcPiece.Row:
                return part.State == KitState.Rest
                    ? Make(none, none, none, none, none, pad, r, e, 0f)
                    : Make(Alpha(tone, 0.16f), Alpha(tone, 0.12f), Alpha(tone, 0.45f), Alpha(tone, 0.45f), none, pad, r, e, 0f);
            case PcPiece.Field:
                Color line = quiet ? Alpha(t.phosphor, 0.6f) : tone;
                return Make(t.glassDeep, t.glassDeep, line, line, none, pad, r * 0.6f, e, 0f, true);
            case PcPiece.Scroll:
                return part.Tone == PcTone.Primary
                    ? Make(Alpha(t.phosphor, 0.5f), Alpha(t.phosphor, 0.4f), t.phosphor, t.phosphor, none, pad, size, e, 0f)
                    : Make(t.glassDeep, t.glassDeep, t.edge, t.edge, none, pad, size, e, 0f);
            case PcPiece.Badge:
                return Make(tone, tone * Shade(0.85f), Alpha(Color.white, 0.6f), Alpha(Color.white, 0.6f), Alpha(tone, 0.4f), pad, size, e, halo);
            case PcPiece.Check:
                return quiet
                    ? Make(t.glassDeep, t.glassDeep, Alpha(t.phosphor, 0.6f), Alpha(t.phosphor, 0.6f), none, pad, r * 0.5f, e, 0f)
                    : Make(tone, tone * Shade(0.85f), tone, Color.white, Alpha(tone, 0.35f), pad, r * 0.5f, e, halo);
        }

        // A plate, by its state.
        Color rim = quiet ? Alpha(t.phosphor, 0.45f) : tone;
        Color top = quiet ? Alpha(t.phosphor, 0.85f) : Color.Lerp(tone, Color.white, 0.35f);
        float tint = quiet ? 0f : 0.16f;
        switch (part.State)
        {
            case KitState.Hover:
                Color lit = Color.Lerp(t.glassRaised, glow, tint + 0.12f);
                return Make(lit, lit * Shade(0.85f), glow, Color.Lerp(glow, Color.white, 0.4f), Alpha(glow, t.haloStrength), pad, r, e, halo);
            case KitState.Pressed:
                Color sunk = Color.Lerp(t.glassDeep, glow, tint + 0.2f);
                return Make(sunk * Shade(0.85f), sunk, glow, Alpha(glow, 0.5f), Alpha(glow, t.haloStrength * 0.5f), pad, r, e, halo * 0.6f);
            case KitState.Locked:
                return Make(t.glassDeep, t.glassDeep, Alpha(t.inkMuted, 0.35f), Alpha(t.inkMuted, 0.35f), none, pad, r, e, 0f);
            default:
                Color rest = Color.Lerp(t.glassRaised, tone, tint);
                return Make(rest, rest * Shade(0.82f), rim, top, none, pad, r, e, 0f);
        }
    }

    /// <summary>A paint from Unity colours.</summary>
    private static PcPaint Make(Color fill, Color fillLow, Color edge, Color lit, Color halo, float pad, float radius, float edgeWidth, float haloWidth, bool underline = false) =>
        new PcPaint(ToRgba(fill), ToRgba(fillLow), ToRgba(edge), ToRgba(lit), ToRgba(halo), pad, radius, edgeWidth, haloWidth, underline);

    /// <summary>A multiplier that darkens a colour by <paramref name="k"/> and keeps its alpha.</summary>
    private static Color Shade(float k) => new Color(k, k, k, 1f);

    /// <summary><paramref name="c"/> at alpha <paramref name="a"/>.</summary>
    private static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

    /// <summary>A Unity colour as an engine-free one.</summary>
    public static Rgba ToRgba(Color c) => new Rgba(c.r, c.g, c.b, c.a);

    /// <summary>An engine-free colour as a 32-bit one.</summary>
    public static Color32 ToColor(Rgba c) =>
        new Color32((byte)(Mathf.Clamp01(c.R) * 255f + 0.5f), (byte)(Mathf.Clamp01(c.G) * 255f + 0.5f), (byte)(Mathf.Clamp01(c.B) * 255f + 0.5f),
                    (byte)(Mathf.Clamp01(c.A) * 255f + 0.5f));
}
