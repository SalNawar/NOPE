/// <summary>What a press of the Start menu's Quit game does (QuitGuard.Press). Not serialized.</summary>
public enum QuitStep
{
    /// <summary>The game quits.</summary>
    Quit,

    /// <summary>The entry turns into the warning ("Quit? Today's shift is forfeit"); a second press within the window quits.</summary>
    Warn
}

/// <summary>
/// The mid-shift quit warning (BUG-35c, UI-PC; BUG-35's "diegetic warnings
/// for forfeit"): quitting while a shift runs forfeits the day, so the first
/// press of Quit game only warns, on the entry itself, and a second press
/// within <see cref="Seconds"/> quits; outside a shift (the briefing, the
/// report, a closed booth) one press quits. Times are the caller's clock
/// (unscaled seconds). Pure; DesktopShell applies it.
/// </summary>
public sealed class QuitGuard
{
    /// <summary>How long the warning stands for its confirming press (seconds).</summary>
    public readonly float Seconds;

    /// <summary>When the warning was shown (seconds), or none standing.</summary>
    private float? _warned;

    /// <summary>A guard whose warning stands <paramref name="seconds"/> (at least 0).</summary>
    public QuitGuard(float seconds) => Seconds = seconds < 0f ? 0f : seconds;

    /// <summary>True while a warning stands at <paramref name="now"/>.</summary>
    public bool Warning(float now) => _warned.HasValue && now >= _warned.Value && now - _warned.Value <= Seconds;

    /// <summary>A press at <paramref name="now"/>, with a shift running or not.</summary>
    public QuitStep Press(bool shiftRunning, float now)
    {
        if (!shiftRunning || Warning(now))
        {
            _warned = null;
            return QuitStep.Quit;
        }
        _warned = now;
        return QuitStep.Warn;
    }

    /// <summary>The warning is dropped (the menu closed).</summary>
    public void Reset() => _warned = null;
}
