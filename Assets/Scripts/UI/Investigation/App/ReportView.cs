using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Investigation app's Report tab (the PC redesign AP5, FO9, §2.8, CM5):
/// the case's Deviation Report (Form_DeviationReport, TC-930, on a FormPage
/// at the pane's width): the case line (the day's date and the claim
/// banner's text), per documented deviation a heading band (its number,
/// category and proof) over a row of its two sides (STATEMENT, CONTRADICTED
/// BY: ReportPage), each side's cell with a ↗ back to where it was
/// picked (SmartLinks.ForKey through the app: a paper's field, a transcript
/// line, a book row, a record row; a held paper links nowhere), the tail
/// (nothing documented yet, or the count), the desk officer's sign-off (the
/// clerk's name over "Desk officer") and the stamp area. Nothing on it picks. A case source. A logged deviation
/// badges the tab; nothing opens it. Each pane has one; EvidencePresenter
/// writes them all.
/// </summary>
public sealed class ReportView : AppView
{
    /// <summary>The Deviation Report in its scroll.</summary>
    [SerializeField] private FormPage page;

    /// <summary>The Deviation Report's page kind (Form_DeviationReport, TC-930).</summary>
    [SerializeField] private FormSpecSO reportForm;

    private IReadOnlyList<ReportEntry> _entries = System.Array.Empty<ReportEntry>();
    private InvestigationApp _app;

    /// <inheritdoc />
    public override AppTab Tab => AppTab.Report;

    /// <summary>The report's form (null without a page).</summary>
    private FormView Form => page != null ? page.Form : null;

    /// <summary>The app this view lives in (where a pick's link leads: LinkFor).</summary>
    private InvestigationApp App => _app != null ? _app : _app = GetComponentInParent<InvestigationApp>();

    private void Awake()
    {
        if (Form != null)
            Form.CellLinkClicked += Follow;
    }

    private void OnDestroy()
    {
        if (Form != null)
            Form.CellLinkClicked -= Follow;
    }

    /// <summary>
    /// Draws the report: <paramref name="entries"/> (the case's documented
    /// deviations with their sides, in order) under <paramref name="caseLine"/>
    /// dated <paramref name="day"/>, headed with <paramref name="agency"/>'s
    /// block and signed by its clerk; with none, the tail says what to compare.
    /// </summary>
    public void Show(IReadOnlyList<ReportEntry> entries, string caseLine, AgencyContent agency, int day)
    {
        _entries = entries ?? System.Array.Empty<ReportEntry>();
        if (page == null || Form == null || reportForm == null)
            return;
        FormData data = reportForm.Page(agency);
        string date = agency != null ? AgencyCalendar.Today(agency.firstDate, day) : null;
        data.Text = new Dictionary<string, string>
        {
            { ReportPage.CaseLineSlot, date != null ? UiText.Format("form.report.caseLine", date, caseLine ?? string.Empty) : caseLine ?? string.Empty },
            { ReportPage.TailSlot, _entries.Count == 0 ? UiText.Get("scanner.idle") : UiText.Format("scanner.summary", _entries.Count) },
            { ReportPage.SignatureSlot, agency != null && agency.clerk != null ? agency.clerk.name : string.Empty }
        };
        data.Rows = new Dictionary<string, IReadOnlyList<string[]>> { { ReportPage.RowsSlot, ReportPage.Rows(_entries, UiText.Category, Proof) } };
        page.Show(reportForm.form, data, _ => false, null, CellLinkHint);
    }

    /// <summary>The proof's word on the report ("Mismatch", "Belongs elsewhere", "Agency records", "Papers disagree").</summary>
    private static string Proof(DiscrepancyProof proof) =>
        UiText.Get(proof == DiscrepancyProof.ForeignOrigin ? "report.proof.foreignOrigin"
                 : proof == DiscrepancyProof.RecordMismatch ? "report.proof.recordMismatch"
                 : proof == DiscrepancyProof.CrossMismatch ? "report.proof.crossMismatch"
                 : "report.proof.claimMismatch");

    /// <summary>The entry a row's slot shows, or null.</summary>
    private ReportEntry EntryOf(FormSlot slot) =>
        slot.Source == ReportPage.RowsSlot && ReportPage.EntryOfRow(slot.Row) >= 0 && ReportPage.EntryOfRow(slot.Row) < _entries.Count
            ? _entries[ReportPage.EntryOfRow(slot.Row)] : null;

    /// <summary>Where a side's cell links (the pick's place in the app), None when it has none.</summary>
    private LinkTarget Link(FormSlot slot, int cell)
    {
        string key = ReportPage.LinkKey(EntryOf(slot), cell);
        return key != null && App != null ? App.LinkFor(key) : LinkTarget.None;
    }

    /// <summary>The ↗'s hover hint of a side's cell with a link ("Open where it was picked: …"), or null (no ↗).</summary>
    private string CellLinkHint(FormSlot slot, int cell)
    {
        if (Link(slot, cell).IsNone)
            return null;
        ReportEntry entry = EntryOf(slot);
        return UiText.Format("app.link.pick", cell == ReportPage.StatementCell ? entry.Statement.Label : entry.Truth.Label);
    }

    /// <summary>A side's ↗ was clicked: its link, through the pane this report is in (LK2).</summary>
    private void Follow(FormSlot slot, int cell)
    {
        LinkTarget link = Link(slot, cell);
        AppPane pane = GetComponentInParent<AppPane>();
        if (!link.IsNone && pane != null)
            pane.FollowLink(link);
    }
}
