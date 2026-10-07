using System;
using System.Collections.Generic;

/// <summary>
/// How far a control's animated face may reach past its rest rect on each
/// side (Saleh 2026-10-07, round 2: "they go out of bounds of their borders,
/// buttons overlap"): at most the side's own most (the kit's border inset; a
/// pull tab's slide on its open side), and never into a neighbour's rest
/// rect: the gap to the nearest neighbour on that side that faces it (one
/// whose span overlaps the control's across that side). A neighbour that
/// overlaps the control itself (a background, a frame) does not count. Pure,
/// so the rule is tested with every layout's spacing.
/// </summary>
public static class ControlRoom
{
    /// <summary>
    /// The room of <paramref name="rest"/> among <paramref name="neighbours"/>
    /// (their rest rects, the control's own left out or skipped as
    /// overlapping): each side's most (<paramref name="mostLeft"/> ...),
    /// lowered to the gap to the nearest neighbour facing that side, never
    /// below 0.
    /// </summary>
    public static void Of(FaceRect rest, IReadOnlyList<FaceRect> neighbours, float mostLeft, float mostRight, float mostBottom, float mostTop,
                          out float left, out float right, out float bottom, out float top)
    {
        left = Math.Max(0f, mostLeft);
        right = Math.Max(0f, mostRight);
        bottom = Math.Max(0f, mostBottom);
        top = Math.Max(0f, mostTop);
        if (neighbours == null)
            return;
        for (int i = 0; i < neighbours.Count; i++)
        {
            FaceRect n = neighbours[i];
            bool acrossX = n.YMin < rest.YMax && n.YMax > rest.YMin; // it faces the left or the right side
            bool acrossY = n.XMin < rest.XMax && n.XMax > rest.XMin; // it faces the bottom or the top
            if (acrossX && acrossY)
                continue; // it overlaps the control: a background or a frame, not a neighbour
            if (acrossX && n.XMin >= rest.XMax)
                right = Math.Min(right, n.XMin - rest.XMax);
            else if (acrossX && n.XMax <= rest.XMin)
                left = Math.Min(left, rest.XMin - n.XMax);
            else if (acrossY && n.YMin >= rest.YMax)
                top = Math.Min(top, n.YMin - rest.YMax);
            else if (acrossY && n.YMax <= rest.YMin)
                bottom = Math.Min(bottom, rest.YMin - n.YMax);
            else
            {
                // A diagonal neighbour: whichever side's gap is the wider keeps the face clear of its corner.
                float gx = n.XMin >= rest.XMax ? n.XMin - rest.XMax : rest.XMin - n.XMax;
                float gy = n.YMin >= rest.YMax ? n.YMin - rest.YMax : rest.YMin - n.YMax;
                if (gx >= gy)
                {
                    if (n.XMin >= rest.XMax)
                        right = Math.Min(right, gx);
                    else
                        left = Math.Min(left, gx);
                }
                else if (n.YMin >= rest.YMax)
                    top = Math.Min(top, gy);
                else
                    bottom = Math.Min(bottom, gy);
            }
        }
        left = Math.Max(0f, left);
        right = Math.Max(0f, right);
        bottom = Math.Max(0f, bottom);
        top = Math.Max(0f, top);
    }
}
