using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The workbench's click-and-match (the PC workbench spec §4; lessons 2 and
/// D5: the player points, the game only confirms; a book or a rule is held
/// like a paper): a click on a value holds it (the status line says what is
/// held, Cancel lets go, a dashed line runs from it to the pointer), a click
/// on another value compares the two through the one compare
/// (CompareController: the Domain's ComparePair, PairCompared, the Deviation
/// Report's proof are untouched) and the result is drawn as a labelled line
/// between the two values (MatchLines), said on the status line and, when it
/// means something, logged in the findings (FindingLog; FindingRules says
/// what a pair means, RuleChecks what a rule says of a value, AgainstToday
/// what the calendar says of a date). A rule of today's memo and the
/// calendar's today are held here (they are not values of the compare);
/// either held against a value is judged here. After a pair the compare is
/// let go at the end of the frame (every listener has read its sides), so the
/// two values show the result's outline, not the pick tint. A finding
/// clicked in the column opens both its documents and shows its line again.
/// A case starts and ends everything. Nothing is ever marked by itself.
/// </summary>
public sealed class MatchBoard : MonoBehaviour
{
    [Header("Parts")]
    /// <summary>The one compare (every pickable value on the PC and at the desk).</summary>
    [SerializeField] private CompareController compare;

    /// <summary>The app (the rows by key, the documents a finding opens).</summary>
    [SerializeField] private InvestigationApp app;

    /// <summary>The line over the two panes.</summary>
    [SerializeField] private MatchLines lines;

    /// <summary>The findings column.</summary>
    [SerializeField] private FindingsView findings;

    [Header("The status line: one plate per state, each in its role's colours")]
    /// <summary>Nothing held, nothing just compared: the teaching hint.</summary>
    [SerializeField] private TMP_Text idleText;

    /// <summary>A value, a rule or the date held.</summary>
    [SerializeField] private TMP_Text holdText;

    /// <summary>Lets go of what is held (Esc too).</summary>
    [SerializeField] private Button cancelButton;

    /// <summary>A match, a rule met, a date that holds, just logged.</summary>
    [SerializeField] private TMP_Text matchText;

    /// <summary>A difference, a rule broken, a date that fails, just logged.</summary>
    [SerializeField] private TMP_Text differText;

    /// <summary>A note: nothing logged.</summary>
    [SerializeField] private TMP_Text infoText;

    /// <summary>What a value, a rule or the date held is: its key, where it is, what it reads; a rule or today for the specials.</summary>
    private sealed class Held
    {
        public string Key;
        public string Title;
        public string Value;
        public TravelRuleSO Rule;
        public bool Today;
    }

    private readonly FindingLog _log = new FindingLog();
    private readonly List<AppRow> _rows = new List<AppRow>();
    private Held _special;
    private CaseInstance _case;
    private string _linkA, _linkB, _linkLabel, _holdKey;
    private FindingLook _linkLook;
    private string _clearA, _clearB;
    private bool _quiet;
    private string _hint = string.Empty;
    private bool _wired;

    /// <summary>Raised when the findings or what is held change (the decision's Deny, the keys' regions).</summary>
    public event Action Changed;

    /// <summary>The case's findings.</summary>
    public FindingLog Log => _log;

    /// <summary>True while a value, a rule or the date is held (Esc lets go; the Holding region).</summary>
    public bool IsHolding => _special != null || (compare != null && compare.Holding);

    /// <summary>The status line's Cancel (the keys' Holding region).</summary>
    public Button CancelButton => cancelButton;

    /// <summary>The findings column (the keys' Findings region).</summary>
    public FindingsView Findings => findings;

    private void Awake() => Wire();

    private void OnDestroy()
    {
        if (!_wired || compare == null)
            return;
        compare.PicksChanged -= PicksChanged;
        compare.PairCompared -= Paired;
    }

