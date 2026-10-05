using System.Collections.Generic;

/// <summary>Where a paper the clerk may ask for stands: not flagged, flagged missing (its request unlocked on the wheel), or asked for and not carried.</summary>
public enum MissingPaperState
{
    /// <summary>Not flagged.</summary>
    None,

    /// <summary>Flagged missing: the wheel offers "Hand me your ..." (InterviewUnlocks.Missing).</summary>
    Flagged,

    /// <summary>Asked for, and the traveller said they do not carry it (FindingKind.PaperMissing logged).</summary>
    NotCarried
}

/// <summary>
/// The papers the clerk can flag missing for the traveller at the desk (the
/// desk-first redesign, Saleh 2026-10-05, item 7: "even asking for a
/// document must be highlighted by flagging missing on the app"): one per
/// request of the day's papers menu (FormRequests.Build over the day's
/// askable forms, so only papers the ramp has introduced), the same list
/// for every traveller whatever they carry (nothing tells the clerk what
/// they hold back), until the paper is handed over. Flagging one unlocks its
/// request on the traveller wheel (InterviewUnlocks.Missing); asked for and
/// not carried, it stays NotCarried for the case. The PC's Papers menu and
/// the desk read the same list. Pure; one per case.
/// </summary>
public sealed class MissingPapers
{
    /// <summary>No traveller: nothing to flag.</summary>
    public static readonly MissingPapers None = new MissingPapers(null, null);

    private readonly List<FormRequest> _requests = new List<FormRequest>();
    private readonly Dictionary<string, MissingPaperState> _states = new Dictionary<string, MissingPaperState>();

    /// <summary>
    /// The flaggable requests among <paramref name="requests"/> (the
    /// traveller's FormRequests.Build): those of the day's papers menu
    /// <paramref name="askable"/> (a form's or its group's id), in order. A
    /// paper the traveller carries outside the menu (not introduced yet) is
    /// never offered.
    /// </summary>
    public MissingPapers(IReadOnlyList<AskableForm> askable, IReadOnlyList<FormRequest> requests)
    {
        var menu = new HashSet<string>();
        foreach (AskableForm f in askable ?? System.Array.Empty<AskableForm>())
            if (f != null && f.Requested)
                menu.Add(FormRequests.IdOf(f.AskGroup, f.FormNumber) ?? string.Empty);
        foreach (FormRequest r in requests ?? System.Array.Empty<FormRequest>())
            if (r != null && menu.Contains(r.Id ?? string.Empty))
                _requests.Add(r);
    }

    /// <summary>Every flaggable request, in the menu's order.</summary>
    public IReadOnlyList<FormRequest> Requests => _requests;

    /// <summary>
    /// The requests still open: not handed over (no carried paper, or its
    /// paper not received yet by <paramref name="papers"/>), in order. A
    /// handed-over paper is on the desk, so there is nothing to flag.
    /// </summary>
    public List<FormRequest> Open(CasePapers papers)
    {
        var open = new List<FormRequest>();
        foreach (FormRequest r in _requests)
            if (!r.Carried || papers == null || papers.State(r.Document) == PaperState.NotHandedOver)
                open.Add(r);
        return open;
    }

    /// <summary>The request with <paramref name="id"/>, or null.</summary>
    public FormRequest Find(string id) => _requests.Find(r => r.Id == id);

    /// <summary>Where request <paramref name="id"/> stands (None for an unknown id).</summary>
    public MissingPaperState State(string id) => id != null && _states.TryGetValue(id, out MissingPaperState s) ? s : MissingPaperState.None;

    /// <summary>Flags request <paramref name="id"/> missing: true when it is flaggable and was not flagged yet (the caller then unlocks its request on the wheel).</summary>
    public bool Flag(string id)
    {
        if (Find(id) == null || State(id) != MissingPaperState.None)
            return false;
        _states[id] = MissingPaperState.Flagged;
        return true;
    }

    /// <summary>Request <paramref name="id"/> was asked for and the traveller does not carry it: true the first time (the caller logs it as missing).</summary>
    public bool NotCarried(string id)
    {
        if (Find(id) == null || State(id) == MissingPaperState.NotCarried)
            return false;
        _states[id] = MissingPaperState.NotCarried;
        return true;
    }
}
