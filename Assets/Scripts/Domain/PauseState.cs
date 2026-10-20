using System;
using System.Collections.Generic;

/// <summary>How much a pause source stops (PauseState). Not serialized.</summary>
public enum PauseScope
{
    /// <summary>The shift clock only (with slot arming and countdowns that read it): audio, input and animation keep running (the booth closed with the AVAILABLE sign, a citation slip).</summary>
    Clock,

    /// <summary>
    /// The clock and the audio (Settings open, the window's focus lost). It
    /// blocks no input itself: Settings in the office is the PC's window, and the
    /// open PC already takes the office's input from the desk (BoothCoordinator);
    /// the focus lost delivers no input and cancels the desk's hold
    /// (HoldRegistry.OnFocusLost); a Settings window outside the PC (HOME's) is
    /// modal itself.
    /// </summary>
    Full
}

/// <summary>
/// The game's pause, as a set of named sources (FLOW, BUG-34; FIX_PLAN_v2
/// FLOW delta: "no pause card"; BELL-SIGN P2's scoped requirement). Each
/// source asks for a scope (Request) and lets go of it (Release). The shift
/// clock runs only while no source holds it (<see cref="ClockHeld"/>);
/// the audio stops too while a Full source holds (<see cref="FullHeld"/>).
/// One source's release never restarts a clock another source holds, in any
/// order. Pure; the game uses <see cref="Shared"/> (ShiftClockDriver reads
/// the clock; PauseMenu holds Settings and the focus and pauses the audio;
/// GameManager holds a citation slip; BELL-SIGN's closed booth is a Clock
/// source of its own).
/// </summary>
public sealed class PauseState
{
    /// <summary>Settings is open (Esc, or the PC's Settings app): Full.</summary>
    public const string Settings = "settings";

    /// <summary>The game's window lost the focus (only when the game does not run in the background): Full.</summary>
    public const string Focus = "focus";

    /// <summary>A citation slip holds the day until it is acknowledged: Clock.</summary>
    public const string Citation = "citation";

    /// <summary>The game's one pause (cleared when play starts).</summary>
    public static PauseState Shared { get; } = new PauseState();

    /// <summary>The sources holding now, with their scopes.</summary>
    private readonly Dictionary<string, PauseScope> _sources = new Dictionary<string, PauseScope>(StringComparer.Ordinal);

    /// <summary>True while any source holds the shift clock (every scope does).</summary>
    public bool ClockHeld => _sources.Count > 0;

    /// <summary>True while a Full source holds (the audio stops too: PauseMenu).</summary>
    public bool FullHeld
    {
        get
        {
            foreach (PauseScope scope in _sources.Values)
                if (scope == PauseScope.Full)
                    return true;
            return false;
        }
    }

    /// <summary>How many sources hold now.</summary>
    public int Count => _sources.Count;

    /// <summary>Raised after <see cref="ClockHeld"/> or <see cref="FullHeld"/> changed.</summary>
    public event Action Changed;

    /// <summary>True while <paramref name="source"/> holds.</summary>
    public bool Holds(string source) => source != null && _sources.ContainsKey(source);

    /// <summary><paramref name="source"/> holds at <paramref name="scope"/> (asked again: one hold, at the newer scope).</summary>
    /// <exception cref="ArgumentException">The source has no name.</exception>
    public void Request(string source, PauseScope scope)
    {
        if (string.IsNullOrEmpty(source))
            throw new ArgumentException("A pause source needs a name.", nameof(source));
        Change(() => _sources[source] = scope);
    }

    /// <summary><paramref name="source"/> lets go (nothing when it does not hold).</summary>
    public void Release(string source)
    {
        if (source != null && _sources.ContainsKey(source))
            Change(() => _sources.Remove(source));
    }

    /// <summary>Every source lets go (a new play session).</summary>
    public void Clear()
    {
        if (_sources.Count > 0)
            Change(_sources.Clear);
    }

    /// <summary>
    /// True when losing the window's focus pauses the game (Full): only when
    /// the game does not run in the background (<paramref name="runInBackground"/>;
    /// the player ships with it off, while the QA rig and the editor's job
    /// runner turn it on so a run in an unfocused editor keeps playing).
    /// </summary>
    public static bool PausesOnFocusLoss(bool runInBackground) => !runInBackground;

    /// <summary>Applies <paramref name="edit"/> and raises Changed when the held state changed.</summary>
    private void Change(Action edit)
    {
        bool clock = ClockHeld, full = FullHeld;
        edit();
        if (clock != ClockHeld || full != FullHeld)
            Changed?.Invoke();
    }
}
