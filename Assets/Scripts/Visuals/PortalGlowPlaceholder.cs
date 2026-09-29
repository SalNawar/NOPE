using System;

/// <summary>
/// The portal rings' effects until their art lands (the portals spec v3
/// VX1, VX4, VX5; the slots Office/portal_glow and Office/portal_return_glow):
/// a soft radial glow with a gentle swirl (an open departure ring) and an
/// inward spiral (the Return Gate), white on transparent (the game tints them
/// and draws them additive), RGBA32, row 0 = bottom, clear at the rim; and the
/// departure flare's curve. Pure, so it is tested headless; PortalEffect turns
/// the bytes into a sprite when a slot has no art, and never writes them to disk.
/// </summary>
public static class PortalGlowPlaceholder
{
    /// <summary>The texture's side in pixels (a placeholder resolution, not a gameplay knob).</summary>
    public const int Size = 128;

    /// <summary>The open ring's glow: bright at the centre, fading to nothing at the rim, with six soft swirling arms.</summary>
    public static byte[] Glow() => Render((r, a) => Falloff(r, 1.6f) * (0.7f + 0.3f * (float)Math.Cos(6.0 * a + 9.0 * r)));

    /// <summary>The Return Gate's spiral: three arms winding inward to a bright core.</summary>
    public static byte[] ReturnSpiral() => Render((r, a) =>
    {
        float arms = (float)Math.Pow(Math.Max(0.0, Math.Cos(3.0 * a - 16.0 * r)), 2.0);
        return Falloff(r, 1.2f) * (0.25f + 0.75f * arms) + Falloff(r * 3.2f, 2f) * 0.6f;
    });

    /// <summary>
    /// The departure flare at <paramref name="progress"/> (0 to 1 of its time):
    /// 0 at both ends, 1 at its middle, eased; with <paramref name="reduced"/>
    /// motion one step: 1 until its time is up, then 0.
    /// </summary>
    public static float Pulse(float progress, bool reduced)
    {
        if (progress <= 0f || progress >= 1f)
            return 0f;
        return reduced ? 1f : (float)Math.Sin(Math.PI * progress);
    }

    /// <summary>1 at the centre, 0 at and past the rim (<paramref name="r"/> 1), eased by <paramref name="power"/>.</summary>
    private static float Falloff(float r, float power) => r >= 1f ? 0f : (float)Math.Pow(1.0 - r, power);

    /// <summary>A white texture whose alpha is <paramref name="alpha"/>(radius 0 to 1 from the centre, angle in radians), clamped to 0..1.</summary>
    private static byte[] Render(Func<float, double, float> alpha)
    {
        var rgba = new byte[Size * Size * 4];
        float half = Size / 2f;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                float r = (float)Math.Sqrt(dx * dx + dy * dy);
                float a = Math.Max(0f, Math.Min(1f, alpha(r, Math.Atan2(dy, dx))));
                int i = (y * Size + x) * 4;
                rgba[i] = rgba[i + 1] = rgba[i + 2] = 255;
                rgba[i + 3] = (byte)Math.Round(a * 255f);
            }
        }
        return rgba;
    }
}
