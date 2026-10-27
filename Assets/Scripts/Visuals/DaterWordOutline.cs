using System;

/// <summary>
/// The dater word's hollow outline as coverage (FLOW; the desk machine spec
/// §1, Saleh's S-401 reference: the verdict word in an outlined slab-serif
/// face): DaterLetters' capitals laid out across the impression's word row
/// and stroked along their edges. DaterImpressionArt paints it once per word
/// a session (BUG-31a). Each pixel measures only the letters that can reach
/// it (<see cref="DefaultReach"/>), not every letter of the word; a reach of
/// infinity measures them all and paints the same pixels (FlowRulesTests pins
/// the two equal). Pure.
/// </summary>
public static class DaterWordOutline
{
    /// <summary>
    /// How far beside its advance box a letter is still measured (cap heights):
    /// the serifs reach at most 0.1 past the box, and a pixel inks only within
    /// the stroke's half-width plus half a pixel of an edge, so a letter further
    /// than this cannot change the pixel (its distance is never the nearest inked one).
    /// </summary>
    public const float DefaultReach = 0.3f;

    /// <summary>The advance of a space (cap heights).</summary>
    private const float SpaceWidth = 0.4f;

    /// <summary>True when <paramref name="word"/> has a letter and DaterLetters draws every letter of it but a space (the slab face prints it; otherwise the font's outline does).</summary>
    public static bool Drawn(string word)
    {
        bool any = false;
        foreach (char c in word ?? string.Empty)
        {
            if (c == ' ')
                continue;
            if (!DaterLetters.Has(c))
                return false;
            any = true;
        }
        return any;
    }

    /// <summary>
    /// <paramref name="word"/> (upper-cased) as outline coverage (0..1 per
    /// pixel, rows bottom-up as a texture's) over a
    /// <paramref name="width"/>×<paramref name="height"/> layer: fitted into
    /// the row between the shares <paramref name="top"/> and
    /// <paramref name="bottom"/> (from the top) and 94% of the width, centred,
    /// letters <paramref name="tracking"/> apart, each edge stroked
    /// <paramref name="line"/> to either side (cap heights). Each pixel measures
    /// the letters within <paramref name="reach"/> of it. Empty coverage for a
    /// word DaterLetters does not draw (<see cref="Drawn"/>).
    /// </summary>
    public static float[] Cover(string word, int width, int height, float top, float bottom, float line, float tracking, float reach = DefaultReach)
    {
        var cover = new float[Math.Max(0, width) * Math.Max(0, height)];
        string w = (word ?? string.Empty).ToUpperInvariant();
        if (!Drawn(w) || width <= 0 || height <= 0)
            return cover;

        // Each letter's left edge, once, and the word's advance.
        var starts = new float[w.Length];
        float pen = 0f;
        for (int i = 0; i < w.Length; i++)
        {
            starts[i] = pen;
            pen += Advance(w[i]) + tracking;
        }
        float total = pen - tracking;

        float rowH = (bottom - top) * height, rowW = 0.94f * width;
        float cap = Math.Min(rowH, total > 0f ? rowW / total : rowH);
        float left = (width - total * cap) / 2f, baseline = (1f - bottom) * height + (rowH - cap) / 2f;

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float ex = (x + 0.5f - left) / cap, ey = (y + 0.5f - baseline) / cap;
                if (ex < -0.2f || ex > total + 0.2f || ey < -0.2f || ey > 1.2f)
                    continue;
                float d = float.MaxValue;
                for (int i = 0; i < w.Length; i++)
                {
                    char c = w[i];
                    if (c == ' ' || ex < starts[i] - reach || ex > starts[i] + DaterLetters.Width(c) + reach)
                        continue;
                    d = Math.Min(d, DaterLetters.Distance(c, ex - starts[i], ey));
                }
                float c01 = (line - Math.Abs(d)) * cap + 0.5f;
                cover[y * width + x] = c01 < 0f ? 0f : c01 > 1f ? 1f : c01;
            }
        return cover;
    }

    /// <summary>A character's advance (cap heights): a space's, or the letter's width.</summary>
    private static float Advance(char c) => c == ' ' ? SpaceWidth : DaterLetters.Width(c);
}
