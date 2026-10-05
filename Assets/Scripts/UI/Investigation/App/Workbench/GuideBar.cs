using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Investigation app's guided steps (the PC workbench spec IA2, IA3; W1:
/// one step at a time; steps suggest, they never lock; polished in wave 5
/// A3): the steps as one segmented control in the header (Papers, Records,
/// Books, Rules, Decision: the current one filled with the primary colour, a
/// finished one ticked), the step's title and its one sentence on the status
/// line while nothing is held, and the foot (Back, one short progress line:
/// the step's next check and how many more, Next). A step is finished when the day's checklist items
/// that belong to it are done (CaseGuide over CaseSteps.Evaluate: the
/// default set until a paper handed over on arrival is read, then the
/// kind's; progress by id, fed by the façade with the case's events and here
/// with what the player sees: the Rules or a scanned paper shown while the PC
/// is looked at), or, with none, once left. The steps follow the day's
/// ramp (CaseGuide.StagesOn: a step owning no checklist item that day is
/// hidden, the pills numbered over those shown). Going to a step raises
/// StageShown (the app puts up its pair of documents). The step hints (the
/// step's sentence on the status line) show or hide from
/// Settings and Ctrl+Shift+S, remembered per player (DesktopPreferences.StepsShown).
/// Since the desk-first redesign (Saleh 2026-10-05, item 10: "far too much to
/// check"; the desk leads now) none of this shows: the pills' panel and the
/// foot are hidden when it wires, and the status line gets no step hint. The
/// steps still run headless (the keys and Go reach a step, the decision
/// view), until the PC's menu bar replaces them.
/// The app's window may be closed while a case runs, so nothing here waits
/// for Awake.
/// </summary>
public sealed class GuideBar : MonoBehaviour
{
    [Header("The steps (one pill per CaseGuide stage, in order)")]
    /// <summary>The pills: each holds its plates (Current; circles Circle, CircleCurrent, CircleDone) and labels (Label, LabelCurrent).</summary>
    [SerializeField] private Button[] pills = new Button[0];

    [Header("The foot")]
    /// <summary>The previous step (hidden on the first).</summary>
    [SerializeField] private Button backButton;

    /// <summary>The next step, "Next: Records" (hidden on the decision).</summary>
    [SerializeField] private Button nextButton;

    /// <summary>Next's label.</summary>
    [SerializeField] private TMP_Text nextLabel;

    /// <summary>"Step 2 of 4 · Next: Visa class against the account (+1 more)" (one line).</summary>
    [SerializeField] private TMP_Text progressText;

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
        _dayStages = CaseGuide.StagesOn(sets, day);
        _guide.Reset(_dayStages);
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
        if (pills.Length > 0 && pills[0] != null)
            pills[0].transform.parent.gameObject.SetActive(false);
        if (backButton != null)
            backButton.transform.parent.gameObject.SetActive(false);
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

    /// <summary>The pills (current, finished), the foot and the status line's hint (the step's title, and its sentence while the hints show) from the guide and the checklist's states.</summary>
    private void Redraw()
    {
        if (_progress != null)
            CaseSteps.Evaluate(_steps, _progress, _states);
        else
            _states.Clear();
        bool caseOn = _progress != null;
        for (int i = 0; i < pills.Length && i < CaseGuide.Stages.Count; i++)
        {
            GuideStage stage = CaseGuide.Stages[i];
            bool current = caseOn && stage == _guide.Current;
            bool done = caseOn && !current && _guide.IsDone(stage, CaseGuide.Checks(stage, _steps, _states));
            int number = 0;
            for (int at = 0; at < _guide.Shown.Count; at++)
                if (_guide.Shown[at] == stage)
                    number = at + 1;
            Pill(pills[i], number, current, done, caseOn);
        }
        if (board != null)
            board.SetIdleHint(caseOn ? string.Empty : UiText.Get("idle.waiting"));

        if (backButton != null)
            backButton.gameObject.SetActive(caseOn && !_guide.IsFirst);
        if (nextButton != null)
            nextButton.gameObject.SetActive(caseOn && !_guide.IsLast);
        if (nextLabel != null && !_guide.IsLast)
            nextLabel.text = UiText.Format("guide.next", UiText.Get("guide." + _guide.Shown[_guide.Number].ToString().ToLowerInvariant() + ".name"));
        if (progressText != null)
            progressText.text = caseOn ? Progress() : string.Empty;
    }

    /// <summary>"Step 2 of 4 · Next: Class (+1 more)" (numbered over the steps shown; the current step's first checklist item not done yet, by its label, and how many more wait), "… · All checks here done", or the step alone when it owns none. One line: the foot never wraps.</summary>
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
            return UiText.Format("guide.progress", _guide.Number, _guide.Count);
        if (left.Count == 0)
            return UiText.Format("guide.progressDone", _guide.Number, _guide.Count);
        string next = left.Count > 1 ? UiText.Format("guide.progressMore", left[0], left.Count - 1) : left[0];
        return UiText.Format("guide.progressNext", _guide.Number, _guide.Count, next);
    }

    /// <summary>One pill: hidden when its step is not shown today (<paramref name="number"/> 0), else numbered among those shown, its plate while current, its circle (the number, the current number, the tick), its label (muted unless current); inert between travellers.</summary>
    private static void Pill(Button pill, int number, bool current, bool done, bool caseOn)
    {
        if (pill == null)
            return;
        if (pill.gameObject.activeSelf != number > 0)
            pill.gameObject.SetActive(number > 0);
        if (number == 0)
            return;
        Number(pill.transform, "Circle", number);
        Number(pill.transform, "CircleCurrent", number);
        pill.interactable = caseOn;
        Show(pill.transform, "Current", current);
        Show(pill.transform, "Circle", !current && !done);
        Show(pill.transform, "CircleCurrent", current);
        Show(pill.transform, "CircleDone", done);
        Show(pill.transform, "Label", !current);
        Show(pill.transform, "LabelCurrent", current);
    }

    /// <summary>A pill's circle reads its step's number among those shown.</summary>
    private static void Number(Transform pill, string circle, int number)
    {
        Transform text = pill.Find(circle + "/Number");
        if (text != null && text.TryGetComponent(out TMP_Text label))
        {
            string value = number.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (label.text != value)
                label.text = value;
        }
    }

    /// <summary>A pill's part on or off.</summary>
    private static void Show(Transform pill, string part, bool on)
    {
        Transform t = pill.Find(part);
        if (t != null && t.gameObject.activeSelf != on)
            t.gameObject.SetActive(on);
    }
}
