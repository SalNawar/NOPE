using System;

/// <summary>Where one of the current traveller's papers is, as the Investigation app shows it (the PC redesign AP6).</summary>
public enum PaperState
{
    /// <summary>Still with the traveller: the desk has to ask for it.</summary>
    NotHandedOver,

    /// <summary>Handed over onto the desk, not scanned yet.</summary>
    OnDesk,

    /// <summary>Scanned: its copy is on the PC.</summary>
    Scanned
}

/// <summary>
/// The current traveller's papers as the Investigation app counts them (the
/// PC redesign AP1's counters "Papers 2 of 3 received · 1 scanned", AP6's
/// chips): each is not handed over, on the desk, or scanned. A paper is
/// handed over once and scanned once; one that reaches the PC without a desk
/// (straight to its scanned copy) counts as received. Indices outside the
/// case change nothing. Pure; CaseDocumentsPresenter keeps one per case.
/// </summary>
public sealed class CasePapers
{
    private readonly PaperState[] _states;

    /// <summary>A case of <paramref name="count"/> papers, none handed over (a negative count is none).</summary>
    public CasePapers(int count)
    {
        _states = new PaperState[Math.Max(0, count)];
    }

    /// <summary>The number of papers the traveller carries.</summary>
    public int Count => _states.Length;

    /// <summary>The papers handed over (on the desk or scanned).</summary>
    public int Received { get; private set; }

    /// <summary>The papers scanned.</summary>
    public int Scanned { get; private set; }

    /// <summary>Where paper <paramref name="index"/> is (NotHandedOver outside the case).</summary>
    public PaperState State(int index) => Inside(index) ? _states[index] : PaperState.NotHandedOver;

    /// <summary>Paper <paramref name="index"/> is handed over onto the desk; true when that changed its state.</summary>
    public bool HandOver(int index)
    {
        if (!Inside(index) || _states[index] != PaperState.NotHandedOver)
            return false;
        _states[index] = PaperState.OnDesk;
        Received++;
        return true;
    }

    /// <summary>Paper <paramref name="index"/> is scanned (handed over first when it was not); true when that changed its state.</summary>
    public bool Scan(int index)
    {
        if (!Inside(index) || _states[index] == PaperState.Scanned)
            return false;
        if (_states[index] == PaperState.NotHandedOver)
            Received++;
        _states[index] = PaperState.Scanned;
        Scanned++;
        return true;
    }

    private bool Inside(int index) => index >= 0 && index < _states.Length;
}
