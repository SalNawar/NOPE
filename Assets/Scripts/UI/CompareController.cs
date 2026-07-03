using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visual-only click-to-compare (Papers, Please style). Click one value, then
/// another, and both are highlighted and shown side by side in a compare bar so
/// the player can spot a mismatch themselves — no automatic verdict. A third
/// click starts a new comparison. Document field rows and reference-book entry
/// rows call <see cref="Select"/>.
/// </summary>
public sealed class CompareController : MonoBehaviour
{
    /// <summary>Bar shown while a comparison is active.</summary>
    [SerializeField] private GameObject compareBar;

    /// <summary>Text that shows the two compared values.</summary>
    [SerializeField] private TMP_Text compareText;

    /// <summary>Tint applied to a selected row's background.</summary>
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.35f, 0.7f);

    /// <summary>Compare-bar text color when the two values match.</summary>
    [SerializeField] private Color matchColor = new Color(0.05f, 0.45f, 0.12f, 1f);

    /// <summary>Compare-bar text color when the two values differ.</summary>
    [SerializeField] private Color mismatchColor = new Color(0.72f, 0.1f, 0.08f, 1f);

    /// <summary>Compare-bar text color while only one value is picked.</summary>
    [SerializeField] private Color neutralColor = new Color(0.18f, 0.15f, 0.05f, 1f);

    private struct Slot
    {
        public string label;
        public string value;
        public Image graphic;
        public Color original;
        public bool set;
        public CompareEvidence evidence;
    }

    private Slot _a;
    private Slot _b;

    /// <summary>
    /// Raised when the second slot fills, with both sides' typed evidence.
    /// The discrepancy system listens to auto-register true contradictions.
    /// </summary>
    public event System.Action<CompareEvidence, CompareEvidence> PairCompared;

    private void Awake()
    {
        if (compareBar != null)
            compareBar.SetActive(false);
    }

    /// <summary>Registers a clicked value for comparison (no typed evidence).</summary>
    public void Select(string label, string value, Image highlight) =>
        Select(label, value, highlight, default);

    /// <summary>Registers a clicked value for comparison, with typed evidence.</summary>
    public void Select(string label, string value, Image highlight, CompareEvidence evidence)
    {
        // Clicking the same row again clears the comparison.
        if ((_a.set && highlight != null && highlight == _a.graphic) ||
            (_b.set && highlight != null && highlight == _b.graphic))
        {
            Clear();
            return;
        }

        if (_a.set && _b.set)
            Clear();

        if (!_a.set)
            _a = Fill(label, value, highlight, evidence);
        else
            _b = Fill(label, value, highlight, evidence);

        Refresh();

        if (_a.set && _b.set)
            PairCompared?.Invoke(_a.evidence, _b.evidence);
    }

    /// <summary>
    /// Replaces the compare bar verdict after a discrepancy registers, so an
    /// origin-proof never reads as a friendly green MATCH.
    /// </summary>
    public void ShowDeviation(string summary)
    {
        if (compareText == null)
            return;

        compareText.color = mismatchColor;
        compareText.text = $"●  DEVIATION LOGGED — {summary}";
    }

    private Slot Fill(string label, string value, Image g, CompareEvidence evidence)
    {
        var s = new Slot { label = label, value = value, graphic = g, set = true, evidence = evidence };

        if (g != null)
        {
            s.original = g.color;
            g.color = highlightColor;
        }

        return s;
    }

    private void Refresh()
    {
        if (compareBar != null)
            compareBar.SetActive(_a.set);

        if (compareText == null)
            return;

        if (_a.set && _b.set)
        {
            bool match = ValuesMatch(_a.value, _b.value);
            compareText.color = match ? matchColor : mismatchColor;
            string verdict = match ? "MATCH" : "MISMATCH";
            compareText.text = $"{verdict}    {_a.label}:  {_a.value}    vs    {_b.label}:  {_b.value}";
        }
        else if (_a.set)
        {
            compareText.color = neutralColor;
            compareText.text = $"{_a.label}:  {_a.value}    vs    (pick another value to compare)";
        }
        else
        {
            compareText.color = neutralColor;
            compareText.text = string.Empty;
        }
    }

    /// <summary>Case-insensitive, trimmed equality for two displayed values.</summary>
    private static bool ValuesMatch(string a, string b)
    {
        return string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(),
            System.StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Clears highlights and the compare bar.</summary>
    public void Clear()
    {
        if (_a.set && _a.graphic != null)
            _a.graphic.color = _a.original;

        if (_b.set && _b.graphic != null)
            _b.graphic.color = _b.original;

        _a = default;
        _b = default;

        if (compareBar != null)
            compareBar.SetActive(false);

        if (compareText != null)
            compareText.text = string.Empty;
    }
}
