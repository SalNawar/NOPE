using System;
using System.Collections.Generic;

/// <summary>
/// The nations' emblems as the code draws them until the art lands (the travel
/// documents spec, TD3; ArtSlots.Emblem): one geometric mark per nation,
/// named in world_source.json countries[].passport.emblem, inked in the
/// passport cover's colour at a booklet header's left and as the visa page's
/// faint watermark. Each is a test of a point in [-1, 1] (y down), so the
/// drawing is checked headless; EmblemArt paints it into a texture. These are
/// stand-ins: the art request (docs/ART_ASSET_LIST.md, Passports) asks for
/// the real emblems.
/// </summary>
public static class EmblemShapes
{
    /// <summary>Every emblem's name, in the order the countries list them (Egypt, Greece, Italy, Iraq, China, Japan, Britain, Germany).</summary>
    public static readonly IReadOnlyList<string> Names = new[] { "WingedSun", "Laurel", "Star", "Octastar", "FiveStars", "Chrysanthemum", "Crown", "Eagle" };

    /// <summary>True when <paramref name="emblem"/> names an emblem (case-insensitive).</summary>
    public static bool Has(string emblem) => Index(emblem) >= 0;

    /// <summary>True when (<paramref name="x"/>, <paramref name="y"/>) in [-1, 1] (y down) is inked in <paramref name="emblem"/>; nothing for an unknown name.</summary>
    public static bool Inked(string emblem, float x, float y)
    {
        switch (Index(emblem))
        {
            case 0: return WingedSun(x, y);
            case 1: return Laurel(x, y);
            case 2: return Ring(x, y, 0.92f, 0.82f) || PixelShapes.InPolygon(Star(5, 0.72f, 0.3f, -Math.PI / 2.0, 0f, 0.04f), x, y);
            case 3: return Octastar(x, y);
            case 4: return FiveStars(x, y);
            case 5: return Chrysanthemum(x, y);
            case 6: return Crown(x, y);
            case 7: return PixelShapes.InPolygon(EaglePolygon, x, y);
            default: return false;
        }
    }

