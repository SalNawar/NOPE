using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The findings column (the PC workbench spec IA8, §4.4; lesson 2: only what
/// the player compares is logged): its heading, a scrolling list with a
/// plate per logged finding, newest first, so a new one is always in view (a match on the match plate, a
/// difference on the difference plate with its title in bold and "Logged as
/// evidence" under a proof), the line shown while nothing is logged, and
/// "Open the report" (the Deviation Report on the target side). A plate
/// clicked shows its finding again (MatchBoard). The texts are built here
/// from the finding's kind and sides (ui strings "finding.title.*",
/// "finding.detail*").
/// </summary>
public sealed class FindingsView : MonoBehaviour
{
    /// <summary>The list's content, where the plates go.</summary>
    [SerializeField] private RectTransform rowsRoot;

    /// <summary>A finding's plate (inactive), cloned per finding: a button holding a "Match" and a "Differ" plate, each with its Title and Detail texts.</summary>
    [SerializeField] private Button rowTemplate;

    /// <summary>The line shown while nothing is logged.</summary>
    [SerializeField] private GameObject emptyText;

    /// <summary>Opens the Deviation Report on the target side.</summary>
    [SerializeField] private Button reportButton;

    /// <summary>The app (the report opens in it).</summary>
    [SerializeField] private InvestigationApp app;

    private readonly List<Button> _rows = new List<Button>();
    private readonly List<Finding> _shown = new List<Finding>();
    private bool _wired;

    /// <summary>Raised when a finding's plate is clicked.</summary>
    public event Action<Finding> Clicked;

    /// <summary>The plates shown, top to bottom (the keys' Findings region), then Open the report.</summary>
    public IEnumerable<Button> Buttons
    {
        get
        {
            foreach (Button row in _rows)
                if (row != null && row.gameObject.activeInHierarchy)
                    yield return row;
            if (reportButton != null && reportButton.gameObject.activeInHierarchy)
                yield return reportButton;
        }
    }

    /// <summary>A finding's title ("Visa class matches", "Breaks the rule: …").</summary>
    public static string Title(Finding finding) => UiText.Format("finding.title." + finding.Kind, finding.Subject);

    /// <summary>A finding's detail: the two values and where each is ("Premium (Leisure Departure Visa) against Economy (Records)"); a rule's or the date's finding names the value alone.</summary>
    public static string Detail(Finding finding)
    {
        if (finding.KeyA == EntryKeys.CalendarToday)
            return UiText.Format("finding.detail.today", finding.ValueB, Where(finding.TitleB), finding.ValueA);
        return EntryKeys.TryRule(finding.KeyA, out _)
            ? UiText.Format("finding.detail.rule", finding.ValueB, Where(finding.TitleB))
            : UiText.Format("finding.detail", finding.ValueA, Where(finding.TitleA), finding.ValueB, Where(finding.TitleB));
    }

    /// <summary>The detail a pick's label names: the part after its last " · " ("Leisure Departure Visa · Citizen ID": "Citizen ID"), else the label.</summary>
    public static string What(string label)
    {
        int at = label != null ? label.LastIndexOf(" · ", StringComparison.Ordinal) : -1;
        return at >= 0 ? label.Substring(at + 3) : label ?? string.Empty;
    }

    /// <summary>Where a pick's label says it is: the part before its first " · " ("Leisure Departure Visa"), else the label.</summary>
    public static string Where(string label)
    {
        int at = label != null ? label.IndexOf(" · ", StringComparison.Ordinal) : -1;
        return at >= 0 ? label.Substring(0, at) : label ?? string.Empty;
    }

    private void Awake() => Wire();

    /// <summary>Hides the template and wires Open the report (once).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
        if (reportButton != null && app != null)
            reportButton.onClick.AddListener(() => app.OpenOnTarget(LinkTarget.ToTab(AppTab.Report)));
    }

    /// <summary>Draws <paramref name="log"/>'s findings, newest first (the plates of the last draw reused), and the empty line when there is none.</summary>
    public void Show(FindingLog log)
    {
        Wire();
        _shown.Clear();
        if (log != null)
            for (int i = log.Items.Count - 1; i >= 0; i--)
                _shown.Add(log.Items[i]);
        while (_rows.Count < _shown.Count && rowTemplate != null && rowsRoot != null)
        {
            Button row = Instantiate(rowTemplate, rowsRoot);
            int index = _rows.Count;
            row.name = "Finding_" + index;
            row.onClick.AddListener(() =>
            {
                if (index < _shown.Count)
                    Clicked?.Invoke(_shown[index]);
            });
            _rows.Add(row);
        }
        for (int i = 0; i < _rows.Count; i++)
        {
            bool on = i < _shown.Count;
            if (_rows[i].gameObject.activeSelf != on)
                _rows[i].gameObject.SetActive(on);
            if (on)
                Draw(_rows[i], _shown[i]);
        }
        if (emptyText != null && emptyText.activeSelf != (_shown.Count == 0))
            emptyText.SetActive(_shown.Count == 0);
    }

    /// <summary>One plate: the match or the difference plate on, its title (bold for a difference) and detail, and "Logged as evidence" under a proof.</summary>
    private static void Draw(Button row, Finding finding)
    {
        bool differ = FindingRules.IsDifference(finding.Kind);
        Transform match = row.transform.Find("Match"), difference = row.transform.Find("Differ");
        if (match != null)
            match.gameObject.SetActive(!differ);
        if (difference != null)
            difference.gameObject.SetActive(differ);
        Transform plate = differ ? difference : match;
        if (plate == null)
            return;
        Transform title = plate.Find("Title"), detail = plate.Find("Detail");
        if (title != null)
            title.GetComponent<TMP_Text>().text = differ ? "<b>" + Title(finding) + "</b>" : Title(finding);
        if (detail != null)
            detail.GetComponent<TMP_Text>().text = finding.Proof ? Detail(finding) + "\n" + UiText.Get("finding.evidence") : Detail(finding);
    }
}
