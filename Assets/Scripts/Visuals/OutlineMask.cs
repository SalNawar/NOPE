using System;

/// <summary>
/// Pure image maths for the hover outline: from a sprite's alpha channel,
/// builds an anti-aliased ring of a given width hugging the silhouette from
/// outside. Grids are row-major (index = y * width + x, row 0 = bottom, as in
/// Unity's GetPixels32). No UnityEngine, so it is unit-tested headless.
/// </summary>
public static class OutlineMask
{
    /// <summary>Source alpha at or above this counts as solid (inside the silhouette).</summary>
    public const byte SolidThreshold = 128;

    /// <summary>Chamfer step cost for a diagonal neighbour (sqrt 2).</summary>
    private const float DiagonalStep = 1.41421356f;

    /// <summary>Transparent border added on every side of the output: ring width + 1.</summary>
    public static int Margin(int ringWidthPx) => ringWidthPx + 1;

    /// <summary>
    /// Pivot of the ring grid, in its own pixels, that keeps it aligned with a
    /// source pivot given in source pixels.
    /// </summary>
    public static (float x, float y) ShiftPivot(float sourcePivotX, float sourcePivotY, int ringWidthPx)
    {
        int m = Margin(ringWidthPx);
        return (sourcePivotX + m, sourcePivotY + m);
    }

    /// <summary>
    /// Returns ring alpha (0..255) for a width x height alpha grid. The output
    /// grid is (width + 2 * margin) x (height + 2 * margin) so the ring never
    /// clips. Solid source pixels are 0 in the output (the ring is outside only).
    /// </summary>
    public static byte[] BuildRing(byte[] alpha, int width, int height, int ringWidthPx, out int outWidth, out int outHeight)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width), "The grid must be at least 1x1.");
        if (alpha == null || alpha.Length != width * height)
            throw new ArgumentException("The alpha grid must hold width * height values.", nameof(alpha));
        if (ringWidthPx < 1)
            throw new ArgumentOutOfRangeException(nameof(ringWidthPx), "The ring must be at least 1 px wide.");

        int m = Margin(ringWidthPx);
        int w = width + 2 * m;
        int h = height + 2 * m;
        outWidth = w;
        outHeight = h;

        // Distance from each output pixel to the nearest solid pixel: a
        // two-pass chamfer transform with orthogonal 1 and diagonal sqrt(2) steps.
        var dist = new float[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int sx = x - m;
                int sy = y - m;
                bool solid = sx >= 0 && sy >= 0 && sx < width && sy < height && alpha[sy * width + sx] >= SolidThreshold;
                dist[y * w + x] = solid ? 0f : float.MaxValue;
            }
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                float d = dist[i];
                if (x > 0) d = Math.Min(d, dist[i - 1] + 1f);
                if (y > 0)
                {
                    d = Math.Min(d, dist[i - w] + 1f);
                    if (x > 0) d = Math.Min(d, dist[i - w - 1] + DiagonalStep);
                    if (x < w - 1) d = Math.Min(d, dist[i - w + 1] + DiagonalStep);
                }
                dist[i] = d;
            }
        }

        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x;
                float d = dist[i];
                if (x < w - 1) d = Math.Min(d, dist[i + 1] + 1f);
                if (y < h - 1)
                {
                    d = Math.Min(d, dist[i + w] + 1f);
                    if (x < w - 1) d = Math.Min(d, dist[i + w + 1] + DiagonalStep);
                    if (x > 0) d = Math.Min(d, dist[i + w - 1] + DiagonalStep);
                }
                dist[i] = d;
            }
        }

        // Coverage of the band [edge, edge + ring width]. The silhouette edge sits
        // half a pixel from a solid pixel's centre, so a pixel at centre distance
        // d lies (d - 0.5) from the edge; the extra 0.5 anti-aliases the far side.
        var ring = new byte[w * h];
        for (int i = 0; i < ring.Length; i++)
        {
            float d = dist[i];
            if (d <= 0f)
                continue; // inside the silhouette

            float coverage = ringWidthPx + 1f - d;
            if (coverage <= 0f)
                continue;

            ring[i] = coverage >= 1f ? (byte)255 : (byte)Math.Round(coverage * 255.0);
        }
        return ring;
    }
}
