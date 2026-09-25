using System;

/// <summary>
/// Finds a cursor's click point from its alpha channel, so swapping cursor art
/// never leaves the click point off the drawn tip. Alpha grids are row-major
/// with row 0 = bottom (Unity's GetPixels32); results are in cursor hotspot
/// space (pixels from the top-left). Faint anti-aliasing is ignored.
/// </summary>
public static class CursorHotspot
{
    /// <summary>Alpha at or above this counts as part of the drawn cursor.</summary>
    public const byte SolidThreshold = 128;

    /// <summary>Which point of the topmost solid row is the click point.</summary>
    public enum Kind
    {
        /// <summary>Arrow: the leftmost solid pixel of the top row.</summary>
        Tip,

        /// <summary>Pointing hand: the centre of the top row's first solid run.</summary>
        Fingertip,
    }

    /// <summary>Returns the hotspot (x, y from top-left); (0, 0) for an empty image.</summary>
    public static (int x, int y) Find(byte[] alpha, int width, int height, Kind kind)
    {
        if (width <= 0 || height <= 0 || alpha == null || alpha.Length != width * height)
            throw new ArgumentException("The alpha grid must hold width * height values.", nameof(alpha));

        for (int y = height - 1; y >= 0; y--)
        {
            int first = -1;
            int last = -1;
            for (int x = 0; x < width; x++)
            {
                bool solid = alpha[y * width + x] >= SolidThreshold;
                if (solid && first < 0)
                    first = x;
                if (solid)
                    last = x;
                else if (first >= 0)
                    break; // end of the first run
            }

            if (first < 0)
                continue;

            int hx = kind == Kind.Tip ? first : (first + last + 1) / 2;
            return (hx, height - 1 - y);
        }

        return (0, 0);
    }
}
