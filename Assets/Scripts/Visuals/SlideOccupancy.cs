/// <summary>
/// A machine's occupancy as its outside shows it (DESK-HOMES P2, "busy or
/// occupied stays out"; the stand-in read until DOC-HANDLE exposes the
/// scanner's own state): it is busy while its scan light shows (working), or
/// while its lid stands open with no paper held (a paper on its way onto the
/// bed, or a scan just ended with the next queued paper to follow), and stays
/// so until the lid has shut and the light is out. A lid opened for a paper
/// the player holds near it is not yet an occupant: the slide must still come
/// to help the drop. Pure; OfficeSceneBinder feeds it the scanner's sweep
/// bar, spill and lid.
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
}
