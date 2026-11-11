using System;
using System.Collections.Generic;

/// <summary>
/// The grab pad of a paper that lies on the counter (Saleh 2026-11-11, "game
/// overall feels unplayable"): the counter shows a paper small (27 x 10 px for a
/// passport at 1920 x 1080), so its press box grows to a least size on the
/// screen. Where two papers lie close, each pad stops at half the way to the
/// other's centre, so the nearest centre wins. Pure: screen pixels in, pixels out.
/// </summary>
public static class GrabPad
{
    /// <summary>
    /// The pad's half size (px, x and y) of a paper with its centre at (<paramref name="cx"/>, <paramref name="cy"/>)
    /// and half sheet size (<paramref name="sheetHx"/>, <paramref name="sheetHy"/>): at least <paramref name="wantHalf"/>
    /// and at least <paramref name="margin"/> past the sheet on each side (never less than the sheet). A paper in <paramref name="others"/> (centres of the other papers) limits
    /// the axis it lies along most to half the way to it, but never below the sheet.
    /// </summary>
    public static (float hx, float hy) Half(float wantHalf, float margin, float sheetHx, float sheetHy, float cx, float cy, IReadOnlyList<(float x, float y)> others)
    {
        float hx = Math.Max(sheetHx + margin, wantHalf);
        float hy = Math.Max(sheetHy + margin, wantHalf);
        if (others == null)
            return (hx, hy);
        for (int i = 0; i < others.Count; i++)
        {
            float dx = Math.Abs(others[i].x - cx);
            float dy = Math.Abs(others[i].y - cy);
            if (dx >= dy)
                hx = Math.Min(hx, Math.Max(sheetHx, dx / 2f));
            else
                hy = Math.Min(hy, Math.Max(sheetHy, dy / 2f));
        }
        return (hx, hy);
    }
}
