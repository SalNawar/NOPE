using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Investigation app's guided steps, headless since the desk-first
/// redesign (Saleh 2026-10-05, items 8 and 10: the desk leads, the PC's menu
/// bar replaced the step pills and the foot) and with no decision since the PC
/// clean-up of the same day (the verdict is the stamp on the passport: the PC
/// only investigates): the steps (Papers, Records, Books, Rules;
/// CaseGuide.StagesOn: those owning a checklist item that day) put a pair of
/// documents up when a traveller is presented and when the keys go to a step
/// (Ctrl+1…4, Ctrl+Tab; StageShown: the app puts up the pair). The case's
/// progress (CaseProgress, fed by the façade with the case's events: papers
/// handed over, asked for and read at the desk, pairs compared, answers
/// heard, garments looked at, records looked up) tells the Records and Books
/// steps where to look. The app's
/// window may be closed while a case runs, so nothing here waits for Awake.
/// </summary>
public sealed class GuideBar : MonoBehaviour
{
    private readonly CaseGuide _guide = new CaseGuide();
    private List<GuideStage> _dayStages;
    private CaseProgress _progress;

    /// <summary>Raised when a step is gone to (the app puts up its pair).</summary>
    public event Action<GuideStage> StageShown;

    /// <summary>The current step.</summary>
    public GuideStage Current => _guide.Current;

    /// <summary>
    /// A traveller is presented: the first step of those shown on
    /// <paramref name="day"/> (CaseGuide.StagesOn over <paramref name="sets"/>),
    /// the progress counted over their papers, today's question categories and
    /// the books'; its pair goes up.
    /// </summary>
    public void BeginCase(StepSetData sets, int day, IReadOnlyList<StepPaper> papers, IEnumerable<ClueCategory> questions, IEnumerable<ClueCategory> books)
    {
        _progress = new CaseProgress(papers, questions, books);
        _dayStages = CaseGuide.StagesOn(sets, day);
        _guide.Reset(_dayStages);
        StageShown?.Invoke(_guide.Current);
    }

    /// <summary>The traveller was decided at the desk: the steps wait for the next one.</summary>
    public void EndCase()
    {
        _progress = null;
        _guide.Reset(_dayStages);
    }

    /// <summary>Goes to <paramref name="stage"/> (any step shown, at any time) and puts its pair up.</summary>
    public void Go(GuideStage stage)
    {
        if (_progress == null || !_guide.IsShown(stage))
            return;
        _guide.Go(stage);
        StageShown?.Invoke(_guide.Current);
    }

    /// <summary>Goes to the step shown at <paramref name="position"/> (1-based, Ctrl+1…4: the steps shown today, in order); none past the last.</summary>
    public void GoTo(int position)
    {
        if (position >= 1 && position <= _guide.Count)
            Go(_guide.Shown[position - 1]);
    }

    /// <summary>The next (1) or previous (-1) step (Ctrl+Tab, Ctrl+Shift+Tab), no further than the ends.</summary>
    public void Step(int direction)
    {
        if (_progress == null)
            return;
        if (direction > 0 ? _guide.Next() : _guide.Back())
            StageShown?.Invoke(_guide.Current);
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

    /// <summary>Paper <paramref name="paper"/> was handed over.</summary>
    public void Received(int paper) => _progress?.Received(paper);

    /// <summary>Paper <paramref name="paper"/> was lifted into the hand at the desk.</summary>
    public void Read(int paper) => _progress?.Read(paper);

    /// <summary>A paper of form <paramref name="kind"/> was asked for through the wheel.</summary>
    public void Requested(string kind) => _progress?.Requested(kind);

    /// <summary>A pair was compared (whatever it showed): the Books step's target moves on past a field held against a book.</summary>
    public void Compared(CompareEvidence a, CompareEvidence b) => _progress?.Compared(a, b);

    /// <summary>The traveller answered in <paramref name="category"/>.</summary>
    public void Asked(ClueCategory category) => _progress?.Asked(category);

    /// <summary>A garment was looked at.</summary>
    public void LookedAt() => _progress?.LookedAt();

    /// <summary>A record was looked up in a Records view.</summary>
    public void RecordViewed() => _progress?.RecordViewed();
}
