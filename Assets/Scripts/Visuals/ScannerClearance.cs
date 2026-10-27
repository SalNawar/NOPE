using System;

/// <summary>
/// The area the desk scanner hides papers in (the desk-first redesign, Saleh
/// 2026-10-05, item 4: "scanner: sometimes things get stuck behind it").
/// Every rectangle is on the desk plane in the view's frame: x to the right,
/// y away from the office camera (its level forward). The scanner hides what
/// lies under it and, behind it, a shadow as deep as its body hides from the
/// camera's height (its height over the tangent of the view's elevation).
/// DeskController keeps that blocked area among a resting paper's keep-outs
/// (DropResolver: the least move clear of it and every other obstacle at
/// once, DOC-HANDLE BUG-6; the old one-at-a-time push out to the left, right,
/// front or eject spot is gone). Engine-free.
/// </summary>
public static class ScannerClearance
{
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
}
