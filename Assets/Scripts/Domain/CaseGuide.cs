using System;
using System.Collections.Generic;

/// <summary>
/// The Investigation app's guided steps (the PC workbench spec IA2, IA3), in
/// order. Runtime only (not serialized): Ctrl+1…5 follow this order.
/// </summary>
public enum GuideStage
{
    /// <summary>The traveller's papers and what they said, against each other.</summary>
    Papers,

    /// <summary>A paper against the agency's citizen record.</summary>
    Records,

    /// <summary>A paper against the reference books.</summary>
    Books,

    /// <summary>Today's rules against the papers.</summary>
    Rules,

    /// <summary>Accept or deny.</summary>
    Decision
}

/// <summary>A guided step's checks this case: those done of those listed (the day's steps checklist's items that belong to it).</summary>
public readonly struct StageChecks
{
    /// <summary>Checks.</summary>
    public StageChecks(int done, int total)
    {
        Done = done;
        Total = total;
    }

    /// <summary>The checks done.</summary>
    public int Done { get; }

    /// <summary>The checks listed.</summary>
    public int Total { get; }
}

/// <summary>
/// Where the clerk is in a case's guided steps (the PC workbench spec IA2,
/// IA3; W1: steps suggest, they never lock): the current step, the steps
/// left behind, and when a step is finished. A step owns the checks of the
/// day's steps checklist that belong to it (StageOf: papers received, read
/// and asked for, the answers heard, the look and paper-against-paper
/// compares belong to Papers; a record looked up and compares against a
/// record to Records; compares against a book to Books; the rules read to
/// Rules; compares against anything, to Papers); a step with checks is
/// finished when all of them are done, one without once it was left. The
/// decision is never finished. The steps follow the day's ramp (Papers
/// Please lessons 4 and D7, see Day pacing): a step owning no checklist item
/// on the day (StagesOn: no set lists one yet, so the Books step waits for
/// the dress of day 7) is not shown, and the numbers, Next and Back run over
/// the steps shown; Papers and the decision always show. Pure; the app's
/// GuideBar owns one per case.
/// </summary>
public sealed class CaseGuide
{
    /// <summary>The steps in order.</summary>
    public static readonly IReadOnlyList<GuideStage> Stages = (GuideStage[])Enum.GetValues(typeof(GuideStage));

    private readonly HashSet<GuideStage> _left = new HashSet<GuideStage>();
    private readonly List<GuideStage> _shown = new List<GuideStage>(Stages);

    /// <summary>The step the clerk is on.</summary>
    public GuideStage Current { get; private set; }

    /// <summary>The steps shown this case, in order (every step until Reset is given fewer).</summary>
    public IReadOnlyList<GuideStage> Shown => _shown;

    /// <summary>The current step's position among the steps shown, 1-based ("Step 2 of 4").</summary>
    public int Number => _shown.IndexOf(Current) + 1;

    /// <summary>The number of steps shown ("Step 2 of 4").</summary>
    public int Count => _shown.Count;

    /// <summary>True on the first step (Back has nowhere to go).</summary>
    public bool IsFirst => Current == _shown[0];

    /// <summary>True on the last step, the decision (Next has nowhere to go).</summary>
    public bool IsLast => Current == _shown[_shown.Count - 1];

    /// <summary>A new case: the first step, none left behind; the steps shown are <paramref name="shown"/> (null: every step; Papers and the decision always), in order.</summary>
    public void Reset(IEnumerable<GuideStage> shown = null)
    {
        var keep = new HashSet<GuideStage>(shown ?? Stages) { GuideStage.Papers, GuideStage.Decision };
        _shown.Clear();
        foreach (GuideStage stage in Stages)
            if (keep.Contains(stage))
                _shown.Add(stage);
        Current = _shown[0];
        _left.Clear();
    }

    /// <summary>True when <paramref name="stage"/> is shown this case.</summary>
    public bool IsShown(GuideStage stage) => _shown.Contains(stage);

