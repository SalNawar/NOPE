using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;

/// <summary>
/// The daters' impression and date bands, painted at runtime (the desk
/// machine spec §1; Saleh's reference, the S-401: "the word in a hollow,
/// outlined, slab-serif display face in violet-blue, below it the date in
/// solid red, then BY: in violet-blue"). An impression is the verdict word's
/// outline (DaterLetters; a letter they lack, the font's outline), the date
/// in solid red and the BY line, each print inked by its density
/// (DaterInk: the pad's fade and the print's own variation) with blotchy
/// coverage, edge breaks where the rubber missed and a faint smudge a
/// pixel or two off. The date and the BY line come from a TMP font's SDF
/// atlas (it must be readable; GeistMono-Bold's is), sampled on the CPU. A
/// clean print (density 1, no wear) shows in the dater's top window. A date
/// band (Band) is the rubber ring of one date wheel: its labels round its
/// circumference. The caller owns every texture made here and destroys it.
/// </summary>
public static class DaterImpressionArt
{
    /// <summary>An impression texture's size in pixels (its aspect is the mark's on the paper).</summary>
    public const int Width = 480, Height = 216;

    /// <summary>The violet-blue ink (the word, the BY line) and the red ink (the date).</summary>
    public static readonly Color32 Violet = new Color32(72, 60, 168, 255), Red = new Color32(196, 34, 40, 255);

    /// <summary>The rows (shares of the height, from the top): the word, the date, the BY line.</summary>
    private const float WordTop = 0.05f, WordBottom = 0.53f, DateTop = 0.57f, DateBottom = 0.79f, ByTop = 0.82f, ByBottom = 0.97f;

    /// <summary>The word's outline half-width (cap heights) and its letters' tracking.</summary>
    private const float Line = 0.034f, Tracking = 0.09f;

    /// <summary>One date band cell's size in pixels: across (around the wheel) and along its axis.</summary>
    private const int CellAcross = 40, CellAlong = 112;

    /// <summary>The clean layers of a print, by its texts (the word, the date and the BY line change at most daily).</summary>
    private static readonly Dictionary<string, Layers> Cache = new Dictionary<string, Layers>();

    /// <summary>The fonts' atlases' alpha, read once.</summary>
    private static readonly Dictionary<TMP_FontAsset, byte[]> Atlases = new Dictionary<TMP_FontAsset, byte[]>();

    /// <summary>A print's three coverage layers (0..1 per pixel, rows bottom-up as a texture's).</summary>
    private sealed class Layers
    {
        public float[] Word, Date, By;
    }

