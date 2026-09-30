using System;
using System.Collections.Generic;

/// <summary>
/// The investigation's evidence (the PC redesign RF1): the current case's
/// discrepancy log, the Deviation Report and the compare's verdict after a
/// proof. When the compare pairs two values while a case is on the desk, a
/// true contradiction (DiscrepancyLog.Prove) is logged once per category
/// with the two picks that proved it (ReportEntry): the report is drawn
/// again, the compare reads DEVIATION LOGGED and the app hears of it (the
/// Report tab's badge; nothing opens: CM5) and it joins search's case layer
/// (redesign phase 19); a second proof of a logged category only reads
/// ALREADY DOCUMENTED. The report is drawn into each pane's Report tab (its
/// form: ReportView). The log clears with each case. It subscribes to the
/// compare it was given and unsubscribes from that same instance (audit
/// R4-003). Plain C#; InvestigationUIController owns it.
/// </summary>
public sealed class EvidencePresenter
{
    private readonly CompareController _compare;
    private readonly IReadOnlyList<ReportView> _reports;
    private readonly Action _logged;
    private readonly Func<CaseInstance> _currentCase;
    private readonly Func<AgencyContent> _agency;
    private readonly Func<int> _day;
    private readonly CaseIndex _index;

    /// <summary>Documented contradictions for the current case.</summary>
    private readonly DiscrepancyLog _discrepancies = new DiscrepancyLog();

    /// <summary>The report's lines: each documented contradiction with the pair that proved it, in order.</summary>
    private readonly List<ReportEntry> _entries = new List<ReportEntry>();

    /// <summary>The compare whose pairs this listens to (null while detached).</summary>
    private CompareController _listening;

    /// <summary>The compare and the Deviation Report's views (one per pane; either may be missing), what a new deviation tells (the app's Report tab), the façade's current case (null between cases), search's index (null: nothing indexed), and the agency block and day the report is headed and signed with.</summary>
    public EvidencePresenter(CompareController compare, IReadOnlyList<ReportView> reports, Action logged, Func<CaseInstance> currentCase, CaseIndex index,
                             Func<AgencyContent> agency, Func<int> day)
    {
        _index = index;
        _compare = compare;
        _reports = reports ?? Array.Empty<ReportView>();
        _logged = logged ?? throw new ArgumentNullException(nameof(logged));
        _currentCase = currentCase ?? throw new ArgumentNullException(nameof(currentCase));
        _agency = agency ?? throw new ArgumentNullException(nameof(agency));
        _day = day ?? throw new ArgumentNullException(nameof(day));
    }

    /// <summary>Raised with each newly documented discrepancy, after the report and the app hear of it (the traveller wheel's question about it; wave 5, lesson 3).</summary>
    public event Action<Discrepancy> Documented;

    /// <summary>Number of discrepancies documented for the current case.</summary>
    public int Count => _discrepancies.Count;

    /// <summary>The documented categories of the current case (the Deviation Report's): what the Analysis Scanner no longer marks (CaseDocumentsPresenter).</summary>
    public IReadOnlyCollection<ClueCategory> DocumentedCategories => _discrepancies.Categories;

    /// <summary>Starts listening to the compare's pairs.</summary>
    public void Attach()
    {
        if (_compare == null)
            return;
        _listening = _compare;
        _listening.PairCompared += HandlePairCompared;
    }

    /// <summary>Stops listening (to the instance it attached to).</summary>
    public void Detach()
    {
        if (_listening == null)
            return;
        _listening.PairCompared -= HandlePairCompared;
        _listening = null;
    }

    /// <summary>A new case: the log clears and the report says so.</summary>
    public void BeginCase()
    {
        _discrepancies.Clear();
        _entries.Clear();
        RefreshReport();
    }

    /// <summary>
    /// Documents a true contradiction when the player compares a liar's tell
    /// against the reference entry or record that disproves it (the pair's
    /// two picks kept for the report's links); proving an already documented
    /// category again only says so in the compare bar.
    /// </summary>
    private void HandlePairCompared(CompareEvidence a, CompareEvidence b)
    {
        CaseInstance current = _currentCase();
        if (current == null)
            return;

        Discrepancy proof = DiscrepancyLog.Prove(a, b,
            current.claimedNation != null ? current.claimedNation.id : null,
            current.claimedEra != null ? current.claimedEra.id : null,
            current.visitorGivenName);
        if (proof == null)
            return;

        if (!_discrepancies.Add(proof))
        {
            if (_compare != null)
                _compare.ShowAlreadyDocumented(UiText.Category(proof.category));
            return;
        }

        _entries.Add(ReportEntry.From(proof, _compare.SideA, _compare.SideB));
        RefreshReport();
        if (_index != null)
        {
            string category = UiText.Category(proof.category);
            _index.Add(IndexEntries.Deviation(_discrepancies.Count - 1, proof.category, UiText.Format("search.title.deviation", category), category,
                                              UiText.Deviation(proof)));
        }

        if (_compare != null)
            _compare.ShowDeviation(UiText.Deviation(proof));

        _logged();
        Documented?.Invoke(proof);
    }

    /// <summary>Draws the Deviation Report from the log, in every pane: the case line (the traveller's name and role, as the app's title: no claim is printed, the personalities spec's B1), the entries, the agency block and the day.</summary>
    private void RefreshReport()
    {
        CaseInstance current = _currentCase();
        string caseLine = current != null ? current.visitorDisplayName : string.Empty;
        foreach (ReportView view in _reports)
            if (view != null)
                view.Show(_entries, caseLine, _agency(), _day());
    }
}
