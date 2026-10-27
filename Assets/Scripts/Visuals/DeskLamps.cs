using System;

/// <summary>Where the case at the desk stands, as the next-tool lamp reads it (NextToolLamp).</summary>
public readonly struct CaseLampState
{
    /// <summary>True while a traveller stands at the desk.</summary>
    public readonly bool TravellerAtDesk;

    /// <summary>True while the booth takes travellers (between them, the bell calls the next).</summary>
    public readonly bool ShiftOpen;

    /// <summary>True once the passport carries the verdict's stamp (it goes back over the deal tray next).</summary>
    public readonly bool Stamped;

    /// <summary>True while a paper waits for the chip reader (FEEDBACK fills it; false until then).</summary>
    public readonly bool ChipOwed;

    /// <summary>True while a paper waits for the scanner (FEEDBACK fills it; false until then).</summary>
    public readonly bool ScanOwed;

    /// <summary>A case's state.</summary>
    public CaseLampState(bool travellerAtDesk, bool shiftOpen, bool stamped, bool chipOwed = false, bool scanOwed = false)
    {
        TravellerAtDesk = travellerAtDesk;
        ShiftOpen = shiftOpen;
        Stamped = stamped;
        ChipOwed = chipOwed;
        ScanOwed = scanOwed;
    }
}

/// <summary>
/// C's "next tool lights up" (Saleh, DESK APPROVED: "Mix: B + A rail + C
/// lamps"): the home whose lamp shows the case's next step, along the case
/// loop of the approved plan (proposal, ours): between travellers the bell;
/// a paper owed to a machine, that machine (the chip reader before the
/// scanner, the rail's order of use); a stamped passport, the deal tray it
/// goes back over; otherwise none (the player reads on the mat). Pure; the
/// booth coordinator feeds it and DeskLamps holds the answer for the
/// renderers (FEEDBACK, DESK-HOMES).
/// </summary>
public static class NextToolLamp
{
    /// <summary>The home id whose lamp lights for <paramref name="state"/>, or null for none.</summary>
    public static string For(CaseLampState state)
    {
        if (!state.TravellerAtDesk)
            return state.ShiftOpen ? DeskHomeMap.Bell : null;
        if (state.Stamped)
            return DeskHomeMap.DealTray;
        if (state.ChipOwed)
            return DeskHomeMap.ChipReader;
        return state.ScanOwed ? DeskHomeMap.Scanner : null;
    }
}

/// <summary>
/// The desk's next-tool lamp (the C lamps' data hook for FEEDBACK and
/// DESK-HOMES): which home's lamp is lit (one at a time, or none) and an
/// event when it changes. Where a lamp sits is its home's DeskHome.LampPoint;
/// the lamp's look, light and sound are its renderer's.
/// </summary>
public sealed class DeskLamps
{
    /// <summary>The lit home's id, or null.</summary>
    public string Lit { get; private set; }

    /// <summary>Raised with the newly lit home's id (null: all out) when it changes.</summary>
    public event Action<string> Changed;

    /// <summary>Lights <paramref name="homeId"/>'s lamp (null: all out); true when that changed anything.</summary>
    public bool Show(string homeId)
    {
        if (string.IsNullOrEmpty(homeId))
            homeId = null;
        if (homeId == Lit)
            return false;
        Lit = homeId;
        Changed?.Invoke(homeId);
        return true;
    }
}
