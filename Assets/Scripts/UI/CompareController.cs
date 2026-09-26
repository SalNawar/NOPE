using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Visual-only click-to-compare (Papers, Please style). Pick one value, then
/// another, and both light up and show side by side so the player can spot a
/// mismatch themselves: no automatic verdict. A third pick starts a new
/// comparison; a value picked again clears it. One comparison serves every
/// surface (piece 10): the PC's rows (document fields, reference books,
/// Citizen Records, transcript answers) and the desk's (a held paper's rows,
/// the bubble's answer, a garment from the wheel's look menu) call
/// <see cref="Select"/> with a pick from EvidencePicks. The pair rule and MATCH
/// are ComparePair's (each side's CompareEvidence.MatchValue: a garment shows
/// its item but matches on its place's Culture value). A PC row lights by its
/// key (the PC redesign CM3: IsPicked and PicksChanged, so a value shown in
/// both panes lights in both and stays lit after a page flip); a desk row and
/// the bubble light through the ICompareHighlight they pass. The pair is
/// drawn twice: in the PC's compare dock as side A, the verdict and side B
/// (CompareDock: DK9, CM2) and on the office strip as one line (optional).
/// </summary>
public sealed class CompareController : MonoBehaviour
{
    /// <summary>The PC's compare dock: side A, the verdict, side B.</summary>
    [SerializeField] private CompareDock dock;

    /// <summary>The office compare strip (piece 10; optional), shown while a value is picked; its parent, the office case HUD, shows only while the frame is closed.</summary>
    [SerializeField] private GameObject officeBar;

    /// <summary>The office strip's text: the pair on one line, in the verdict's colour.</summary>
    [SerializeField] private TMP_Text officeText;