    /// <summary>Goes to <paramref name="stage"/> (any step shown, at any time); the step left is remembered. False when it is the current one or not shown.</summary>
    public bool Go(GuideStage stage)
    {
        if (stage == Current || !IsShown(stage))
            return false;
        _left.Add(Current);
        Current = stage;
        return true;
    }

    /// <summary>The next step shown (false on the decision).</summary>
    public bool Next() => !IsLast && Go(_shown[Number]);

    /// <summary>The previous step shown (false on the first).</summary>
    public bool Back() => !IsFirst && Go(_shown[Number - 2]);

    /// <summary>
    /// The steps shown on <paramref name="day"/>: Papers and the decision, and
    /// each other step some set of <paramref name="sets"/> (not a data-only
    /// one) lists a checklist item of on that day (CaseSteps.Resolve,
    /// StageOf), in order; every step
    /// when there are no sets. The same for every traveller of the day, so
    /// the steps never name the kind.
    /// </summary>
    public static List<GuideStage> StagesOn(StepSetData sets, int day)
    {
        if (sets == null || sets.sets == null || sets.sets.Count == 0)
            return new List<GuideStage>(Stages);
        var owned = new HashSet<GuideStage> { GuideStage.Papers, GuideStage.Decision };
        foreach (StepSet set in sets.sets)
            if (set != null && !set.dataOnly)
                foreach (StepSpec step in CaseSteps.Resolve(sets, set.type, day))
                    owned.Add(StageOf(step));
        var shown = new List<GuideStage>();
        foreach (GuideStage stage in Stages)
            if (owned.Contains(stage))
                shown.Add(stage);
        return shown;
    }

    /// <summary>True once the clerk has left <paramref name="stage"/> (visited it and gone on).</summary>
    public bool WasLeft(GuideStage stage) => _left.Contains(stage);

    /// <summary>True when <paramref name="stage"/> is finished: its checks all done when it has some, else left behind; never the decision.</summary>
    public bool IsDone(GuideStage stage, StageChecks checks) =>
        stage != GuideStage.Decision && (checks.Total > 0 ? checks.Done >= checks.Total : WasLeft(stage));

    /// <summary>The step a checklist item belongs to (see the class summary).</summary>
    public static GuideStage StageOf(StepSpec step)
    {
        switch (step.when)
        {
            case StepWhen.RecordViewed:
                return GuideStage.Records;
            case StepWhen.RulesViewed:
                return GuideStage.Rules;
            case StepWhen.Compared:
                return step.truth == TruthKind.Record ? GuideStage.Records
                     : step.truth == TruthKind.Reference ? GuideStage.Books
                     : GuideStage.Papers;
            default:
                return GuideStage.Papers;
        }
    }

    /// <summary>
    /// The checks of <paramref name="stage"/> this case: of the listed items'
    /// states (<paramref name="states"/>, CaseSteps.Evaluate over
    /// <paramref name="steps"/>: an item with no parts this case is not
    /// listed), those that belong to it, and how many are done.
    /// </summary>
    public static StageChecks Checks(GuideStage stage, IReadOnlyList<StepSpec> steps, IReadOnlyList<StepState> states)
    {
        int done = 0, total = 0;
        if (steps == null || states == null)
            return new StageChecks(0, 0);
        foreach (StepState state in states)
        {
            StepSpec spec = Find(steps, state.Id);
            if (spec == null || StageOf(spec) != stage)
                continue;
            total++;
            if (state.Done)
                done++;
        }
        return new StageChecks(done, total);
    }

    /// <summary>The spec of a listed item by its id, or null.</summary>
    private static StepSpec Find(IReadOnlyList<StepSpec> steps, string id)
    {
        foreach (StepSpec spec in steps)
            if (spec != null && spec.id == id)
                return spec;
        return null;
    }
}
