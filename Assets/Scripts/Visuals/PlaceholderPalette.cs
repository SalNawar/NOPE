/// <summary>
/// Colours of generated character placeholders: an HSV conversion for
/// country hues and era shades, the art brief's five skin swatches and its
/// hair colours. Pure, so every colour rule is tested headless.
/// </summary>
public static class PlaceholderPalette
{
    /// <summary>The art brief's skin swatches, tones 1..5 (#F1D3C0, #E0B394, #C39A6B, #94653F, #5C3A24).</summary>
    private static readonly (byte r, byte g, byte b)[] SkinSwatches =
    {
        (0xF1, 0xD3, 0xC0), (0xE0, 0xB3, 0x94), (0xC3, 0x9A, 0x6B), (0x94, 0x65, 0x3F), (0x5C, 0x3A, 0x24)
    };

    /// <summary>A neutral grey for anything unknown.</summary>
    public static readonly (byte r, byte g, byte b) Unknown = (0x80, 0x80, 0x80);

    /// <summary>
    /// A colour from hue, saturation and value: <paramref name="h"/> wraps (its
    /// fractional part; 0 red, 1/3 green, 2/3 blue), <paramref name="s"/> and
    /// <paramref name="v"/> are clamped to 0..1, channels round to the nearest byte.
    /// </summary>
    public static (byte r, byte g, byte b) FromHsv(float h, float s, float v)
    {
        h -= (float)System.Math.Floor(h);
        s = s < 0f ? 0f : s > 1f ? 1f : s;
        v = v < 0f ? 0f : v > 1f ? 1f : v;

        float sector = h * 6f;
        int i = (int)sector % 6;
        float f = sector - (float)System.Math.Floor(sector);
        float p = v * (1f - s);
        float q = v * (1f - s * f);
        float t = v * (1f - s * (1f - f));

        float r, g, b;
        switch (i)
        {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }

        return (Byte(r), Byte(g), Byte(b));
    }

    /// <summary>The skin swatch of a tone 1..5 (grey outside that range).</summary>
    public static (byte r, byte g, byte b) Skin(int tone) =>
        tone >= 1 && tone <= SkinSwatches.Length ? SkinSwatches[tone - 1] : Unknown;

    /// <summary>A hair colour by its LookKeys token (black, brown, blond, red, grey); grey for anything else (a wig's own colour).</summary>
    public static (byte r, byte g, byte b) Hair(string colour)
    {
        switch (colour)
        {
            case "black": return (0x2B, 0x25, 0x22);
            case "brown": return (0x6B, 0x4A, 0x2F);
            case "blond": return (0xD8, 0xB5, 0x6A);
            case "red": return (0xA8, 0x45, 0x2A);
            case "grey": return (0xB4, 0xB4, 0xB4);
            default: return Unknown;
        }
    }

    /// <summary>A darker copy of a colour (each channel times <paramref name="factor"/>, clamped to 0..1).</summary>
    public static (byte r, byte g, byte b) Darker((byte r, byte g, byte b) c, float factor)
    {
        factor = factor < 0f ? 0f : factor > 1f ? 1f : factor;
        return (Byte(c.r / 255f * factor), Byte(c.g / 255f * factor), Byte(c.b / 255f * factor));
    }

    /// <summary>A 0..1 channel as the nearest byte.</summary>
    private static byte Byte(float x) => (byte)System.Math.Round(x * 255f, System.MidpointRounding.AwayFromZero);
}
