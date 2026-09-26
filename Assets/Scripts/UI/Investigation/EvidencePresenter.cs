using System;
using System.Collections.Generic;
using System.Text;
using TMPro;

/// <summary>
/// The investigation's evidence (the PC redesign RF1): the current case's
/// discrepancy log, the Deviation Report's text and the compare's verdict
/// after a proof. When the compare pairs two values while a case is on the
/// desk, a true contradiction (DiscrepancyLog.Prove) is logged once per
/// category: the report is rewritten, the compare reads DEVIATION LOGGED and
/// the app hears of it (the Report tab's badge; nothing opens: CM5); a second
/// proof of a logged category only reads ALREADY DOCUMENTED. The report is
/// written into each pane's Report tab. The log clears
/// with each case. It subscribes to the
/// compare it was given and unsubscribes from that same instance (audit
/// R4-003). Plain C#; InvestigationUIController owns it.
/// </summary>
public sealed class EvidencePresenter
{
    private readonly CompareController _compare;
    private readonly IReadOnlyList<TMP_Text> _reportTexts;
    private readonly Action _logged;
    private readonly Func<CaseInstance> _currentCase;

    /// <summary>Documented contradictions for the current case.</summary>
    private readonly DiscrepancyLog _discrepancies = new DiscrepancyLog();

    /// <summary>The compare whose pairs this listens to (null while detached).</summary>
    private CompareController _listening;

    /// <summary>The compare and the Deviation Report's texts (one per pane; either may be missing), what a new deviation tells (the app's Report tab), and the façade's current case (null between cases).</summary>
    public EvidencePresenter(CompareController compare, IReadOnlyList<TMP_Text> reportTexts, Action logged, Func<CaseInstance> currentCase)
    {
        _compare = compare;
        _reportTexts = reportTexts ?? Array.Empty<TMP_Text>();
        _logged = logged ?? throw new ArgumentNullException(nameof(logged));
        _currentCase = currentCase ?? throw new ArgumentNullException(nameof(currentCase));
    }

    /// <summary>Number of discrepancies documented for the current case.</summary>
    public int Count => _discrepancies.Count;

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
        RefreshReport();
    }

    /// <summary>
    /// Documents a true contradiction when the player compares a liar's tell
    /// against the reference entry or record that disproves it; proving an
    /// already documented category again only says so in the compare bar.
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

        RefreshReport();

        if (_compare != null)
            _compare.ShowDeviation(UiText.Deviation(proof));

        _logged();
    }

    /// <summary>Rewrites the Deviation Report's body from the discrepancy log, in every pane.</summary>
    private void RefreshReport()
    {
        string report;
        if (_discrepancies.Count == 0)
        {
            report = UiText.Get("scanner.idle");
        }
        else
        {
            var sb = new StringBuilder();
            foreach (Discrepancy d in _discrepancies.Items)
                sb.AppendLine(UiText.Format("list.bullet", UiText.Deviation(d)));

            sb.AppendLine();
            sb.AppendLine(UiText.Format("scanner.summary", _discrepancies.Count));
            report = sb.ToString();
        }

        foreach (TMP_Text text in _reportTexts)
            if (text != null)
                text.text = report;
    }
}
