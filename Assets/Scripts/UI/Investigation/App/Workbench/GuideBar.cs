using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Investigation app's guided steps, headless since the desk-first
/// redesign (Saleh 2026-10-05, items 8 and 10: the desk leads, the PC's menu
/// bar replaced the step pills and the foot): the steps (Papers, Records,
/// Books, Rules, Decision; CaseGuide.StagesOn: those owning a checklist item
/// that day) still put a pair of documents up when a traveller is presented
/// and when the keys go to a step (Ctrl+1…5, Ctrl+Tab; StageShown: the app
/// puts up the pair, the decision view at the Decision step), and they
/// follow the day's checklist (CaseGuide over CaseSteps.Evaluate: the
/// default set until a paper handed over on arrival is read, then the
/// kind's; fed by the façade with the case's events and here with what the
/// player sees: the Rules or a scanned paper shown while the PC is looked
/// at). The status line reads "waiting" between travellers. The step hints'
/// choice is still remembered (Settings, Ctrl+Shift+S;
/// DesktopPreferences.StepsShown). The app's window may be closed while a
/// case runs, so nothing here waits for Awake.
/// </summary>
public sealed class GuideBar : MonoBehaviour
{
    [Header("The app")]
    /// <summary>The app: what the player sees (the showing panes' sources and scanned copies).</summary>
    [SerializeField] private InvestigationApp app;

    /// <summary>The workbench (its status line reads "waiting" between travellers; no step hint since the desk-first redesign).</summary>
    [SerializeField] private MatchBoard board;

    /// <summary>The PC's screen: the player looks at the desktop while it takes input (the frame open, the screen on).</summary>
    [SerializeField] private MonitorScreen screen;

    private readonly CaseGuide _guide = new CaseGuide();
    private List<GuideStage> _dayStages;
    private readonly List<StepState> _states = new List<StepState>();
    private readonly List<int> _copies = new List<int>();
    private IReadOnlyList<StepSpec> _steps = Array.Empty<StepSpec>();
    private CaseProgress _progress;
    private StepSetData _sets;
    private string _kind = string.Empty;
    private string _setName = string.Empty;
    private int _day;

    /// <summary>Raised when a step is gone to (the app puts up its pair).</summary>
    public event Action<GuideStage> StageShown;

    /// <summary>Raised when the hints are shown or hidden (Settings repaints its choice).</summary>
    public event Action ShownChanged;

    /// <summary>The current step.</summary>
    public GuideStage Current => _guide.Current;

    /// <summary>True while a traveller is at the desk (the steps work).</summary>
    public bool CaseOn => _progress != null;

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
        _sets = sets;
        _kind = kind.ToString();
        _day = day;
        _progress = new CaseProgress(papers, questions, books);
        _setName = string.Empty;
        _dayStages = CaseGuide.StagesOn(sets, day);
        _guide.Reset(_dayStages);
        ResolveSet();
        Redraw();
        StageShown?.Invoke(_guide.Current);
    }

    /// <summary>The decision: the steps wait for the next traveller.</summary>
    public void EndCase()
    {
        _progress = null;
        _sets = null;
        _setName = string.Empty;
        _steps = Array.Empty<StepSpec>();
        _states.Clear();
        _guide.Reset(_dayStages);
        Redraw();
    }

    /// <summary>Goes to <paramref name="stage"/> (a pill; any step shown, at any time) and puts its pair up.</summary>
    public void Go(GuideStage stage)
    {
        if (_progress == null || !_guide.IsShown(stage))
            return;
        _guide.Go(stage);
        Redraw();
        StageShown?.Invoke(_guide.Current);
    }

    /// <summary>Goes to the step shown at <paramref name="position"/> (1-based, Ctrl+1…5: the steps shown today, in order); none past the last.</summary>
    public void GoTo(int position)
    {
        if (position >= 1 && position <= _guide.Count)
            Go(_guide.Shown[position - 1]);
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

    private void OnEnable() => Redraw();

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

    /// <summary>The checklist's states from the guide's progress, and the status line's idle words between travellers.</summary>
    private void Redraw()
    {
        if (_progress != null)
            CaseSteps.Evaluate(_steps, _progress, _states);
        else
            _states.Clear();
        if (board != null)
            board.SetIdleHint(_progress != null ? string.Empty : UiText.Get("idle.waiting"));
    }
}
