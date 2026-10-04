using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's guided steps (the PC workbench spec IA2, IA3; W1:
/// one step at a time; steps suggest, they never lock): the five step pills
/// in the header (Papers, Records, Books, Rules, Decision: the current one
/// filled with the primary colour, a finished one ticked), the lead over the
/// work (the step's title; its one sentence is the status line's hint while
/// nothing is held), and the foot (Back, the progress line, Next). A step is finished when the day's checklist items
/// that belong to it are done (CaseGuide over CaseSteps.Evaluate: the
/// default set until a paper handed over on arrival is read, then the
/// kind's; progress by id, fed by the façade with the case's events and here
/// with what the player sees: the Rules or a scanned paper shown while the PC
/// is looked at), or, with none, once left. Going to a step raises
/// StageShown (the app puts up its pair of documents). The step hints (the
/// step's sentence on the status line) show or hide from
/// Settings and Ctrl+Shift+S, remembered per player (DesktopPreferences.StepsShown).
/// The app's window may be closed while a case runs, so nothing here waits
/// for Awake.
/// </summary>
public sealed class GuideBar : MonoBehaviour
{
    [Header("The steps (one pill per CaseGuide stage, in order)")]
    /// <summary>The pills: each holds its plates (Current; circles Circle, CircleCurrent, CircleDone) and labels (Label, LabelCurrent).</summary>
    [SerializeField] private Button[] pills = new Button[0];

    [Header("The lead")]
    /// <summary>The step's title (its sentence is the status line's hint, MatchBoard.SetIdleHint).</summary>
    [SerializeField] private TMP_Text leadTitle;

    [Header("The foot")]
    /// <summary>The previous step (hidden on the first).</summary>
    [SerializeField] private Button backButton;

    /// <summary>The next step, "Next: Records" (hidden on the decision).</summary>
    [SerializeField] private Button nextButton;

    /// <summary>Next's label.</summary>
    [SerializeField] private TMP_Text nextLabel;

    /// <summary>"Step 2 of 5 · 1 of 3 checks done".</summary>
    [SerializeField] private TMP_Text progressText;

    [Header("The app")]
    /// <summary>The app: what the player sees (the showing panes' sources and scanned copies).</summary>
    [SerializeField] private InvestigationApp app;

    /// <summary>The workbench (its status line's teaching hint follows the hints).</summary>
    [SerializeField] private MatchBoard board;

    /// <summary>The PC's screen: the player looks at the desktop while it takes input (the frame open, the screen on).</summary>
    [SerializeField] private MonitorScreen screen;

    private readonly CaseGuide _guide = new CaseGuide();
    private readonly List<StepState> _states = new List<StepState>();
    private readonly List<int> _copies = new List<int>();
    private IReadOnlyList<StepSpec> _steps = Array.Empty<StepSpec>();
    private CaseProgress _progress;
    private StepSetData _sets;
    private string _kind = string.Empty;
    private string _setName = string.Empty;
    private int _day;
    private bool _wired;

    /// <summary>Raised when a step is gone to (the app puts up its pair).</summary>
    public event Action<GuideStage> StageShown;

    /// <summary>Raised when the hints are shown or hidden (Settings repaints its choice).</summary>
    public event Action ShownChanged;

    /// <summary>The current step.</summary>
    public GuideStage Current => _guide.Current;

    /// <summary>True while a traveller is at the desk (the steps work).</summary>
    public bool CaseOn => _progress != null;

    /// <summary>The pills, in order (the keys' Steps region).</summary>
    public IReadOnlyList<Button> Pills => pills;

    /// <summary>
    /// A traveller is presented: the first step, the default checklist on
    /// <paramref name="day"/> from <paramref name="sets"/> (their
    /// <paramref name="kind"/>'s once a paper handed over on arrival is read),
    /// counted over their papers, today's question categories and the books';
    /// nothing done yet.
    /// </summary>
    public void BeginCase(StepSetData sets, TravellerKind kind, int day, IReadOnlyList<StepPaper> papers, IEnumerable<ClueCategory> questions,
                          IEnumerable<ClueCategory> books)
    {
        Wire();
        _sets = sets;
        _kind = kind.ToString();
        _day = day;
        _progress = new CaseProgress(papers, questions, books);
        _setName = string.Empty;
        _guide.Reset();
        ResolveSet();
        Redraw();
        StageShown?.Invoke(_guide.Current);
    }

