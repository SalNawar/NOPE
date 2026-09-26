using TMPro;
using UnityEngine;

/// <summary>
/// Visual-only click-to-compare (Papers, Please style). Pick one value, then
/// another, and both light up and show side by side in a compare bar so the
/// player can spot a mismatch themselves: no automatic verdict. A third pick
/// starts a new comparison; a value picked again clears it. One comparison
/// serves every surface (piece 10): the PC's rows (document fields, reference
/// books, Citizen Records, transcript answers) and the desk's (a held paper's
/// rows, the bubble's answer, a garment from the wheel's look menu) call
/// <see cref="Select"/> with a pick from EvidencePicks. The pair rule and MATCH
/// are ComparePair's (each side's CompareEvidence.MatchValue: a garment shows
/// its item but matches on its place's Culture value). The same text and
/// colours are drawn in two bars: the PC's compare dock (the PC redesign DK9,
/// CM2: docked above the taskbar, outside every window) and the office strip
/// (optional).
/// </summary>
public sealed class CompareController : MonoBehaviour
{
    /// <summary>The PC compare dock's pair, shown while a value is picked (over the dock's "click two values" hint; the dock itself shows while a traveller is at the desk).</summary>
    [SerializeField] private GameObject compareBar;

    /// <summary>The dock's text: the compared values and the verdict.</summary>
    [SerializeField] private TMP_Text compareText;

    /// <summary>The office compare strip (piece 10; optional), shown while a value is picked; its parent, the office case HUD, shows only while the frame is closed.</summary>
    [SerializeField] private GameObject officeBar;

    /// <summary>The office strip's text (the PC bar's text and colour).</summary>
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

    /// <summary>Where each side lit up (null: nowhere, e.g. a garment).</summary>
    private ICompareHighlight _highlightA;
    private ICompareHighlight _highlightB;

    /// <summary>
    /// Raised when the second side is picked, with both sides' typed evidence.
    /// The discrepancy system listens to auto-register true contradictions.
    /// </summary>
    public event System.Action<CompareEvidence, CompareEvidence> PairCompared;

    /// <summary>The two bars' texts (the dock's and the office strip's), gathered once (audit R4-026).</summary>
    private TMP_Text[] _bars;

    private void Awake()
    {
        _bars = new[] { compareText, officeText };
        if (compareBar != null)
            compareBar.SetActive(false);
        if (officeBar != null)
            officeBar.SetActive(false);
    }

    /// <summary>
    /// Picks a value for comparison (EvidencePicks builds it; its shown text
    /// is the canonical value or an untranslated statement's placeholder, its
    /// evidence always canonical) and lights <paramref name="highlight"/>
    /// (optional) while it is picked: the same key again clears the
    /// comparison, a first pick waits, a second pairs (PairCompared), a pick
    /// after a pair starts anew.
    /// </summary>
    public void Select(ComparePick pick, ICompareHighlight highlight)
    {
        CompareStep step = _pair.Select(pick);
        if (step == CompareStep.Cleared)
        {
            ClearHighlights();
            Draw();
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
    /// Replaces the bars' verdict after a discrepancy registers, so an
    /// origin-proof never reads as a friendly green MATCH.
    /// </summary>
    public void ShowDeviation(string summary) =>
        WriteBars(UiText.Format("compare.deviationLogged", summary), mismatchColor);

    /// <summary>
    /// Replaces the bars' verdict when a pair proves a category that is
    /// already in the Deviation Report, so a second proof visibly adds nothing.
    /// </summary>
    public void ShowAlreadyDocumented(string categoryLabel) =>
        WriteBars(UiText.Format("compare.alreadyDocumented", categoryLabel), neutralColor);

    /// <summary>Clears the picks, their highlights and the bars.</summary>
    public void Clear()
    {
        _pair.Clear();
        ClearHighlights();
        Draw();
    }

    /// <summary>Restores both sides' highlights.</summary>
    private void ClearHighlights()
    {
        _highlightA?.Show(false, highlightColor);
        _highlightB?.Show(false, highlightColor);
        _highlightA = null;
        _highlightB = null;
    }

    /// <summary>Shows the bars while a value is picked, with the pair's text: MATCH or MISMATCH, the first pick waiting, or nothing.</summary>
    private void Draw()
    {
        bool active = _pair.HasA;
        if (compareBar != null)
            compareBar.SetActive(active);
        if (officeBar != null)
            officeBar.SetActive(active);

        if (_pair.IsPaired)
        {
            bool match = _pair.Matches;
            string verdict = UiText.Get(match ? "compare.match" : "compare.mismatch");
            WriteBars(UiText.Format("compare.pair", verdict, _pair.A.Label, _pair.A.Shown, _pair.B.Label, _pair.B.Shown), match ? matchColor : mismatchColor);
        }
        else if (active)
        {
            WriteBars(UiText.Format("compare.pickAnother", _pair.A.Label, _pair.A.Shown), neutralColor);
        }
        else
        {
            WriteBars(string.Empty, neutralColor);
        }
    }

    /// <summary>Writes the same text and colour into both bars' texts.</summary>
    private void WriteBars(string text, Color colour)
    {
        foreach (TMP_Text t in _bars)
        {
            if (t == null)
                continue;
            t.color = colour;
            t.text = text;
        }
    }
}
