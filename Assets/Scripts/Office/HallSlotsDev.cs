/// <summary>
/// The dev overrides of the hall's variables (the cheat menu's "Hall:"
/// buttons, HallSlotsCheats, and the Capture Combinations tool): each set
/// value replaces the run's when HallSlotsLink reads the hall's state; null
/// keeps the run's. Nothing sets them in a release build, so the hall then
/// always shows the run.
/// </summary>
public static class HallSlotsDev
{
    /// <summary>The culture shown ("neutral" or a nation id), or null for the run's.</summary>
    public static string Culture;

    /// <summary>The tier shown, or null for the Helix River's.</summary>
    public static StabilityTier? Tier;

    /// <summary>The phase shown, or null for the day's.</summary>
    public static HallPhase? Phase;

    /// <summary>Today's special shown, or null for the day plan's.</summary>
    public static HallEvent? Event;

    /// <summary>The famous nation shown as both the strongest and the latest exhibit ("none" or a nation id; it also counts one let through), or null for the run's.</summary>
    public static string Exhibit;

    /// <summary>True to draw the stand-in of every slot whose picked art is missing (off by default: no stand-ins in play unless the cheat asks).</summary>
    public static bool ShowAllStandIns;

    /// <summary>Counts the changes, so the link re-reads the state at the next frame.</summary>
    public static int Version { get; private set; }

    /// <summary>Call after changing a value: the hall re-reads its state.</summary>
    public static void Changed() => Version++;

    /// <summary>Clears every override (the hall shows the run again).</summary>
    public static void Clear()
    {
        Culture = null;
        Tier = null;
        Phase = null;
        Event = null;
        Exhibit = null;
        Changed();
    }

    /// <summary>The overrides applied to <paramref name="state"/> (in place).</summary>
    public static void Apply(HallState state)
    {
        if (Culture != null)
            state.Culture = Culture == HallStates.Neutral ? null : Culture;
        if (Tier.HasValue)
            state.Tier = Tier.Value;
        if (Phase.HasValue)
            state.Phase = Phase.Value;
        if (Event.HasValue)
            state.Event = Event.Value;
        if (Exhibit != null)
        {
            bool none = Exhibit == HallStates.NoExhibit;
            state.StrongestExhibit = none ? null : Exhibit;
            state.RecentExhibit = none ? null : Exhibit;
            state.Exhibits = none ? new HallExhibit[0] : new[] { new HallExhibit(Exhibit, 1) };
        }
    }
}
