using System;

/// <summary>
/// The 2150 city's stand-in skyline (the desk-first redesign, Saleh
/// 2026-10-05, item 6: "player can look left for a view at the city"), drawn
/// in code until the art side paints the city (docs/ART_ASSET_LIST.md, "City
/// view"): one layer of the skyline as two alpha masks of the same size, the
/// buildings' bodies (towers of seeded widths and heights standing on the
/// bottom edge, some with a spire) and their windows (a grid of small lit
/// cells inside each body, a seeded share of them lit). The same seed and
/// layer always paint the same picture. Engine-free (tested headless);
/// CityView turns the masks into textures and tints them by the time of day.
/// </summary>
public static class CitySkyline
{
    /// <summary>A painted layer: the bodies' and the windows' alpha masks (row-major, the bottom row first), width by height.</summary>
    public sealed class Layer
    {
        /// <summary>The layer's width in pixels.</summary>
        public int Width;

        /// <summary>The layer's height in pixels.</summary>
        public int Height;

        /// <summary>The buildings' bodies: 255 inside a building, 0 in the sky.</summary>
        public byte[] Bodies;

        /// <summary>The lit windows: 255 on a lit window (always inside a body), else 0.</summary>
        public byte[] Windows;
    }

    /// <summary>
    /// Paints one layer <paramref name="width"/> by <paramref name="height"/>
    /// from <paramref name="seed"/>: towers side by side across the whole
    /// width, each between <paramref name="minHeight"/> and
    /// <paramref name="maxHeight"/> of the height (0..1), a quarter of them
    /// with a spire, and windows lit at <paramref name="litShare"/> (0..1).
    /// </summary>
    public static Layer Paint(int width, int height, int seed, float minHeight, float maxHeight, float litShare)
    {
        width = Math.Max(8, width);
        height = Math.Max(8, height);
        var layer = new Layer { Width = width, Height = height, Bodies = new byte[width * height], Windows = new byte[width * height] };
        var random = new Random(seed);
        int x = 0;
        while (x < width)
        {
            int towerWidth = Math.Min(width - x, Math.Max(4, (int)(width * (0.025 + random.NextDouble() * 0.05))));
            double share = minHeight + random.NextDouble() * Math.Max(0f, maxHeight - minHeight);
            int top = Math.Max(1, Math.Min(height, (int)(height * share)));
            Fill(layer.Bodies, width, x, 0, towerWidth, top);
            if (random.NextDouble() < 0.25 && towerWidth >= 6)
            {
                int spire = Math.Max(1, towerWidth / 6);
                int spireTop = Math.Min(height, top + (int)(height * 0.08));
                Fill(layer.Bodies, width, x + towerWidth / 2 - spire / 2, top, spire, spireTop - top);
            }
            Windows(layer, random, x, towerWidth, top, litShare);
            x += towerWidth + (random.NextDouble() < 0.3 ? Math.Max(1, width / 200) : 0);
        }
        return layer;
    }

    /// <summary>The window cells of one tower: two-pixel cells on a grid inside its body (a one-pixel margin), lit at the share.</summary>
    private static void Windows(Layer layer, Random random, int x, int towerWidth, int top, float litShare)
    {
        const int cell = 2, pitch = 4;
        for (int wy = pitch; wy + cell < top - 1; wy += pitch)
            for (int wx = x + 2; wx + cell <= x + towerWidth - 2; wx += pitch)
                if (random.NextDouble() < litShare)
                    Fill(layer.Windows, layer.Width, wx, wy, cell, cell);
    }

    /// <summary>Sets a rectangle of a mask to 255 (clipped to the mask).</summary>
    private static void Fill(byte[] mask, int width, int x, int y, int w, int h)
    {
        int height = mask.Length / width;
        for (int j = Math.Max(0, y); j < Math.Min(height, y + h); j++)
            for (int i = Math.Max(0, x); i < Math.Min(width, x + w); i++)
                mask[j * width + i] = 255;
    }
}
