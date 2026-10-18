/// <summary>
/// The machine close-up's timing (Saleh's 1018a playtest: "you cant see what
/// each machiene does the camera should be better"; the desk layout spec §4):
/// when a machine is used (a paper dropped on the scanner or the chip reader,
/// an upgrade's object clicked), the camera pushes in on it and holds while it
/// works, then gives the view back. Settings' "Machine close-ups" (on by
/// default) turns it off; it never starts during a drag; any click or key
/// gives the view back at once (Input), so it never blocks the player; a
/// machine still working keeps it (Extend); another machine takes it over
/// (Start). How the camera moves (an ease, a cut under Reduced Motion) is
/// MachineCloseUp's. Pure; tested headless.
/// </summary>
public sealed class CloseUpTimeline
{
    private float _until;

    /// <summary>The machine shown close (its bay's id), or null while the view is the player's.</summary>
    public string Machine { get; private set; }

    /// <summary>True while a close-up holds.</summary>
    public bool Active => Machine != null;

    /// <summary>
    /// <paramref name="machine"/> was used at <paramref name="now"/>: the
    /// close-up shows it for <paramref name="hold"/> seconds (taking over
    /// from another machine's); false (nothing) with the setting off
    /// (<paramref name="enabled"/>), during a drag, or without a machine.
    /// </summary>
    public bool Start(string machine, bool enabled, bool dragging, float now, float hold)
    {
        if (!enabled || dragging || string.IsNullOrEmpty(machine))
            return false;
        Machine = machine;
        _until = now + System.Math.Max(0f, hold);
        return true;
    }

    /// <summary><paramref name="machine"/> still works at <paramref name="now"/>: its close-up holds at least <paramref name="hold"/> seconds more (nothing for another machine, or with none shown).</summary>
    public void Extend(string machine, float now, float hold)
    {
        if (Active && Machine == machine)
            _until = System.Math.Max(_until, now + System.Math.Max(0f, hold));
    }

    /// <summary>A click or a key: the view goes back at once; true when a close-up ended.</summary>
    public bool Input() => End();

    /// <summary>Time passes: true at the moment the hold runs out (the close-up ends).</summary>
    public bool Tick(float now) => Active && now >= _until && End();

    private bool End()
    {
        if (!Active)
            return false;
        Machine = null;
        return true;
    }
}
