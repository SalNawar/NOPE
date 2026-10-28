using System;

/// <summary>
/// The rulebook folder's home on the painted desk (Saleh's playtest of
/// 2026-10-28, point 5: "isnt the big red binder for the folder? why is the
/// folder by default on the desk? still no place for it"; DESK-PLAN's
/// rulebookPocket home, the painting's red book in its pocket left of the
/// mat; DESK-V3's open call for DESK-HOMES P4: the painted book and the open
/// folder are one book). The folder starts each day in its pocket (the red
/// book shows) and the mat starts empty; a click on the book brings it out
/// onto the mat, a drag from the book takes it out under the pointer, and a
/// folder let go on the pocket goes back in. In the pocket the folder lies
/// under the painted book, shrunk to fit it (Fit). Pure.
/// </summary>
public static class RulebookPocket
{
    /// <summary>How far round the pocket's footprint a let-go still counts as on it (metres): the pocket is small beside the folder.</summary>
    public const float Reach = 0.05f;

    /// <summary>The least share of its size the folder shrinks to in the pocket.</summary>
    public const float LeastFit = 0.2f;

    /// <summary>The share of the book's footprint the shrunk folder covers at most (it lies hidden under the book, never past its edge).</summary>
    public const float Under = 0.85f;

    /// <summary>True when a folder let go with the pointer at (<paramref name="pointerX"/>, <paramref name="pointerY"/>) or its footprint's centre at (<paramref name="centreX"/>, <paramref name="centreY"/>) (the desk's frame, metres) goes into the pocket whose footprint is <paramref name="pocket"/>: either lies on it, grown by Reach.</summary>
    public static bool Takes(DeskRect pocket, float pointerX, float pointerY, float centreX, float centreY)
    {
        if (pocket.Width <= 0f || pocket.Height <= 0f)
            return false;
        var grown = new DeskRect(pocket.CentreX, pocket.CentreY, pocket.Width + 2f * Reach, pocket.Height + 2f * Reach);
        return grown.Contains(pointerX, pointerY) || grown.Contains(centreX, centreY);
    }

    /// <summary>The share of its size the folder (<paramref name="folderWidth"/> by <paramref name="folderDepth"/> on the desk) takes in the pocket, so it lies within Under of the book's footprint (<paramref name="bookWidth"/> by <paramref name="bookDepth"/>), never under LeastFit and never over 1 (it never grows).</summary>
    public static float Fit(float bookWidth, float bookDepth, float folderWidth, float folderDepth)
    {
        if (folderWidth <= 0f || folderDepth <= 0f || bookWidth <= 0f || bookDepth <= 0f)
            return 1f;
        float fit = Under * Math.Min(bookWidth / folderWidth, bookDepth / folderDepth);
        return Math.Max(LeastFit, Math.Min(1f, fit));
    }

    /// <summary>The eased share (0 out at its own size, 1 in the pocket at its fit) of a move into or out of the pocket <paramref name="t"/> of the way through (smoothstep: it starts and lands with no jolt), clamped.</summary>
    public static float Ease(float t)
    {
        float x = Math.Max(0f, Math.Min(1f, t));
        return x * x * (3f - 2f * x);
    }
}