    /// <summary>The decision: the steps wait for the next traveller.</summary>
    public void EndCase()
    {
        Wire();
        _progress = null;
        _sets = null;
        _setName = string.Empty;
        _steps = Array.Empty<StepSpec>();
        _states.Clear();
        _guide.Reset();
        Redraw();
    }

    /// <summary>Goes to <paramref name="stage"/> (a pill, Ctrl+1…5; any step, at any time) and puts its pair up.</summary>
    public void Go(GuideStage stage)
    {
        if (_progress == null)
            return;
        _guide.Go(stage);
        Redraw();
        StageShown?.Invoke(_guide.Current);
    }

    /// <summary>The next (1) or previous (-1) step (Next, Back, Ctrl+Tab), no further than the ends.</summary>
    public void Step(int direction)
    {
        if (_progress == null)
            return;
        if (direction > 0 ? _guide.Next() : _guide.Back())
        {
            Redraw();
            StageShown?.Invoke(_guide.Current);
        }
    }

    /// <summary>Where the Records step looks: the primary paper's record (by its Citizen ID, else its name; the checklist's PrimaryName jump), else Records alone.</summary>
    public LinkTarget RecordsTarget()
    {
        StepTarget target = _progress != null ? CaseSteps.Target(new StepSpec { link = StepLink.PrimaryName }, _progress) : StepTarget.Nothing;
        return target.Kind == StepTargetKind.Record ? LinkTarget.ToRecords(target.Query) : LinkTarget.ToTab(AppTab.Records);
    }

    /// <summary>Where the Books step looks: the book of the first paper field not yet held against a book (the checklist's FirstUncheckedField), else the first book.</summary>
    public LinkTarget BooksTarget()
    {
        StepTarget target = _progress != null
            ? CaseSteps.Target(new StepSpec { when = StepWhen.Compared, statement = StatementKind.Field, truth = TruthKind.Reference, link = StepLink.FirstUncheckedField }, _progress)
            : StepTarget.Nothing;
        return target.Kind == StepTargetKind.Book ? LinkTarget.ToRow(AppTab.Reference, EntryKeys.Book(target.Book)) : LinkTarget.ToTab(AppTab.Reference, 0);
    }

    /// <summary>Shows the hints when hidden, hides them when shown (Ctrl+Shift+S).</summary>
    public void Toggle() => SetShown(!DesktopPreferences.StepsShown);

