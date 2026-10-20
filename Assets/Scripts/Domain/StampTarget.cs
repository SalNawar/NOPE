using System.Collections.Generic;

/// <summary>What a dater's aim meets (StampTarget.Pick): a document on the desk, or the rulebook folder.</summary>
public enum StampTargetKind
{
    /// <summary>A paper of the case (the passport takes the verdict; any other is refused).</summary>
    Document,

    /// <summary>The rulebook folder (it never takes a stamp).</summary>
    Folder
}

/// <summary>One thing the view's ray through a dater's aim meets (StampTarget.Pick).</summary>
public readonly struct StampCandidate
{
    /// <summary>One thing met: <paramref name="distance"/> along the view's ray (nearer lies on top, as the player sees it), what it is, and <paramref name="edgeDistance"/>, how far inside its outline the ray met it (metres to its nearest edge on the desk).</summary>
    public StampCandidate(float distance, StampTargetKind kind, float edgeDistance)
    {
        Distance = distance;
        Kind = kind;
        EdgeDistance = edgeDistance;
    }

    /// <summary>How far along the view's ray it was met (nearer: on top).</summary>
    public float Distance { get; }

    /// <summary>What it is.</summary>
    public StampTargetKind Kind { get; }

    /// <summary>How far inside its outline the ray met it (metres to its nearest edge on the desk; 0 on the edge).</summary>
    public float EdgeDistance { get; }
}

/// <summary>
/// Where a dater lands (BUG-40a; Saleh 2026-10-06: "the stamp mark must land
/// exactly where the stamp is pressed"): the top-most thing the player sees
/// under its aim (the pointer's screen point, which the carried die hangs
/// straight above: the nearest along the view's ray), except that a folder's
/// edge does not swallow the stamp when a document lies under that edge (day
/// 12: the moved folder over the passport's corner refused the stamp aimed at
/// the passport). The stroke drops the die exactly onto the surface it met,
/// not to a fixed height over the desk (a stack, the folder: no sink). Pure;
/// DeskStampTray casts the ray and applies it.
/// </summary>
public static class StampTarget
{
    /// <summary>
    /// The index in <paramref name="under"/> of what the stamp lands on: the
    /// nearest along the view's ray, passing over a Folder met within
    /// <paramref name="folderEdge"/> metres of its edge when a Document lies
    /// beyond it on the ray; -1 when nothing was met (the bare desk). The
    /// list may come in any order.
    /// </summary>
    public static int Pick(IReadOnlyList<StampCandidate> under, float folderEdge)
    {
        if (under == null || under.Count == 0)
            return -1;
        var order = new List<int>(under.Count);
        for (int i = 0; i < under.Count; i++)
            order.Add(i);
        order.Sort((a, b) => under[a].Distance != under[b].Distance ? under[a].Distance.CompareTo(under[b].Distance) : a.CompareTo(b));
        for (int k = 0; k < order.Count; k++)
        {
            StampCandidate c = under[order[k]];
            if (c.Kind == StampTargetKind.Folder && c.EdgeDistance < folderEdge && DocumentBeyond(under, order, k))
                continue;
            return order[k];
        }
        return -1;
    }

    /// <summary>How far a stroke drops the die from <paramref name="dieHeight"/> (its foot's height over the desk, metres) so its foot stands on the surface it met at <paramref name="surfaceHeight"/>; 0 when it is there already.</summary>
    public static float StrokeDrop(float dieHeight, float surfaceHeight) => dieHeight > surfaceHeight ? dieHeight - surfaceHeight : 0f;

    private static bool DocumentBeyond(IReadOnlyList<StampCandidate> under, List<int> order, int k)
    {
        for (int j = k + 1; j < order.Count; j++)
            if (under[order[j]].Kind == StampTargetKind.Document)
                return true;
        return false;
    }
}

/// <summary>
/// The ink a dater's stroke owes the passport (BUG-40a: a re-grab mid-stroke
/// recorded the verdict with no ink): owed from the press that records the
/// verdict (StampFlow.Press), it lands exactly once, as the stroke bottoms out
/// or, if anything cuts the stroke short first, at the cut. Pure; one per
/// dater, DeskStampTray applies it.
/// </summary>
public sealed class StampInk
{
    /// <summary>True while a print is owed (the verdict is recorded, its ink not printed yet).</summary>
    public bool Owed { get; private set; }

    /// <summary>The press recorded the verdict: its print is owed.</summary>
    public void Owe() => Owed = true;

    /// <summary>The print lands now (the stroke bottomed out or was cut): true once for each owed print, false when none is owed.</summary>
    public bool Land()
    {
        if (!Owed)
            return false;
        Owed = false;
        return true;
    }
}