    private static int Index(string emblem)
    {
        for (int i = 0; i < Names.Count; i++)
            if (string.Equals(Names[i], emblem, StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    /// <summary>A ring between two radii about the centre.</summary>
    private static bool Ring(float x, float y, float outer, float inner)
    {
        float r = (float)Math.Sqrt(x * x + y * y);
        return r <= outer && r >= inner;
    }

    /// <summary>A sun disc between two wings, each of three feathers stepping down and out.</summary>
    private static bool WingedSun(float x, float y)
    {
        if (PixelShapes.InEllipse(0f, -0.05f, 0.3f, 0.3f, x, y))
            return true;
        float ax = Math.Abs(x);
        for (int k = 0; k < 3; k++)
        {
            float top = -0.2f + k * 0.17f, reach = 0.98f - k * 0.2f;
            if (ax >= 0.26f && ax <= reach && y >= top && y <= top + 0.12f)
                return true;
        }
        return false;
    }

    /// <summary>A wreath of leaves round the centre, open at the top, and a berry at its foot.</summary>
    private static bool Laurel(float x, float y)
    {
        if (PixelShapes.InEllipse(0f, 0.86f, 0.08f, 0.08f, x, y))
            return true;
        for (int i = 0; i < 14; i++)
        {
            // From the top left round the bottom to the top right, skipping the top's gap.
            double a = Math.PI * (-0.38 + i * 1.76 / 13.0) + Math.PI / 2.0;
            float cx = (float)(0.7 * Math.Cos(a)), cy = (float)(0.7 * Math.Sin(a));
            if (InRotatedEllipse(cx, cy, 0.2f, 0.085f, a + Math.PI / 2.0, x, y))
                return true;
        }
        return false;
    }

    /// <summary>An eight-pointed star in a ring, its middle cut out round a dot.</summary>
    private static bool Octastar(float x, float y)
    {
        if (PixelShapes.InEllipse(0f, 0f, 0.14f, 0.14f, x, y))
            return true;
        bool star = PixelShapes.InPolygon(Star(8, 0.95f, 0.45f, -Math.PI / 2.0, 0f, 0f), x, y);
        return star && !PixelShapes.InEllipse(0f, 0f, 0.3f, 0.3f, x, y);
    }

    /// <summary>One large star at the upper left and four small ones in an arc at its right.</summary>
    private static bool FiveStars(float x, float y)
    {
        if (PixelShapes.InPolygon(Star(5, 0.55f, 0.22f, -Math.PI / 2.0, -0.4f, -0.3f), x, y))
            return true;
        float[,] small = { { 0.35f, -0.75f }, { 0.65f, -0.4f }, { 0.65f, 0.05f }, { 0.35f, 0.42f } };
        for (int i = 0; i < 4; i++)
            if (PixelShapes.InPolygon(Star(5, 0.18f, 0.075f, -Math.PI / 2.0, small[i, 0], small[i, 1]), x, y))
                return true;
        return false;
    }

    /// <summary>Sixteen petals round a disc, each petal ringed off from the disc.</summary>
    private static bool Chrysanthemum(float x, float y)
    {
        if (PixelShapes.InEllipse(0f, 0f, 0.32f, 0.32f, x, y))
            return true;
        if (PixelShapes.InEllipse(0f, 0f, 0.4f, 0.4f, x, y))
            return false;
        for (int i = 0; i < 16; i++)
        {
            double a = i * Math.PI / 8.0;
            float cx = (float)(0.68 * Math.Cos(a)), cy = (float)(0.68 * Math.Sin(a));
            if (InRotatedEllipse(cx, cy, 0.28f, 0.11f, a, x, y))
                return true;
        }
        return false;
    }

    /// <summary>A crown: a band, five points over it with a pearl on each, and a cross over the middle point.</summary>
    private static bool Crown(float x, float y)
    {
        if (x >= -0.8f && x <= 0.8f && y >= 0.35f && y <= 0.62f)
            return true;
        if (PixelShapes.InPolygon(CrownPoints, x, y))
            return true;
        for (int i = 0; i < 5; i++)
        {
            float px = -0.8f + i * 0.4f;
            float py = i == 2 ? -0.42f : i % 2 == 0 ? -0.18f : -0.3f;
            if (PixelShapes.InEllipse(px, py, 0.1f, 0.1f, x, y))
                return true;
        }
        return (Math.Abs(x) <= 0.05f && y >= -0.9f && y <= -0.5f) || (Math.Abs(y + 0.75f) <= 0.05f && Math.Abs(x) <= 0.16f);
    }

    /// <summary>The crown's points over its band (y down).</summary>
    private static readonly (float x, float y)[] CrownPoints =
    {
        (-0.8f, 0.36f), (-0.8f, -0.1f), (-0.55f, 0.15f), (-0.4f, -0.22f), (-0.2f, 0.12f), (0f, -0.34f),
        (0.2f, 0.12f), (0.4f, -0.22f), (0.55f, 0.15f), (0.8f, -0.1f), (0.8f, 0.36f)
    };

    /// <summary>An eagle displayed: head, body, spread wings stepped into feathers and a fanned tail (y down).</summary>
    private static readonly (float x, float y)[] EaglePolygon =
    {
        (0f, -0.82f), (0.12f, -0.72f), (0.1f, -0.5f), (0.3f, -0.55f), (0.95f, -0.7f), (0.85f, -0.48f), (0.95f, -0.38f), (0.8f, -0.22f),
        (0.88f, -0.1f), (0.65f, 0.02f), (0.7f, 0.15f), (0.4f, 0.15f), (0.18f, 0.35f), (0.4f, 0.85f), (0.12f, 0.65f), (0f, 0.9f),
        (-0.12f, 0.65f), (-0.4f, 0.85f), (-0.18f, 0.35f), (-0.4f, 0.15f), (-0.7f, 0.15f), (-0.65f, 0.02f), (-0.88f, -0.1f), (-0.8f, -0.22f),
        (-0.95f, -0.38f), (-0.85f, -0.48f), (-0.95f, -0.7f), (-0.3f, -0.55f), (-0.1f, -0.5f), (-0.12f, -0.72f)
    };

    /// <summary>A star of <paramref name="points"/> points between two radii, its first point at <paramref name="start"/> radians, centred at (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    private static (float x, float y)[] Star(int points, float outer, float inner, double start, float cx, float cy)
    {
        var p = new (float x, float y)[points * 2];
        for (int i = 0; i < p.Length; i++)
        {
            double a = start + i * Math.PI / points;
            float r = i % 2 == 0 ? outer : inner;
            p[i] = (cx + (float)(r * Math.Cos(a)), cy + (float)(r * Math.Sin(a)));
        }
        return p;
    }

    /// <summary>True when (<paramref name="px"/>, <paramref name="py"/>) lies in the ellipse of radii <paramref name="rx"/> along <paramref name="angle"/> and <paramref name="ry"/> across it, centred at (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    private static bool InRotatedEllipse(float cx, float cy, float rx, float ry, double angle, float px, float py)
    {
        double dx = px - cx, dy = py - cy, c = Math.Cos(angle), s = Math.Sin(angle);
        double u = (dx * c + dy * s) / rx, v = (-dx * s + dy * c) / ry;
        return u * u + v * v <= 1.0;
    }
}
