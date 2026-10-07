#if UNITY_EDITOR || DEVELOPMENT_BUILD || DEMO_CHEATS
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The hall slots' cheats in the cheat menu's "More (other tracks)"
/// (DevCheats.Register; the hall slots spec): each "Hall:" button steps one
/// of the hall's variables through its values (the run's own first, then
/// each value: HallSlotsDev's overrides), "Hall: the run's" clears them all,
/// "Hall: stand-ins" turns on or off the stand-in of every slot whose art is
/// missing (off by default). The hall swaps at
/// once and logs its state and picks.
/// </summary>
public static class HallSlotsCheats
{
    /// <summary>Registers the cheats once, at startup.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Register()
    {
        DevCheats.Register("Hall: culture", run => { HallSlotsDev.Culture = Step(HallSlotsDev.Culture, Cultures(run)); Report(); });
        DevCheats.Register("Hall: tier", run => { HallSlotsDev.Tier = StepEnum(HallSlotsDev.Tier); Report(); });
        DevCheats.Register("Hall: phase", run => { HallSlotsDev.Phase = StepEnum(HallSlotsDev.Phase); Report(); });
        DevCheats.Register("Hall: event", run => { HallSlotsDev.Event = StepEnum(HallSlotsDev.Event); Report(); });
        DevCheats.Register("Hall: exhibit", run => { HallSlotsDev.Exhibit = Step(HallSlotsDev.Exhibit, Exhibits(run)); Report(); });
        DevCheats.Register("Hall: the run's", run => { HallSlotsDev.Clear(); Report(); });
        DevCheats.Register("Hall: stand-ins", run => { HallSlotsDev.ShowAllStandIns = !HallSlotsDev.ShowAllStandIns; Report(); });
    }

    /// <summary>"neutral" then the content's nations.</summary>
    private static List<string> Cultures(RunManager run)
    {
        var values = new List<string> { HallStates.Neutral };
        if (run != null && run.Library != null)
            foreach (NationSO n in run.Library.Nations)
                if (n != null && !string.IsNullOrWhiteSpace(n.id))
                    values.Add(n.id);
        return values;
    }

    /// <summary>"none" then the content's nations.</summary>
    private static List<string> Exhibits(RunManager run)
    {
        List<string> values = Cultures(run);
        values[0] = HallStates.NoExhibit;
        return values;
    }

    /// <summary>The value after <paramref name="current"/> (null, the run's, comes first and after the last).</summary>
    private static string Step(string current, List<string> values)
    {
        int at = current == null ? -1 : values.IndexOf(current);
        return at + 1 < values.Count ? values[at + 1] : null;
    }

    /// <summary>The enum value after <paramref name="current"/> (null, the run's, comes first and after the last).</summary>
    private static T? StepEnum<T>(T? current) where T : struct, System.Enum
    {
        var values = (T[])System.Enum.GetValues(typeof(T));
        int at = current.HasValue ? System.Array.IndexOf(values, current.Value) : -1;
        return at + 1 < values.Length ? values[at + 1] : (T?)null;
    }

    /// <summary>Has the hall re-read its state next frame (its link logs the state and the picks) and logs the overrides.</summary>
    private static void Report()
    {
        HallSlotsDev.Changed();
        HallSlotsLink link = HallSlotsLink.Live;
        Debug.Log($"[HallSlotsCheats] overrides: stand-ins {(HallSlotsDev.ShowAllStandIns ? "on" : "off")}, culture={HallSlotsDev.Culture ?? "run"} tier={(HallSlotsDev.Tier?.ToString() ?? "run")} phase={(HallSlotsDev.Phase?.ToString() ?? "run")} event={(HallSlotsDev.Event?.ToString() ?? "run")} exhibit={HallSlotsDev.Exhibit ?? "run"}; the hall {(link != null ? "re-reads its state" : "is not loaded")}.");
    }
}
#endif