    /// <summary>Listens to the compare, Cancel and the findings' clicks (once; the board is driven while the app's window may be closed).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (compare != null)
        {
            compare.PicksChanged += PicksChanged;
            compare.PairCompared += Paired;
        }
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Release);
        if (findings != null)
            findings.Clicked += Revisit;
        ShowStatus(FindingLook.Info, null);
    }

    /// <summary>A traveller is presented: the findings, the line and anything held go.</summary>
    public void BeginCase(CaseInstance inst)
    {
        Wire();
        _case = inst;
        Reset();
    }

    /// <summary>The decision: the same, until the next traveller.</summary>
    public void EndCase()
    {
        Wire();
        _case = null;
        Reset();
    }

    /// <summary>The idle status line's hint: the current step's sentence (GuideBar; empty while the player hides the hints).</summary>
    public void SetIdleHint(string hint)
    {
        _hint = hint ?? string.Empty;
        if (idleText != null)
            idleText.text = _hint;
    }

    /// <summary>Lets go of what is held (Cancel, Esc).</summary>
    public void Release()
    {
        Wire();
        _special = null;
        _holdKey = null;
        if (compare != null && compare.Holding)
            Quietly(compare.Clear);
        if (lines != null)
            lines.Clear();
        ShowStatus(FindingLook.Info, null);
        Changed?.Invoke();
    }

    /// <summary>
    /// Rule <paramref name="index"/> of today's memo was clicked (RulesView):
    /// held against the value held, else held itself (clicked again: let go;
    /// another rule or the date held: nothing to compare).
    /// </summary>
    public void PickRule(int index, TravelRuleSO rule)
    {
        if (rule == null)
            return;
        PickSpecial(new Held { Key = EntryKeys.Rule(index), Title = UiText.Get("app.tab.rules"), Value = rule.Summary(), Rule = rule });
    }

    /// <summary>The calendar's today (<paramref name="todayText"/>, as the papers print dates) was clicked (CalendarView): as a rule is.</summary>
    public void PickToday(string todayText)
    {
        PickSpecial(new Held { Key = EntryKeys.CalendarToday, Title = UiText.Get("app.tab.calendar"), Value = todayText ?? string.Empty, Today = true });
    }

    /// <summary>A rule or the date clicked: judged against the value held, else held (see PickRule).</summary>
    private void PickSpecial(Held special)
    {
        Wire();
        if (compare != null && compare.Holding)
        {
            ComparePick value = compare.SideA;
            Quietly(compare.Clear);
            Judge(special, value);
            return;
        }
        if (_special != null)
        {
            Held first = _special;
            _special = null;
            if (first.Key == special.Key)
            {
                Release();
                return;
            }
            Record(FindingKind.NothingToCompare, first.Key, first.Title, first.Value, special.Key, special.Title, special.Value, first.Value, What(first), What(special), false);
            return;
        }
        if (compare != null && compare.Paired)
            Quietly(compare.Clear);
        _special = special;
        _holdKey = special.Key;
        ClearLink();
        ShowStatus(FindingLook.Info, special.Today ? UiText.Format("status.holdingToday", special.Value) : UiText.Format("status.holdingRule", special.Value), true);
        Changed?.Invoke();
    }

    /// <summary>The compare's picks changed: a first value held (against a held rule or the date: judged at once), or let go.</summary>
    private void PicksChanged()
    {
        if (_quiet || compare == null)
            return;
        if (compare.Holding)
        {
            ComparePick a = compare.SideA;
            if (_special != null)
            {
                Held special = _special;
                _special = null;
                Quietly(compare.Clear);
                Judge(special, a);
                return;
            }
            _holdKey = a.Key;
            ClearLink();
            ShowStatus(FindingLook.Info, UiText.Format("status.holding", a.Shown, a.Label), true);
            Changed?.Invoke();
            return;
        }
        if (compare.Paired)
            return; // Paired says what the pair means
        if (_holdKey != null && _special == null)
        {
            _holdKey = null;
            if (_linkA == null && lines != null)
                lines.Clear();
            ShowStatus(FindingLook.Info, null);
            Changed?.Invoke();
        }
    }

    /// <summary>Two values compared: what they mean (DiscrepancyLog.Prove, then FindingRules.Classify), drawn and, when it means something, logged; the compare is let go at the end of the frame.</summary>
    private void Paired(CompareEvidence ea, CompareEvidence eb)
    {
        ComparePick a = compare.SideA, b = compare.SideB;
        _holdKey = null;
        string nation = _case != null && _case.claimedNation != null ? _case.claimedNation.id : null;
        string era = _case != null && _case.claimedEra != null ? _case.claimedEra.id : null;
        string traveller = _case != null ? _case.visitorGivenName : null;
        Discrepancy proof = _case != null ? DiscrepancyLog.Prove(ea, eb, nation, era, traveller) : null;
        FindingKind kind = FindingRules.Classify(ea, eb, proof, nation, era, traveller);
        Record(kind, a.Key, a.Label, a.Shown, b.Key, b.Label, b.Shown, FindingsView.What(a.Label), FindingsView.What(a.Label), FindingsView.What(b.Label), proof != null);
        _clearA = a.Key;
        _clearB = b.Key;
    }

    /// <summary>A rule or the date held against a value: RuleChecks or AgainstToday, drawn and logged as a pair is (the special first).</summary>
    private void Judge(Held special, ComparePick value)
    {
        CompareEvidence e = value.Evidence;
        FindingKind kind;
        if (special.Today)
            kind = _case != null && _case.facts != null && _case.facts.Today.HasValue
                ? FindingRules.AgainstToday(e.category, e.value, _case.facts.Today.Value)
                : FindingKind.DifferentDetails;
        else
            kind = FindingRules.Of(RuleChecks.Check(special.Rule.Directive, e.category, e.value, _case != null ? _case.facts : null,
                                                    _case != null && _case.claimedNation != null ? _case.claimedNation.id : null,
                                                    _case != null && _case.claimedEra != null ? _case.claimedEra.id : null));
        string subject = special.Today ? FindingsView.What(value.Label) : special.Value;
        Record(kind, special.Key, special.Title, special.Value, value.Key, value.Label, value.Shown, subject, What(special), FindingsView.What(value.Label), false);
    }

    /// <summary>What a held rule or the date is, in a note ("Today's date", "The rule").</summary>
    private static string What(Held special) => UiText.Get(special.Today ? "finding.what.today" : "finding.what.rule");

    /// <summary>A result: the line between the two values, the status line (a note names <paramref name="whatA"/> and <paramref name="whatB"/>) and, for a logged kind, the findings, titled by <paramref name="subject"/> (a pair already logged is only shown again).</summary>
    private void Record(FindingKind kind, string keyA, string titleA, string valueA, string keyB, string titleB, string valueB, string subject, string whatA,
                        string whatB, bool proof)
    {
        var finding = new Finding(kind, keyA, keyB, titleA, valueA, titleB, valueB, subject, proof);
        FindingLook look = FindingRules.Look(kind);
        _holdKey = null;
        Link(keyA, keyB, look, UiText.Get(FindingRules.LinkKey(kind)));
        if (!FindingRules.IsLogged(kind))
            ShowStatus(FindingLook.Info, UiText.Format("finding.note." + kind, whatA, whatB));
        else if (!_log.Add(finding))
            ShowStatus(FindingLook.Info, UiText.Get("status.already"));
        else
        {
            ShowStatus(look, UiText.Format(proof ? "status.evidence" : "status.logged", FindingsView.Title(finding)));
            if (findings != null)
                findings.Show(_log);
        }
        Changed?.Invoke();
    }

    /// <summary>A finding clicked in the column: its two documents opened (the first value's on the left, the second's on the right) and its line shown again.</summary>
    private void Revisit(Finding finding)
    {
        if (finding == null || app == null)
            return;
        app.OpenPair(Target(finding.KeyA), Target(finding.KeyB));
        Link(finding.KeyA, finding.KeyB, FindingRules.Look(finding.Kind), UiText.Get(FindingRules.LinkKey(finding.Kind)));
        ShowStatus(FindingRules.Look(finding.Kind), UiText.Format("status.shown", FindingsView.Title(finding)));
    }

    /// <summary>Where a value's key is read: its paper, record, book row or line (SmartLinks.ForKey), a rule's memo, the calendar.</summary>
    private LinkTarget Target(string key)
    {
        if (key == EntryKeys.CalendarToday)
            return LinkTarget.ToTab(AppTab.Calendar);
        if (EntryKeys.TryRule(key, out _))
            return LinkTarget.ToTab(AppTab.Rules);
        return app.LinkFor(key);
    }

    /// <summary>The line between two keys, redrawn each frame as their rows move.</summary>
    private void Link(string keyA, string keyB, FindingLook look, string label)
    {
        _linkA = keyA;
        _linkB = keyB;
        _linkLook = look;
        _linkLabel = label;
        DrawLine();
    }

    /// <summary>No line.</summary>
    private void ClearLink()
    {
        _linkA = _linkB = null;
        if (lines != null)
            lines.Clear();
    }

    /// <summary>Everything goes: the findings, what is held, the line, the status line back to the hint.</summary>
    private void Reset()
    {
        _log.Clear();
        _special = null;
        _holdKey = null;
        _clearA = _clearB = null;
        ClearLink();
        if (findings != null)
            findings.Show(_log);
        ShowStatus(FindingLook.Info, null);
        Changed?.Invoke();
    }

    /// <summary>After a pair (every listener has read the sides): the compare is let go, so the next click holds anew; the rows keep their line.</summary>
    private void LateUpdate()
    {
        if (_clearA != null && compare != null && compare.Paired && compare.SideA.Key == _clearA && compare.SideB.Key == _clearB)
            Quietly(compare.Clear);
        _clearA = _clearB = null;
        DrawLine();
    }

    /// <summary>The line: the held value's box and the dashed line to the pointer, or the two compared values' boxes joined, found by key in the showing panes.</summary>
    private void DrawLine()
    {
        if (lines == null)
            return;
        if (_holdKey != null)
            lines.ShowHold(Row(_holdKey));
        else if (_linkA != null)
            lines.ShowLink(Row(_linkA), Row(_linkB), _linkLook, _linkLabel);
    }

    /// <summary>The box of the row with <paramref name="key"/> in a showing pane, or null.</summary>
    private RectTransform Row(string key) => app != null ? app.FindRow(key, _rows) : null;

    /// <summary>Runs a compare change without hearing it back.</summary>
    private void Quietly(Action change)
    {
        _quiet = true;
        try
        {
            change();
        }
        finally
        {
            _quiet = false;
        }
    }

    /// <summary>The status line: the plate of <paramref name="look"/> with <paramref name="line"/> (null: the teaching hint), or the holding plate.</summary>
    private void ShowStatus(FindingLook look, string line, bool holding = false)
    {
        bool idle = line == null && !holding;
        Set(idleText, idle, _hint);
        Set(holdText, holding, line);
        Set(matchText, !idle && !holding && look == FindingLook.Match, line);
        Set(differText, !idle && !holding && look == FindingLook.Differ, line);
        Set(infoText, !idle && !holding && look == FindingLook.Info, line);
    }

    /// <summary>One plate (the text's parent) on or off with its line.</summary>
    private static void Set(TMP_Text text, bool on, string line)
    {
        if (text == null)
            return;
        GameObject plate = text.transform.parent.gameObject;
        if (plate.activeSelf != on)
            plate.SetActive(on);
        if (on)
            text.text = line ?? string.Empty;
    }
}
