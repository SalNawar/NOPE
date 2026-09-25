using System;
using System.Globalization;

/// <summary>
/// An engine-free RGBA colour (channels 0..1) for the theme maths: contrast,
/// palettes and placeholder painters. Themes are authored as hex strings in
/// world_source.json and parsed here (piece 6).
/// </summary>
public readonly struct Rgba
{
    /// <summary>Red, 0..1.</summary>
    public readonly float R;

    /// <summary>Green, 0..1.</summary>
    public readonly float G;

    /// <summary>Blue, 0..1.</summary>
    public readonly float B;

    /// <summary>Alpha, 0..1 (1 = opaque).</summary>
    public readonly float A;

    /// <summary>A colour from its channels (opaque unless <paramref name="a"/> says otherwise).</summary>
    public Rgba(float r, float g, float b, float a = 1f)
    {
        R = r;
        G = g;
        B = b;
        A = a;
    }

    /// <summary>The same colour with another alpha.</summary>
    public Rgba WithAlpha(float a) => new Rgba(R, G, B, a);

    /// <summary>
    /// Parses "#RRGGBB" or "#RRGGBBAA" (either case). Anything else, including
    /// null, returns false with a default colour.
    /// </summary>
    public static bool TryParseHex(string hex, out Rgba color)
    {
        color = default;
        if (hex == null || hex.Length != 7 && hex.Length != 9 || hex[0] != '#')
            return false;

        var channels = new float[] { 0f, 0f, 0f, 1f };
        for (int i = 0; i < (hex.Length - 1) / 2; i++)
        {
            if (!byte.TryParse(hex.Substring(1 + 2 * i, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte value))
                return false;
            channels[i] = value / 255f;
        }

        color = new Rgba(channels[0], channels[1], channels[2], channels[3]);
        return true;
    }
}