    /// <summary>
    /// A print of <paramref name="word"/> over <paramref name="date"/> and
    /// <paramref name="byLine"/> (the date and the BY line in
    /// <paramref name="font"/>) at ink <paramref name="density"/> (0..1),
    /// worn by the print's <paramref name="seed"/> (its blotches, edge breaks
    /// and smudge; <paramref name="worn"/> false: a clean print). Textures
    /// run bottom-up; the print reads the right way round.
    /// </summary>
    public static Texture2D Paint(string word, string date, string byLine, TMP_FontAsset font, float density, int seed, bool worn = true)
    {
        Layers layers = LayersOf(word ?? string.Empty, date ?? string.Empty, byLine ?? string.Empty, font);
        var pixels = new Color32[Width * Height];
        float d = Mathf.Clamp01(density);
        int sx = worn ? 1 + (int)(DaterInk.Hash01(seed, 1, 7) * 2.5f) : 0, sy = worn ? (int)(DaterInk.Hash01(seed, 2, 7) * 2.5f) - 1 : 0;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                int i = y * Width + x;
                float violet = Mathf.Max(layers.Word[i], layers.By[i]), red = layers.Date[i];
                if (worn)
                {
                    int j = Mathf.Clamp(y - sy, 0, Height - 1) * Width + Mathf.Clamp(x - sx, 0, Width - 1);
                    violet = Mathf.Max(violet, 0.16f * Mathf.Max(layers.Word[j], layers.By[j]));
                    red = Mathf.Max(red, 0.16f * layers.Date[j]);
                }
                float cover = Mathf.Max(violet, red);
                if (cover <= 0f)
                    continue;
                float ink = d;
                if (worn)
                {
                    float blotch = Noise(seed, x, y, 22), grain = Noise(seed + 101, x, y, 3);
                    ink *= 0.74f + 0.34f * blotch;
                    if (grain < (1.15f - d) * 0.42f)
                        ink *= 0.12f; // an edge break: the rubber missed here
                }
                Color32 colour = red > violet ? Red : Violet;
                colour.a = (byte)(255f * Mathf.Clamp01(cover * ink));
                pixels[i] = colour;
            }
        var texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true) { name = "DaterImpression", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        return texture;
    }

    /// <summary>
    /// One date wheel's rubber band: <paramref name="labels"/> (the wheel's
    /// notches in order) printed round it in <paramref name="font"/>,
    /// <paramref name="ink"/> on <paramref name="rubber"/>: one cell a label
    /// up the texture's height (round a Unity cylinder's circumference), each
    /// label reading across its width (along the wheel's axis).
    /// </summary>
    public static Texture2D Band(IReadOnlyList<string> labels, TMP_FontAsset font, Color32 rubber, Color32 ink)
    {
        int n = Mathf.Max(1, labels.Count);
        int w = CellAlong, h = n * CellAcross;
        var pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = rubber;
        for (int cell = 0; cell < labels.Count; cell++)
        {
            float[] cover = TextLayer(labels[cell], font, CellAlong, CellAcross, 0.08f, 0.92f, 0.12f, 0.88f, 0.04f);
            for (int ty = 0; ty < CellAcross; ty++)
                for (int tx = 0; tx < CellAlong; tx++)
                {
                    float c = cover[ty * CellAlong + tx];
                    if (c <= 0f)
                        continue;
                    int at = (cell * CellAcross + ty) * w + tx;
                    pixels[at] = Color32.Lerp(pixels[at], ink, c);
                }
        }
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, true) { name = "DaterBand", wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear };
        texture.SetPixels32(pixels);
        texture.Apply(true, true);
        return texture;
    }

    /// <summary>The clean layers of a print (painted once per texts).</summary>
    private static Layers LayersOf(string word, string date, string byLine, TMP_FontAsset font)
    {
        string key = word + "\n" + date + "\n" + byLine + "\n" + (font != null ? font.GetInstanceID() : 0);
        if (Cache.TryGetValue(key, out Layers layers))
            return layers;
        layers = new Layers
        {
            Word = WordLayer(word, font),
            Date = TextLayer(date, font, Width, Height, 0.06f, 0.94f, DateTop, DateBottom, 0.035f),
            By = TextLayer(byLine, font, Width, Height, 0.06f, 0.94f, ByTop, ByBottom, 0.02f)
        };
        if (Cache.Count > 16)
            Cache.Clear();
        Cache[key] = layers;
        return layers;
    }

    /// <summary>The word's outline in the word row: DaterLetters' slab-serif capitals, or (a letter they lack) the whole word's outline from the font.</summary>
    private static float[] WordLayer(string word, TMP_FontAsset font)
    {
        var cover = new float[Width * Height];
        string w = word.ToUpperInvariant();
        foreach (char c in w)
            if (c != ' ' && !DaterLetters.Has(c))
                return TextLayer(w, font, Width, Height, 0.04f, 0.96f, WordTop, WordBottom, 0f, Line * 0.9f);

        float total = 0f;
        foreach (char c in w)
            total += (c == ' ' ? 0.4f : DaterLetters.Width(c)) + Tracking;
        total -= Tracking;
        float rowH = (WordBottom - WordTop) * Height, rowW = 0.94f * Width;
        float cap = Mathf.Min(rowH, total > 0f ? rowW / total : rowH);
        float left = (Width - total * cap) / 2f, baseline = (1f - WordBottom) * Height + (rowH - cap) / 2f;
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                float ex = (x + 0.5f - left) / cap, ey = (y + 0.5f - baseline) / cap;
                if (ex < -0.2f || ex > total + 0.2f || ey < -0.2f || ey > 1.2f)
                    continue;
                float d = float.MaxValue, at = 0f;
                foreach (char c in w)
                {
                    if (c != ' ')
                        d = Mathf.Min(d, DaterLetters.Distance(c, ex - at, ey));
                    at += (c == ' ' ? 0.4f : DaterLetters.Width(c)) + Tracking;
                }
                cover[y * Width + x] = Mathf.Clamp01((Line - Mathf.Abs(d)) * cap + 0.5f);
            }
        return cover;
    }

    /// <summary>
    /// <paramref name="text"/> in <paramref name="font"/> as coverage over a
    /// w×h layer (rows bottom-up), fitted into the box between the shares
    /// <paramref name="left"/>..<paramref name="right"/> and
    /// <paramref name="top"/>..<paramref name="bottom"/> (from the top) and
    /// centred; <paramref name="bold"/> thickens the strokes (an SDF bias);
    /// <paramref name="outline"/> &gt; 0 strokes only the outline that wide
    /// (cap heights). Empty for no font or an unreadable atlas (with a warning).
    /// </summary>
    private static float[] TextLayer(string text, TMP_FontAsset font, int w, int h, float left, float right, float top, float bottom, float bold, float outline = 0f)
    {
        var cover = new float[w * h];
        byte[] atlas = AtlasOf(font);
        if (atlas == null || string.IsNullOrEmpty(text))
            return cover;
        FaceInfo face = font.faceInfo;
        int aw = font.atlasWidth, pad = font.atlasPadding;
        float capAtlas = face.capLine > 0f ? face.capLine : face.ascentLine * 0.7f;

        // The glyphs and the string's advance, in atlas pixels.
        var glyphs = new List<(Glyph glyph, float x)>();
        float advance = 0f;
        foreach (char c in text)
        {
            if (!font.characterLookupTable.TryGetValue(c, out TMP_Character ch) || ch.glyph == null)
            {
                advance += capAtlas * 0.5f;
                continue;
            }
            glyphs.Add((ch.glyph, advance));
            advance += ch.glyph.metrics.horizontalAdvance;
        }
        float boxW = (right - left) * w, boxH = (bottom - top) * h;
        float scale = Mathf.Min(boxH / capAtlas, advance > 0f ? boxW / advance : boxH / capAtlas); // output px per atlas px
        float originX = (w - advance * scale) / 2f, baseline = (1f - bottom) * h + (boxH - capAtlas * scale) / 2f;
        float spread = 2f * pad; // the SDF's falloff across the padding, in atlas px
        foreach ((Glyph glyph, float gx) in glyphs)
        {
            GlyphRect r = glyph.glyphRect;
            GlyphMetrics m = glyph.metrics;
            // The glyph's box on the layer, padded.
            float x0 = originX + (gx + m.horizontalBearingX - pad) * scale, x1 = originX + (gx + m.horizontalBearingX + m.width + pad) * scale;
            float y0 = baseline + (m.horizontalBearingY - m.height - pad) * scale, y1 = baseline + (m.horizontalBearingY + pad) * scale;
            for (int py = Mathf.Max(0, (int)y0); py < Mathf.Min(h, Mathf.CeilToInt(y1)); py++)
                for (int px = Mathf.Max(0, (int)x0); px < Mathf.Min(w, Mathf.CeilToInt(x1)); px++)
                {
                    float ax = r.x + ((px + 0.5f - originX) / scale - gx - m.horizontalBearingX);
                    float ay = r.y + ((py + 0.5f - baseline) / scale - (m.horizontalBearingY - m.height));
                    float a = Sample(atlas, aw, font.atlasHeight, ax, ay);
                    float dist = (0.5f - a) * spread; // atlas px, positive outside
                    float c = outline > 0f
                        ? Mathf.Clamp01((outline * capAtlas - Mathf.Abs(dist)) * scale + 0.5f)
                        : Mathf.Clamp01((bold * capAtlas - dist) * scale + 0.5f);
                    int i = py * w + px;
                    cover[i] = Mathf.Max(cover[i], c);
                }
        }
        return cover;
    }

    /// <summary>The atlas's alpha at (x, y) in atlas pixels, bilinear; 0 outside.</summary>
    private static float Sample(byte[] atlas, int w, int h, float x, float y)
    {
        x -= 0.5f;
        y -= 0.5f;
        int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
        float fx = x - ix, fy = y - iy;
        float At(int u, int v) => u < 0 || v < 0 || u >= w || v >= h ? 0f : atlas[v * w + u] / 255f;
        return Mathf.Lerp(Mathf.Lerp(At(ix, iy), At(ix + 1, iy), fx), Mathf.Lerp(At(ix, iy + 1), At(ix + 1, iy + 1), fx), fy);
    }

    /// <summary>The font's atlas alpha, read once; null (with a warning) without a font or a readable atlas.</summary>
    private static byte[] AtlasOf(TMP_FontAsset font)
    {
        if (font == null)
            return null;
        if (Atlases.TryGetValue(font, out byte[] alpha))
            return alpha;
        Texture2D atlas = font.atlasTexture;
        if (atlas == null || !atlas.isReadable)
        {
            Debug.LogWarning($"[DaterImpressionArt] The font '{font.name}' has no readable atlas, so the daters print no date. Use a font asset whose atlas is readable (GeistMono-Bold SDF).");
            Atlases[font] = null;
            return null;
        }
        Color32[] pixels = atlas.GetPixels32();
        alpha = new byte[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
            alpha[i] = pixels[i].a;
        Atlases[font] = alpha;
        return alpha;
    }

    /// <summary>Smooth value noise in 0..1 at pixel (x, y) with cells of <paramref name="cell"/> pixels (DaterInk.Hash01 at the lattice).</summary>
    private static float Noise(int seed, int x, int y, int cell)
    {
        float fx = (float)x / cell, fy = (float)y / cell;
        int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
        float tx = fx - ix, ty = fy - iy;
        tx = tx * tx * (3f - 2f * tx);
        ty = ty * ty * (3f - 2f * ty);
        float a = DaterInk.Hash01(seed, ix, iy), b = DaterInk.Hash01(seed, ix + 1, iy);
        float c = DaterInk.Hash01(seed, ix, iy + 1), d = DaterInk.Hash01(seed, ix + 1, iy + 1);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    /// <summary>Forgets the caches when play starts (the editor without a domain reload keeps statics).</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Cache.Clear();
        Atlases.Clear();
    }
}