    /// <summary>Tint applied to a picked value (a row's background, a paper's row, the bubble).</summary>
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.35f, 0.7f);

    /// <summary>Compare-bar text color when the two values match.</summary>
    [SerializeField] private Color matchColor = new Color(0.05f, 0.45f, 0.12f, 1f);

    /// <summary>Compare-bar text color when the two values differ.</summary>
    [SerializeField] private Color mismatchColor = new Color(0.72f, 0.1f, 0.08f, 1f);

    /// <summary>Compare-bar text color while only one value is picked.</summary>
    [SerializeField] private Color neutralColor = new Color(0.18f, 0.15f, 0.05f, 1f);

    /// <summary>The two sides (the one pair rule).</summary>
    private readonly ComparePair _pair = new ComparePair();

    /// <summary>Where each side lit up by its own object (a desk row, the bubble; null: nowhere, or a PC row lit by its key).</summary>
    private ICompareHighlight _highlightA;
    private ICompareHighlight _highlightB;

    /// <summary>
    /// Raised when the second side is picked, with both sides' typed evidence.
    /// The discrepancy system listens to auto-register true contradictions.
    /// </summary>
    public event Action<CompareEvidence, CompareEvidence> PairCompared;

    /// <summary>Raised whenever the picked keys change (a pick, a pair, a clear): keyed rows relight (CM3).</summary>
    public event Action PicksChanged;

    /// <summary>The theme's tint of a picked value (keyed rows read it when they light).</summary>
    public Color HighlightColor => highlightColor;

    private void Awake()
    {
        if (officeBar != null)
            officeBar.SetActive(false);
        Draw();
    }

    /// <summary>True while the value with <paramref name="key"/> is one of the picked sides.</summary>
    public bool IsPicked(string key) =>
        !string.IsNullOrEmpty(key) && ((_pair.HasA && _pair.A.Key == key) || (_pair.IsPaired && _pair.B.Key == key));

    /// <summary>
    /// Picks a value for comparison (EvidencePicks builds it; its shown text
    /// is the canonical value or an untranslated statement's placeholder, its
    /// evidence always canonical) and lights <paramref name="highlight"/>
    /// (optional: a PC row lights by its key) while it is picked: the same key
    /// again clears the comparison, a first pick waits, a second pairs
    /// (PairCompared), a pick after a pair starts anew.
    /// </summary>
    public void Select(ComparePick pick, ICompareHighlight highlight)
    {
        CompareStep step = _pair.Select(pick);
        if (step == CompareStep.Cleared)
        {
            ClearHighlights();
            Draw();
            PicksChanged?.Invoke();
            return;
        }

        if (step == CompareStep.Pending)
        {
            ClearHighlights();
            _highlightA = highlight;
        }
        else
        {
            _highlightB = highlight;
        }

        highlight?.Show(true, highlightColor);
        Draw();
        PicksChanged?.Invoke();

        if (step == CompareStep.Paired)
            PairCompared?.Invoke(_pair.A.Evidence, _pair.B.Evidence);
    }

    /// <summary>The present culture's compare colours (CultureThemeService at scene load; piece 6).</summary>
    public void ApplyTheme(Color match, Color mismatch, Color neutral, Color highlight)
    {
        matchColor = match;
        mismatchColor = mismatch;
        neutralColor = neutral;
        highlightColor = highlight;
    }

    /// <summary>
    /// Replaces the verdict after a discrepancy registers, so an origin-proof
    /// never reads as a friendly green MATCH: the office strip names the
    /// deviation, the dock reads DEVIATION LOGGED and leads to the Report.
    /// </summary>
    public void ShowDeviation(string summary) =>
        WriteVerdict(UiText.Format("compare.deviationLogged", summary), UiText.Get("compare.dockDeviation"), mismatchColor, true);

    /// <summary>
    /// Replaces the verdict when a pair proves a category that is already in
    /// the Deviation Report, so a second proof visibly adds nothing.
    /// </summary>
    public void ShowAlreadyDocumented(string categoryLabel) =>
        WriteVerdict(UiText.Format("compare.alreadyDocumented", categoryLabel), UiText.Get("compare.dockAlready"), neutralColor, true);

    /// <summary>Clears the picks, their highlights and the bars.</summary>
    public void Clear()
    {
        _pair.Clear();
        ClearHighlights();
        Draw();
        PicksChanged?.Invoke();
    }

    /// <summary>Restores both sides' highlights.</summary>
    private void ClearHighlights()
    {
        _highlightA?.Show(false, highlightColor);
        _highlightB?.Show(false, highlightColor);
        _highlightA = null;
        _highlightB = null;
    }

    /// <summary>Shows the bars while a value is picked: the dock's sides and the office strip, with MATCH or MISMATCH, the first pick waiting, or nothing.</summary>
    private void Draw()
    {
        bool active = _pair.HasA;
        if (officeBar != null)
            officeBar.SetActive(active);
        if (dock != null)
            dock.ShowSides(active ? _pair.A : (ComparePick?)null, _pair.IsPaired ? _pair.B : (ComparePick?)null);

        if (_pair.IsPaired)
        {
            bool match = _pair.Matches;
            string verdict = UiText.Get(match ? "compare.match" : "compare.mismatch");
            WriteVerdict(UiText.Format("compare.pair", verdict, _pair.A.Label, _pair.A.Shown, _pair.B.Label, _pair.B.Shown), verdict,
                         match ? matchColor : mismatchColor, false);
        }
        else if (active)
        {
            WriteVerdict(UiText.Format("compare.pickAnother", _pair.A.Label, _pair.A.Shown), UiText.Get("compare.dockPending"), neutralColor, false);
        }
        else
        {
            WriteVerdict(string.Empty, string.Empty, neutralColor, false);
        }
    }

    /// <summary>Writes the office strip's line and the dock's verdict (<paramref name="toReport"/>: it leads to the Report) in one colour.</summary>
    private void WriteVerdict(string officeLine, string dockVerdict, Color colour, bool toReport)
    {
        if (officeText != null)
        {
            officeText.color = colour;
            officeText.text = officeLine;
        }
        if (dock != null)
            dock.ShowVerdict(dockVerdict, colour, toReport);
    }
}
