using System;

/// <summary>
/// Draws the desktop's placeholder icon glyphs (the PC redesign DK2, until
/// the asset list's Assets/Art/UI/Resources/Desktop/icon_&lt;id&gt;.png art lands): a
/// white glyph on transparent per app id, shapes only, never letters: a
/// magnifying glass (investigation), a globe (internet), an envelope (mail),
/// an ID card (citizen_account), a ruled sheet with a folded corner (notes)
/// and a gear (settings). The theme tints the white. RGBA32, row 0 = bottom
/// (like Texture2D raw data), a clear border all round. Pure, so it is tested
/// headless; DesktopIconView turns the bytes into a sprite when an icon has no
/// art, and never writes them to disk.
/// </summary>
public static class DesktopIconPlaceholder
{
    /// <summary>The glyph's side in pixels (a placeholder resolution, not a gameplay knob).</summary>
    public const int Size = 64;

    /// <summary>The glyph for an app id (DesktopAppIds' ids; this assembly does not see the Domain, so they are written out here and the tests check they match), or null for an id with no glyph.</summary>
    public static byte[] Render(string appId)
    {
        if (!Knows(appId))
            return null;

        var rgba = new byte[Size * Size * 4];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                if (!In(appId, x + 0.5f, y + 0.5f))
                    continue;

                int i = (y * Size + x) * 4;
                rgba[i] = rgba[i + 1] = rgba[i + 2] = rgba[i + 3] = 255;
            }
        }

        return rgba;
    }

    /// <summary>True for the ids this class draws.</summary>
    private static bool Knows(string appId)
    {
        switch (appId)
        {
            case "investigation":
            case "internet":
            case "mail":
            case "citizen_account":
            case "notes":
            case "settings":
                return true;
            default:
                return false;
        }
    }

    /// <summary>Whether a point (pixels, y up from the bottom) lies in the glyph.</summary>
    private static bool In(string appId, float px, float py)
    {
        switch (appId)
        {
            case "investigation":
                // A lens ring and a handle down to the right.
                return Ring(26f, 38f, 17f, 11f, px, py) || NearSegment(38f, 26f, 55f, 9f, 4.5f, px, py);

            case "internet":
            {
                // A globe: its outline, a meridian ellipse and the equator, clipped to the disc.
                bool disc = PixelShapes.InEllipse(32f, 32f, 25f, 25f, px, py);
                return Ring(32f, 32f, 25f, 21f, px, py) ||
                       (disc && (Math.Abs(EllipseRadius(32f, 32f, 11f, 25f, px, py) - 1f) < 0.16f || Math.Abs(py - 32f) <= 2f || Math.Abs(px - 32f) <= 2f));
            }

            case "mail":
                // An envelope: its outline and the flap's V.
                return Frame(8f, 14f, 56f, 50f, 4f, px, py) || (py <= 50f && py >= 26f && (NearSegment(9f, 49f, 32f, 29f, 2.5f, px, py) || NearSegment(55f, 49f, 32f, 29f, 2.5f, px, py)));

            case "citizen_account":
                // An ID card: its outline, a head and shoulders on the left, two lines on the right.
                return Frame(5f, 14f, 59f, 50f, 3.5f, px, py) ||
                       PixelShapes.InEllipse(21f, 37f, 6f, 6f, px, py) ||
                       (PixelShapes.InEllipse(21f, 20f, 11f, 9f, px, py) && py >= 18f && py <= 29f) ||
                       (px >= 35f && px <= 53f && ((py >= 34f && py <= 38f) || (py >= 24f && py <= 28f)));

            case "notes":
            {
                // A sheet with a folded top-right corner and three rules.
                bool sheet = PixelShapes.InPolygon(new (float x, float y)[] { (12f, 6f), (52f, 6f), (52f, 44f), (40f, 58f), (12f, 58f) }, px, py);
                bool inside = px > 16f && px < 48f && py > 10f && py < 40f;
                bool rule = (py >= 16f && py <= 19f) || (py >= 25f && py <= 28f) || (py >= 34f && py <= 37f);
                return sheet && !(inside && !rule);
            }

            case "settings":
            {
                // A gear: a toothed rim (eight teeth) round a hole.
                float dx = px - 32f, dy = py - 32f;
                float r = (float)Math.Sqrt(dx * dx + dy * dy);
                double angle = Math.Atan2(dy, dx);
                bool tooth = Math.Cos(angle * 8.0) > 0.35;
                return r >= 9f && (r <= 19f || (tooth && r <= 27f));
            }

            default:
                return false;
        }
    }

    /// <summary>Inside the ring between two circles round (cx, cy).</summary>
    private static bool Ring(float cx, float cy, float outer, float inner, float px, float py) =>
        PixelShapes.InEllipse(cx, cy, outer, outer, px, py) && !PixelShapes.InEllipse(cx, cy, inner, inner, px, py);

    /// <summary>Inside the border of a rect, <paramref name="width"/> thick.</summary>
    private static bool Frame(float x0, float y0, float x1, float y1, float width, float px, float py) =>
        px >= x0 && px <= x1 && py >= y0 && py <= y1 && (px < x0 + width || px > x1 - width || py < y0 + width || py > y1 - width);

    /// <summary>The point's normalised radius in an axis-aligned ellipse (1 on its edge).</summary>
    private static float EllipseRadius(float cx, float cy, float rx, float ry, float px, float py)
    {
        float dx = (px - cx) / rx, dy = (py - cy) / ry;
        return (float)Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Within <paramref name="halfWidth"/> of the segment from (x0, y0) to (x1, y1).</summary>
    private static bool NearSegment(float x0, float y0, float x1, float y1, float halfWidth, float px, float py)
    {
        float vx = x1 - x0, vy = y1 - y0;
        float t = Math.Max(0f, Math.Min(1f, ((px - x0) * vx + (py - y0) * vy) / (vx * vx + vy * vy)));
        float dx = px - (x0 + t * vx), dy = py - (y0 + t * vy);
        return dx * dx + dy * dy <= halfWidth * halfWidth;
    }
}
