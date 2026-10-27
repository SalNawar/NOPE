using System.Collections.Generic;

/// <summary>
/// A machine's occupancy as its outside shows it (DESK-HOMES P2, "busy or
/// occupied stays out"; the stand-in read until DOC-HANDLE exposes the
/// scanner's own state): it is busy while its scan light shows (working), or
/// while its lid stands open with no paper held (a paper on its way onto the
/// bed, or a scan just ended with the next queued paper to follow), and stays
/// so until the lid has shut and the light is out. A lid opened for a paper
/// the player holds near it is not yet an occupant: the slide must still come
/// to help the drop. Pure; OfficeSceneBinder feeds it the scanner's sweep
/// bar, spill and lid. It also names the paper docked in a machine (Rider).
/// </summary>
public sealed class SlideOccupancy
{
    private bool _busy;

    /// <summary>The frame's look: <paramref name="working"/> (its scan light shows), <paramref name="open"/> (its lid stands open) and <paramref name="holding"/> (the player drags a paper). Returns true while the machine is occupied.</summary>
    public bool Step(bool working, bool open, bool holding)
    {
        if (working || (open && !holding))
            _busy = true;
        else if (!open)
            _busy = false;
        return _busy;
    }

    /// <summary>
    /// Which of the desk's <paramref name="papers"/> is the one docked in a
    /// machine, to ride its slide home in a forced retract (MachineSlideIn's
    /// TakeRider; DESK-HOMES P2, one mover): among those lying still inside
    /// the machine's drop area, the nearest to where its dock puts a paper
    /// (each paper's <c>distance</c> to its pad, bed or slot point), so a
    /// paper merely lying beside the machine never rides while the docked one
    /// is left behind; -1 when none lies still there (a paper still on its
    /// own carry has that carry as its only mover).
    /// </summary>
    public static int Rider(IReadOnlyList<(float distance, bool inside, bool still)> papers)
    {
        int best = -1;
        float nearest = float.PositiveInfinity;
        int count = papers != null ? papers.Count : 0;
        for (int i = 0; i < count; i++)
        {
            (float distance, bool inside, bool still) = papers[i];
            if (inside && still && distance < nearest)
            {
                best = i;
                nearest = distance;
            }
        }
        return best;
    }
}
