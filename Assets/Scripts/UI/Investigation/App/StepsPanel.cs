using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The Investigation app's optional steps checklist, the sidebar's Steps
/// section (the PC redesign ST1-ST4; redesign phase 21). While a traveller is
/// at the desk it lists their kind's steps (CaseSteps.Resolve over the
/// library's pc.steps), each with its tick and, with parts, its progress
/// ("Read every paper (1/3)"); between travellers it says steps show then. A
/// step ticks when the player made the check, never on what the check found
/// (CaseProgress, fed by the façade with the case's events and here with what
/// the player sees: the Rules tab or a scanned paper shown while the PC is
/// looked at, a record looked up). The tick box ticks or unticks a step by
/// hand for the rest of the case; the label jumps where the step is done
/// (CaseSteps.Target: a tab, a record looked up, a book) or, for work at the
/// desk, toasts a hint. The toolbar's Steps, Settings and phase 20's key
/// (Toggle) show or hide the list, remembered per player
/// (DesktopPreferences.StepsShown); hidden, the section collapses to a line.
/// The app's window may be closed while a case runs, so nothing here waits for
/// Awake.
/// </summary>
public sealed class StepsPanel : MonoBehaviour
{
    [Header("The section")]
    /// <summary>The rows' scrolling list (hidden with the steps and between travellers).</summary>
    [SerializeField] private GameObject list;

    /// <summary>The list's content, where the rows go.</summary>
    [SerializeField] private RectTransform rowsRoot;

    /// <summary>A row (inactive), cloned per step.</summary>
    [SerializeField] private StepRowView rowTemplate;

    /// <summary>The line shown instead of the list: steps are hidden, or no traveller is at the desk.</summary>
    [SerializeField] private TMP_Text stateText;

    [Header("The app")]
    /// <summary>The app: the tab the player sees, and the tab a jump shows.</summary>
    [SerializeField] private InvestigationApp app;

    /// <summary>The Documents tab: the scanned paper the player sees.</summary>
    [SerializeField] private DocumentsView documents;

    /// <summary>The Records tab's lookup: a lookup ticks "a record looked up"; a jump looks the primary paper's record up.</summary>
    [SerializeField] private CitizenRecordsWindowController records;

    /// <summary>The Reference tab: a jump chooses a book.</summary>
    [SerializeField] private ReferenceView reference;

    /// <summary>The desktop's toast: a hint for a step done at the desk.</summary>
    [SerializeField] private AppToast toast;

    /// <summary>The desktop's knobs (the toast's time).</summary>
    [SerializeField] private DesktopConfigSO config;

    [Header("The PC")]
    /// <summary>The PC's screen: the player looks at the desktop while it takes input (the frame open, the screen on).</summary>
    [SerializeField] private MonitorScreen screen;

    private readonly List<StepRowView> _rows = new List<StepRowView>();
    private IReadOnlyList<StepSpec> _steps = Array.Empty<StepSpec>();
    private List<StepState> _states = new List<StepState>();
    private CaseProgress _progress;
    private bool _wired;
    private int _seenTab = -2;
    private int _seenPaper = -2;

    /// <summary>Raised when the steps are shown or hidden (Settings repaints its choice).</summary>
    public event Action ShownChanged;

    /// <summary>Shows the steps when hidden, hides them when shown (the toolbar's Steps; phase 20's key).</summary>
    public void Toggle() => SetShown(!DesktopPreferences.StepsShown);

    /// <summary>Shows or hides the steps and remembers it for the player.</summary>
    public void SetShown(bool shown)
    {
        Wire();
        DesktopPreferences.StepsShown = shown;
        Apply();
        ShownChanged?.Invoke();
    }

    /// <summary>
    /// A traveller is presented: their kind's steps on <paramref name="day"/>
    /// from <paramref name="sets"/>, counted over their papers, today's
    /// question categories and the books' categories; nothing done yet.
    /// </summary>
    public void BeginCase(StepSetData sets, TravellerKind kind, int day, IReadOnlyList<StepPaper> papers, IEnumerable<ClueCategory> questions,
                          IEnumerable<ClueCategory> books)
    {
        Wire();
        _steps = CaseSteps.Resolve(sets, kind.ToString(), day);
        _progress = new CaseProgress(papers, questions, books);
        _seenTab = -2;
        _seenPaper = -2;
        BuildRows();
        Redraw();
    }

    /// <summary>The decision: the steps go until the next traveller.</summary>
    public void EndCase()
    {
        Wire();
        _progress = null;
        _steps = Array.Empty<StepSpec>();
        _states.Clear();
        BuildRows();
        Apply();
    }

    /// <summary>Paper <paramref name="paper"/> was handed over.</summary>
    public void Received(int paper) => Changed(_progress != null && _progress.Received(paper));

    /// <summary>Paper <paramref name="paper"/> was lifted into the hand at the desk.</summary>
    public void Read(int paper) => Changed(_progress != null && _progress.Read(paper));

    /// <summary>A paper of form <paramref name="kind"/> was asked for through the wheel.</summary>
    public void Requested(string kind) => Changed(_progress != null && _progress.Requested(kind));

    /// <summary>A pair was compared (whatever it showed).</summary>
    public void Compared(CompareEvidence a, CompareEvidence b) => Changed(_progress != null && _progress.Compared(a, b));

