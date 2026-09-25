using System;

/// <summary>
/// Text-free placeholder art for culture themes (piece 6 U13, R11): pure
/// painters that Generate World writes as PNGs only where final art is
/// missing. Output is RGBA32 bytes, row 0 at the bottom (Unity's texture order).
/// </summary>
public static class CulturePlaceholders
{
    /// <summary>
    /// A "Bliss"-like wallpaper: a rolling hill with a ground gradient, a sky
    /// gradient and three soft clouds (the office builder's original XP
    /// painter, so the neutral colours reproduce xp_bliss.png).
    /// </summary>
    /// <exception cref="ArgumentException">A non-positive size.</exception>
    public static byte[] Wallpaper(int width, int height, Rgba skyTop, Rgba skyBottom, Rgba groundLow, Rgba groundHigh, Rgba cloud)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("A wallpaper needs a positive width and height.");

        var pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            double fy = (double)y / height;
            for (int x = 0; x < width; x++)
            {
                double fx = (double)x / width;
                double hill = 0.30 + 0.05 * Math.Sin(fx * 6.2831 * 1.4) + 0.03 * Math.Sin(fx * 6.2831 * 3.1 + 1.2);
                Rgba c;
                if (fy < hill)
                {
                    c = Lerp(groundLow, groundHigh, fy / hill);
                }
                else
                {
                    c = Lerp(skyBottom, skyTop, (fy - hill) / (1.0 - hill));
                    c = Lerp(c, cloud, Clouds(fx, fy));
                }
                Put(pixels, (y * width + x) * 4, c);
            }
        }
        return pixels;
    }

    /// <summary>The three cloud blobs' combined strength at a point (0..1).</summary>
    private static double Clouds(double fx, double fy) =>
        Clamp01(Blob(fx, fy, 0.22, 0.82, 0.13, 0.05) + Blob(fx, fy, 0.6, 0.9, 0.16, 0.05) + Blob(fx, fy, 0.82, 0.73, 0.1, 0.04));

    /// <summary>One soft elliptical blob.</summary>
    private static double Blob(double fx, double fy, double cx, double cy, double rx, double ry)
    {
        double dx = (fx - cx) / rx, dy = (fy - cy) / ry;
        return Clamp01(1.0 - (dx * dx + dy * dy)) * 0.85;
    }

    /// <summary>Linear blend of two colours (opaque).</summary>
    private static Rgba Lerp(Rgba a, Rgba b, double t)
    {
        t = Clamp01(t);
        return new Rgba((float)(a.R + (b.R - a.R) * t), (float)(a.G + (b.G - a.G) * t), (float)(a.B + (b.B - a.B) * t), 1f);
    }

    /// <summary>Writes one opaque pixel.</summary>
    private static void Put(byte[] pixels, int i, Rgba c)
    {
        pixels[i] = Byte(c.R);
        pixels[i + 1] = Byte(c.G);
        pixels[i + 2] = Byte(c.B);
        pixels[i + 3] = 255;
    }

    /// <summary>A 0..1 channel as a byte.</summary>
    private static byte Byte(float v) => (byte)Math.Round(Clamp01(v) * 255.0);

    /// <summary>Clamps to 0..1.</summary>
    private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
}
