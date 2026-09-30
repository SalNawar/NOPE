using System;
using System.Collections.Generic;

/// <summary>
/// How each seal outline is drawn (the document design spec, D4): a thick
/// outer band and a hairline inside it, in the outline's shape, the legend
/// printed in the middle by the renderers. The outlines are named as the
/// Domain's SealShape values (the EditMode suite checks every name has one).
/// Pure, so the drawing is tested headless; SealArt paints it into a texture.
/// </summary>
public static class SealOutlines
{
    /// <summary>The outer band's inner edge, as a share of the outline's size.</summary>
    public const float OuterBand = 0.9f;

    /// <summary>The hairline's outer and inner edges, as shares of the outline's size.</summary>
    public const float HairOuter = 0.82f, HairInner = 0.78f;

    /// <summary>Each polygon outline in [-1, 1] (y down); the circle and the ellipse are round (Round).</summary>
    private static readonly Dictionary<string, (float x, float y)[]> Polygons = new Dictionary<string, (float x, float y)[]>(StringComparer.OrdinalIgnoreCase)
    {
        ["Hexagon"] = Regular(6, 0f, 1f),
        ["Square"] = new[] { (-0.88f, -0.88f), (0.88f, -0.88f), (0.88f, 0.88f), (-0.88f, 0.88f) },
        ["Shield"] = new[] { (-0.9f, -0.95f), (0.9f, -0.95f), (0.9f, 0.1f), (0.55f, 0.62f), (0f, 1f), (-0.55f, 0.62f), (-0.9f, 0.1f) },
        ["Octagon"] = Regular(8, (float)(Math.PI / 8.0), 1f)
    };

    /// <summary>True when <paramref name="shape"/> names an outline (the circle or a polygon).</summary>
    public static bool Has(string shape) => Round(shape) > 0f || (shape != null && Polygons.ContainsKey(shape));

    /// <summary>A round outline's height over its width (the circle 1, the ellipse <see cref="EllipseHeight"/>); 0 for a polygon or an unknown name.</summary>
    private static float Round(string shape) =>
        string.Equals(shape, "Circle", StringComparison.OrdinalIgnoreCase) ? 1f : string.Equals(shape, "Ellipse", StringComparison.OrdinalIgnoreCase) ? EllipseHeight : 0f;

    /// <summary>The ellipse's height over its width.</summary>
    public const float EllipseHeight = 0.8f;

    /// <summary>True when (<paramref name="x"/>, <paramref name="y"/>) in [-1, 1] (y down) is inked: in the outer band or on the hairline of <paramref name="shape"/>'s outline; nothing for an unknown shape.</summary>
    public static bool Inked(string shape, float x, float y) =>
        Has(shape) && ((Inside(shape, x, y, 1f) && !Inside(shape, x, y, OuterBand)) || (Inside(shape, x, y, HairOuter) && !Inside(shape, x, y, HairInner)));

    /// <summary>True when the point lies inside the outline scaled by <paramref name="scale"/> about the centre.</summary>
    private static bool Inside(string shape, float x, float y, float scale)
    {
        float round = Round(shape);
        if (round > 0f)
            return PixelShapes.InEllipse(0f, 0f, scale, scale * round, x, y);
        return PixelShapes.InPolygon(Polygons[shape], x / scale, y / scale);
    }

    /// <summary>A regular polygon of <paramref name="sides"/> sides on a circle of <paramref name="radius"/>, its first corner at <paramref name="start"/> radians.</summary>
    private static (float x, float y)[] Regular(int sides, float start, float radius)
    {
        var points = new (float x, float y)[sides];
        for (int i = 0; i < sides; i++)
        {
            double a = start + i * 2.0 * Math.PI / sides;
            points[i] = ((float)(radius * Math.Cos(a)), (float)(radius * Math.Sin(a)));
        }
        return points;
    }
}