    /// <summary>Shows or hides the hints and remembers it for the player (Settings).</summary>
    public void SetShown(bool shown)
    {
        Wire();
        DesktopPreferences.StepsShown = shown;
        Redraw();
        ShownChanged?.Invoke();
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

    /// <summary>A record was looked up in a Records view.</summary>
    public void RecordViewed() => Changed(_progress != null && _progress.RecordViewed());

    private void OnEnable()
    {
        Wire();
        Redraw();
    }

    /// <summary>What the player sees while looking at the PC: the Rules, or scanned papers, in either showing pane (each frame; the marks are idempotent and allocate nothing).</summary>
    private void LateUpdate()
    {
        if (_progress == null || app == null)
            return;
        bool looking = app.IsShowing && (screen == null || screen.IsInteractive);
        bool changed = looking && app.Sees(AppTab.Rules) && _progress.RulesViewed();
        _copies.Clear();
        if (looking)
            app.CopiesSeen(_copies);
        foreach (int paper in _copies)
            changed |= _progress.Read(paper);
        Changed(changed);
    }

    /// <summary>Wires the pills, Back and Next (once).</summary>
    private void Wire()
    {
        if (_wired)
            return;
        _wired = true;
        for (int i = 0; i < pills.Length && i < CaseGuide.Stages.Count; i++)
        {
            GuideStage stage = CaseGuide.Stages[i];
            if (pills[i] != null)
                pills[i].onClick.AddListener(() => Go(stage));
        }
        if (backButton != null)
            backButton.onClick.AddListener(() => Step(-1));
        if (nextButton != null)
            nextButton.onClick.AddListener(() => Step(1));
    }

    private void Changed(bool changed)
    {
        if (!changed)
            return;
        ResolveSet();
        Redraw();
    }

    /// <summary>The checklist for what has been read (the default until an arrival paper is read, then the kind's).</summary>
    private void ResolveSet()
    {
        string name = CaseSteps.SetName(_kind, _progress != null && _progress.ArrivalRead);
        if (name == _setName)
            return;
        _setName = name;
        _steps = CaseSteps.Resolve(_sets, name, _day);
    }

    /// <summary>The pills (current, finished), the lead, the foot and the status line's hint from the guide and the checklist's states.</summary>
    private void Redraw()
    {
        if (_progress != null)
            CaseSteps.Evaluate(_steps, _progress, _states);
        else
            _states.Clear();
        bool caseOn = _progress != null;
        bool hints = DesktopPreferences.StepsShown;
        for (int i = 0; i < pills.Length && i < CaseGuide.Stages.Count; i++)
        {
            GuideStage stage = CaseGuide.Stages[i];
            bool current = caseOn && stage == _guide.Current;
            bool done = caseOn && !current && _guide.IsDone(stage, CaseGuide.Checks(stage, _steps, _states));
            Pill(pills[i], current, done, caseOn);
        }

        string key = "guide." + _guide.Current.ToString().ToLowerInvariant();
        if (leadTitle != null)
            leadTitle.text = caseOn ? UiText.Get(key + ".title") : UiText.Get("idle.waiting");
        if (board != null)
            board.SetIdleHint(!hints ? string.Empty : caseOn ? UiText.Get(key + ".text") : UiText.Get("guide.idle.text"));

        if (backButton != null)
            backButton.gameObject.SetActive(caseOn && !_guide.IsFirst);
        if (nextButton != null)
            nextButton.gameObject.SetActive(caseOn && !_guide.IsLast);
        if (nextLabel != null && !_guide.IsLast)
            nextLabel.text = UiText.Format("guide.next", UiText.Get("guide." + (_guide.Current + 1).ToString().ToLowerInvariant() + ".name"));
        if (progressText != null)
            progressText.text = caseOn ? Progress() : string.Empty;
    }

    /// <summary>"Step 2 of 5 · To check: Class, Transponder" (the current step's checklist items not done yet, by their labels), "… · All checks here done", or the step alone when it owns none.</summary>
    private string Progress()
    {
        var left = new List<string>();
        int total = 0;
        foreach (StepState state in _states)
        {
            StepSpec spec = null;
            foreach (StepSpec s in _steps)
                if (s != null && s.id == state.Id)
                    spec = s;
            if (spec == null || CaseGuide.StageOf(spec) != _guide.Current)
                continue;
            total++;
            if (!state.Done)
                left.Add(state.Need > 1 ? UiText.Format("steps.progress", UiText.Get(StepSets.LabelKey(state.Id)), state.Have, state.Need)
                                        : UiText.Get(StepSets.LabelKey(state.Id)));
        }
        if (total == 0)
            return UiText.Format("guide.progress", _guide.Number, CaseGuide.Stages.Count);
        return left.Count == 0
            ? UiText.Format("guide.progressDone", _guide.Number, CaseGuide.Stages.Count)
            : UiText.Format("guide.progressChecks", _guide.Number, CaseGuide.Stages.Count, string.Join(", ", left));
    }

    /// <summary>One pill: its plate while current, its circle (the number, the current number, the tick), its label (muted unless current); inert between travellers.</summary>
    private static void Pill(Button pill, bool current, bool done, bool caseOn)
    {
        if (pill == null)
            return;
        pill.interactable = caseOn;
        Show(pill.transform, "Current", current);
        Show(pill.transform, "Circle", !current && !done);
        Show(pill.transform, "CircleCurrent", current);
        Show(pill.transform, "CircleDone", done);
        Show(pill.transform, "Label", !current);
        Show(pill.transform, "LabelCurrent", current);
    }

    /// <summary>A pill's part on or off.</summary>
    private static void Show(Transform pill, string part, bool on)
    {
        Transform t = pill.Find(part);
        if (t != null && t.gameObject.activeSelf != on)
            t.gameObject.SetActive(on);
    }
}
