using System;

/// <summary>
/// Keeps papers out from behind the desk scanner (the desk-first redesign,
/// Saleh 2026-10-05, item 4: "scanner: sometimes things get stuck behind
/// it"). Every rectangle is on the desk plane in the view's frame: x to the
/// right, y away from the office camera (its level forward). The scanner
/// hides what lies under it and, behind it, a shadow as deep as its body
/// hides from the camera's height (its height over the tangent of the view's
/// elevation). A paper whose footprint reaches into that blocked area is
/// moved the shortest way out to the left, the right or the front (never
/// further back), with a gap, kept on the desk; where none of those is clear
/// it goes to the eject spot in front of the scanner. A scanned paper comes
/// back to where it was picked up only when that spot is clear, else to the
/// eject spot. Engine-free; DeskController applies it.
/// </summary>
public static class ScannerClearance
{
    /// <summary>The gap left between a moved paper and the scanner's blocked area, in metres.</summary>
    public const float Gap = 0.02f;

    /// <summary>The depth behind the scanner (metres along the view) that its body hides from a camera looking down at <paramref name="elevationDegrees"/> (0 for a camera looking straight down or a body with no height; capped at <paramref name="cap"/> for a camera near level).</summary>
    public static float Shadow(float bodyHeight, float elevationDegrees, float cap)
    {
        if (bodyHeight <= 0f || elevationDegrees >= 90f)
            return 0f;
        double tan = Math.Tan(Math.Max(1f, elevationDegrees) * Math.PI / 180.0);
        return (float)Math.Min(Math.Max(0f, cap), bodyHeight / tan);
    }

    /// <summary>The area the scanner blocks: its footprint and its <paramref name="shadow"/> behind it (further from the camera, +y).</summary>
    public static DeskRect Blocked(DeskRect scanner, float shadow) =>
        new DeskRect(scanner.CentreX, scanner.CentreY + Math.Max(0f, shadow) / 2f, scanner.Width, scanner.Height + Math.Max(0f, shadow));

    /// <summary>True when two rectangles share more than an edge.</summary>
    public static bool Overlaps(DeskRect a, DeskRect b) =>
        Math.Abs(a.CentreX - b.CentreX) * 2f < a.Width + b.Width && Math.Abs(a.CentreY - b.CentreY) * 2f < a.Height + b.Height;

    /// <summary>
    /// Where a paper lying over <paramref name="paper"/> should lie instead: its
    /// own centre when it is clear of the scanner's blocked area, else the
    /// nearest clear place to its left, right or front (ties go to the front),
    /// with its centre kept in <paramref name="area"/> (the desk's clamp area),
    /// else the eject spot.
    /// </summary>
    public static (float x, float y) Clear(DeskRect paper, DeskRect scanner, float shadow, DeskRect area)
    {
        DeskRect blocked = Blocked(scanner, shadow);
        if (!Overlaps(paper, blocked))
            return (paper.CentreX, paper.CentreY);

        float halfW = paper.Width / 2f, halfH = paper.Height / 2f;
        float left = blocked.CentreX - blocked.Width / 2f - Gap - halfW;
        float right = blocked.CentreX + blocked.Width / 2f + Gap + halfW;
        float front = blocked.CentreY - blocked.Height / 2f - Gap - halfH;
        var candidates = new[]
        {
            (x: paper.CentreX, y: front),
            (x: left, y: paper.CentreY),
            (x: right, y: paper.CentreY),
        };

        bool found = false;
        (float x, float y) best = default;
        float bestMove = float.MaxValue;
        foreach ((float x, float y) c in candidates)
        {
            (float x, float y) kept = area.Clamp(c.x, c.y);
            if (Overlaps(new DeskRect(kept.x, kept.y, paper.Width, paper.Height), blocked))
                continue;
            float move = Math.Abs(kept.x - paper.CentreX) + Math.Abs(kept.y - paper.CentreY);
            if (move < bestMove - 1e-5f)
            {
                best = kept;
                bestMove = move;
                found = true;
            }
        }
        return found ? best : Eject(paper.Width, paper.Height, scanner, area);
    }

    /// <summary>
    /// The eject spot for a paper of <paramref name="width"/> by
    /// <paramref name="depth"/>: just in front of the scanner (toward the
    /// camera), centred on its width; where the desk ends before that (the
    /// spot, kept in <paramref name="area"/>, would still touch the scanner),
    /// beside its left edge (the mat's side), else its right; the front spot
    /// when nothing is clear.
    /// </summary>
    public static (float x, float y) Eject(float width, float depth, DeskRect scanner, DeskRect area)
    {
        var candidates = new[]
        {
            area.Clamp(scanner.CentreX, scanner.CentreY - scanner.Height / 2f - Gap - depth / 2f),
            area.Clamp(scanner.CentreX - scanner.Width / 2f - Gap - width / 2f, scanner.CentreY),
            area.Clamp(scanner.CentreX + scanner.Width / 2f + Gap + width / 2f, scanner.CentreY),
        };
        foreach ((float x, float y) c in candidates)
            if (!Overlaps(new DeskRect(c.x, c.y, width, depth), scanner))
                return c;
        return candidates[0];
    }
}