    /// <summary>The traveller answered in <paramref name="category"/>.</summary>
    public void Asked(ClueCategory category) => Changed(_progress != null && _progress.Asked(category));

    /// <summary>A garment was looked at.</summary>
    public void LookedAt() => Changed(_progress != null && _progress.LookedAt());

    private void OnEnable()
    {
        Wire();
        Apply();
    }

    private void OnDestroy()
    {
        if (_wired && records != null)
            records.Searched -= HandleSearched;
    }

    /// <summary>What the player sees while looking at the PC: the Rules tab, or a scanned paper in Documents (checked when either changes).</summary>
    private void LateUpdate()
    {
        if (_progress == null || app == null)
            return;
        bool looking = app.IsShowing && (screen == null || screen.IsInteractive);
        int tab = looking ? (int)app.ActiveTab : -1;
        int paper = tab == (int)AppTab.Documents && documents != null && documents.ShowsCopy ? documents.Selected : -1;
        if (tab == _seenTab && paper == _seenPaper)
            return;
        _seenTab = tab;
        _seenPaper = paper;

        bool changed = tab == (int)AppTab.Rules && _progress.RulesViewed();
        changed |= paper >= 0 && _progress.Read(paper);
        Changed(changed);
    }

    /// <summary>Listens to the Records lookup and hides the row template (once; the panel may be driven while its window is closed).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        if (records != null)
            records.Searched += HandleSearched;
        if (rowTemplate != null)
            rowTemplate.gameObject.SetActive(false);
    }

    private void HandleSearched() => Changed(_progress != null && _progress.RecordViewed());

    private void Changed(bool changed)
    {
        if (changed)
            Redraw();
    }

    /// <summary>A row per resolved step (the rows of the last case go).</summary>
    private void BuildRows()
    {
        foreach (StepRowView row in _rows)
            if (row != null)
                Destroy(row.gameObject);
        _rows.Clear();
        if (rowTemplate == null || rowsRoot == null)
            return;

        for (int i = 0; i < _steps.Count; i++)
        {
            StepRowView row = Instantiate(rowTemplate, rowsRoot);
            row.gameObject.name = "Step_" + _steps[i].id;
            int index = i;
            if (row.Box != null)
                row.Box.onClick.AddListener(() => TickByHand(index));
            if (row.Label != null)
                row.Label.onClick.AddListener(() => Jump(index));
            _rows.Add(row);
        }
    }

    /// <summary>Each row from the steps' states (a step with no parts this case hides), then the section's visibility.</summary>
    private void Redraw()
    {
        _states = _progress != null ? CaseSteps.Evaluate(_steps, _progress) : new List<StepState>();
        for (int i = 0; i < _rows.Count; i++)
        {
            int s = IndexOfState(_steps[i].id);
            bool listed = s >= 0;
            if (_rows[i].gameObject.activeSelf != listed)
                _rows[i].gameObject.SetActive(listed);
            if (!listed)
                continue;
            StepState state = _states[s];
            string name = UiText.Get(StepSets.LabelKey(state.Id));
            _rows[i].Show(state.Done, state.Need > 1 ? UiText.Format("steps.progress", name, state.Have, state.Need) : name);
        }
        Apply();
    }

    /// <summary>The list shows while steps are shown and a traveller is at the desk; the line says why it does not otherwise.</summary>
    private void Apply()
    {
        bool shown = DesktopPreferences.StepsShown;
        bool listing = shown && _progress != null;
        if (list != null && list.activeSelf != listing)
            list.SetActive(listing);
        if (stateText == null)
            return;
        if (stateText.gameObject.activeSelf == listing)
            stateText.gameObject.SetActive(!listing);
        stateText.text = UiText.Get(shown ? "steps.none" : "steps.hidden");
    }

    /// <summary>The tick box: the step ticked or unticked by hand, for the rest of the case.</summary>
    private void TickByHand(int index)
    {
        if (_progress == null || index < 0 || index >= _steps.Count)
            return;
        int s = IndexOfState(_steps[index].id);
        _progress.SetManual(_steps[index].id, s < 0 || !_states[s].Done);
        Redraw();
    }

    /// <summary>The label: to where the step is done, in the app (or a hint toast for work at the desk).</summary>
    private void Jump(int index)
    {
        if (_progress == null || index < 0 || index >= _steps.Count || app == null)
            return;
        StepTarget target = CaseSteps.Target(_steps[index], _progress);
        switch (target.Kind)
        {
            case StepTargetKind.Hint:
                if (toast != null)
                    toast.Show(UiText.Get(target.HintKey), config != null ? config.toastSeconds : 4f, null);
                break;
            case StepTargetKind.Tab:
                app.ShowTab(target.Tab);
                break;
            case StepTargetKind.Record:
                app.ShowTab(AppTab.Records);
                if (records != null)
                    records.Lookup(target.Query);
                break;
            case StepTargetKind.Book:
                app.ShowTab(AppTab.Reference);
                if (reference != null)
                    reference.ShowBook(target.Book);
                break;
        }
    }

    private int IndexOfState(string id)
    {
        for (int i = 0; i < _states.Count; i++)
            if (_states[i].Id == id)
                return i;
        return -1;
    }
}
