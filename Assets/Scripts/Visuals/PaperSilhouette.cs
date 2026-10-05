using System;
using System.Collections.Generic;

/// <summary>
/// A paper's outline on the desk and on the PC (the travel documents spec,
/// TD1): a plain rectangle, or a card's with rounded corners. Points run round
/// the rectangle's centre (x right, y up, the caller's units), so a fan from
/// the centre fills it (every outline is convex). Pure, so it is tested
/// headless; the desk paper builds its mesh from it and the PC its sprite's
/// corner.
/// </summary>
public static class PaperSilhouette
{
    /// <summary>A card's corner radius, in H (the print unit: FormLayout.PrintUnit).</summary>
    public const float CardCorner = 0.035f;

    /// <summary>The points along each rounded corner (its two ends included).</summary>
    public const int CornerSteps = 6;

    /// <summary>The corner radius <paramref name="frame"/> rounds a page with print unit <paramref name="unit"/> by (0: square corners), at most a quarter of its shorter side (<paramref name="width"/>, <paramref name="height"/>).</summary>
    public static float Corner(FormFrame frame, float width, float height, float unit) =>
        frame == FormFrame.Card ? Math.Min(CardCorner * unit, Math.Min(width, height) / 4f) : 0f;

    /// <summary>The outline of a <paramref name="width"/> by <paramref name="height"/> page whose corners are rounded by <paramref name="radius"/> (0: its four corners), counter-clockwise from the bottom right corner's lower end, about the centre.</summary>
    public static List<(float x, float y)> Outline(float width, float height, float radius)
    {
        float hw = width / 2f, hh = height / 2f;
        var points = new List<(float x, float y)>();
        if (radius <= 0f)
        {
            points.Add((hw, -hh));
            points.Add((hw, hh));
            points.Add((-hw, hh));
            points.Add((-hw, -hh));
            return points;
        }
        (float cx, float cy, double start)[] corners =
        {
            (hw - radius, -hh + radius, -Math.PI / 2.0),
            (hw - radius, hh - radius, 0.0),
            (-hw + radius, hh - radius, Math.PI / 2.0),
            (-hw + radius, -hh + radius, Math.PI)
        };
        foreach ((float cx, float cy, double start) in corners)
            for (int i = 0; i <= CornerSteps; i++)
            {
                double a = start + i * (Math.PI / 2.0) / CornerSteps;
                points.Add((cx + (float)(radius * Math.Cos(a)), cy + (float)(radius * Math.Sin(a))));
            }
        return points;
    }
}
