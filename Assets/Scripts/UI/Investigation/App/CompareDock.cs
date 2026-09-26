using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The PC's compare dock (the PC redesign DK9, CM2): a strip above the
/// taskbar, outside every window, shown while a traveller is at the desk.
/// Empty, its hint says to click two values; while a value is picked its pair
/// covers the hint with three columns: side A, the verdict (MATCH, MISMATCH,
/// DEVIATION LOGGED, ALREADY DOCUMENTED, or the wait for a second pick) and
/// side B, and ✕ clears (the builder wires it to CompareController.Clear).
/// Each side reads "label: value" and is a link back to where it was picked
/// (SmartLinks.ForKey: the Investigation app opens it in its active pane;
/// underlined when it leads somewhere; a paper held at the desk and not
/// scanned, or a garment, leads nowhere). After a proof the verdict leads to
/// the Report tab. CompareController draws it; a link never picks.
/// </summary>
public sealed class CompareDock : MonoBehaviour
{
    /// <summary>The three columns and ✕, shown over the hint while a value is picked.</summary>
    [SerializeField] private GameObject pair;

    /// <summary>Side A: a link back to its source.</summary>
    [SerializeField] private Button sideA;

    /// <summary>Side A's text.</summary>
    [SerializeField] private TMP_Text sideAText;

    /// <summary>The verdict: after a proof, a link to the Report tab.</summary>
    [SerializeField] private Button verdict;

    /// <summary>The verdict's text (in the verdict's colour).</summary>
    [SerializeField] private TMP_Text verdictText;

    /// <summary>Side B: a link back to its source.</summary>
    [SerializeField] private Button sideB;

    /// <summary>Side B's text.</summary>
    [SerializeField] private TMP_Text sideBText;

    /// <summary>The Investigation app the links open.</summary>
    [SerializeField] private InvestigationApp app;

    private string _keyA;
    private string _keyB;
    private bool _toReport;
    private bool _wired;

    /// <summary>The picked sides (null: none picked; B null while the first pick waits).</summary>
    public void ShowSides(ComparePick? a, ComparePick? b)
    {
        Wire();
        _keyA = a?.Key;
        _keyB = b?.Key;
        if (pair != null && pair.activeSelf != a.HasValue)
            pair.SetActive(a.HasValue);
        WriteSide(sideA, sideAText, a);
        WriteSide(sideB, sideBText, b);
    }

    /// <summary>The verdict's text and colour; <paramref name="toReport"/> makes it the way to the Report tab.</summary>
    public void ShowVerdict(string text, Color colour, bool toReport)
    {
        Wire();
        _toReport = toReport;
        if (verdictText != null)
        {
            verdictText.text = text;
            verdictText.color = colour;
            verdictText.fontStyle = toReport ? FontStyles.Bold | FontStyles.Underline : FontStyles.Bold;
        }
        if (verdict != null)
            verdict.interactable = toReport;
    }

    /// <summary>A side's "label: value", underlined and clickable when it leads somewhere.</summary>
    private void WriteSide(Button side, TMP_Text text, ComparePick? pick)
    {
        bool linked = pick.HasValue && app != null && !app.LinkFor(pick.Value.Key).IsNone;
        if (text != null)
        {
            text.text = pick.HasValue ? UiText.Format("compare.dockSide", pick.Value.Label, pick.Value.Shown) : string.Empty;
            text.fontStyle = linked ? FontStyles.Underline : FontStyles.Normal;
        }
        if (side != null)
            side.interactable = linked;
    }

    /// <summary>The columns' clicks, once.</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (sideA != null)
            sideA.onClick.AddListener(() => Open(_keyA));
        if (sideB != null)
            sideB.onClick.AddListener(() => Open(_keyB));
        if (verdict != null)
            verdict.onClick.AddListener(OpenReport);
    }

    /// <summary>A side's source in the app's active pane (nothing when it leads nowhere).</summary>
    private void Open(string key)
    {
        if (app == null || string.IsNullOrEmpty(key))
            return;
        LinkTarget target = app.LinkFor(key);
        if (!target.IsNone)
            app.Open(target, false);
    }

    /// <summary>The Report tab in the app's active pane, after a proof.</summary>
    private void OpenReport()
    {
        if (_toReport && app != null)
            app.Open(LinkTarget.ToTab(AppTab.Report), false);
    }
}
