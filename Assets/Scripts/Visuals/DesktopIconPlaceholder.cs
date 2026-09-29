using System;
using System.Collections.Generic;

/// <summary>
/// Draws the PC's placeholder glyphs (the PC redesign DK2, until the asset
/// list's art lands): a white glyph on transparent per app id, shapes only,
/// never letters: a magnifying glass (investigation), a globe (internet), an
/// envelope (mail), an ID card (citizen_account), a parcel with a tick
/// (orders), a ruled sheet with a folded corner (notes) and a gear
/// (settings) for Assets/Art/UI/Resources/Desktop/icon_&lt;id&gt;.png; and the
/// Orders tree's glyphs (<see cref="OrdersGlyphs"/>, Saleh 2026-09-29): a
/// band's by its branch for Orders/branch_&lt;id&gt;.png (a scanner, a speech
/// bubble, a portal ring, two linked rings), which a node without its own
/// icon shows too, and the node states' badges (a padlock, a clock, a tick).
/// The theme tints the white. RGBA32, row 0 = bottom (like Texture2D raw
/// data), a clear border all round. Pure, so it is tested headless;
/// DesktopIconView and the Orders tree turn the bytes into sprites when there
/// is no art, and never write them to disk.
/// </summary>
public static class DesktopIconPlaceholder
{
    /// <summary>The glyph's side in pixels (a placeholder resolution, not a gameplay knob).</summary>
    public const int Size = 64;

    /// <summary>The Orders tree's glyph keys: the four bands' (the art slot's file name, "branch_" and the branch) and the three state badges.</summary>
    public static readonly IReadOnlyList<string> OrdersGlyphs = new[]
    {
        "branch_desk", "branch_interview", "branch_portals", "branch_contacts", "padlock", "clock", "tick"
    };

    /// <summary>The notes glyph's sheet with its folded corner (pixels, y up), built once: In runs for every pixel.</summary>
    private static readonly (float x, float y)[] NotesSheet = { (12f, 6f), (52f, 6f), (52f, 44f), (40f, 58f), (12f, 58f) };

    /// <summary>The speech bubble's tail (pixels, y up), built once.</summary>
    private static readonly (float x, float y)[] BubbleTail = { (15f, 30f), (10f, 7f), (29f, 24f) };

    /// <summary>The glyph for an app id (DesktopAppIds' ids; this assembly does not see the Domain, so they are written out here and the tests check they match) or an <see cref="OrdersGlyphs"/> key, or null for a name with no glyph.</summary>
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
            case "orders":
            case "notes":
            case "settings":
            case "branch_desk":
            case "branch_interview":
            case "branch_portals":
            case "branch_contacts":
            case "padlock":
            case "clock":
            case "tick":
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

            case "orders":
                // A parcel: its box, its lid and a tick on its face.
                return Frame(10f, 8f, 54f, 40f, 4f, px, py) || (px >= 6f && px <= 58f && py >= 42f && py <= 52f) ||
                       NearSegment(20f, 25f, 28f, 17f, 3f, px, py) || NearSegment(28f, 17f, 44f, 33f, 3f, px, py);

            case "branch_desk":
                // A scanner: its body with the scan line, and a sheet feeding in at the top.
                return Frame(8f, 10f, 56f, 32f, 4f, px, py) || (px >= 14f && px <= 50f && py >= 19f && py <= 23f) || Frame(20f, 32f, 44f, 56f, 3f, px, py);

            case "branch_interview":
                // A speech bubble with its tail at the bottom left.
                return (PixelShapes.InEllipse(32f, 38f, 25f, 17f, px, py) && !PixelShapes.InEllipse(32f, 38f, 20f, 12f, px, py)) ||
                       PixelShapes.InPolygon(BubbleTail, px, py);

            case "branch_portals":
                // A portal: a ring round a smaller ring.
                return Ring(32f, 32f, 26f, 20f, px, py) || Ring(32f, 32f, 13f, 9f, px, py);

            case "branch_contacts":
                // Two linked rings.
                return Ring(23f, 32f, 16f, 11f, px, py) || Ring(41f, 32f, 16f, 11f, px, py);

            case "padlock":
                // A shackle over a body.
                return (Ring(32f, 38f, 14f, 9f, px, py) && py >= 38f) || (px >= 18f && px <= 23f && py >= 30f && py <= 38f) ||
                       (px >= 41f && px <= 46f && py >= 30f && py <= 38f) || (px >= 14f && px <= 50f && py >= 8f && py <= 32f);

            case "clock":
                // A face and two hands.
                return Ring(32f, 32f, 26f, 21f, px, py) || NearSegment(32f, 32f, 32f, 50f, 2.5f, px, py) || NearSegment(32f, 32f, 45f, 32f, 2.5f, px, py);

            case "tick":
                return NearSegment(12f, 34f, 26f, 18f, 5f, px, py) || NearSegment(26f, 18f, 53f, 47f, 5f, px, py);

            case "notes":
            {
                // A sheet with a folded top-right corner and three rules.
                bool sheet = PixelShapes.InPolygon(NotesSheet, px, py);
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
