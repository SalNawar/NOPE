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
/// decision is never finished. Pure; the app's GuideBar owns one per case.
/// </summary>
public sealed class CaseGuide
{
    /// <summary>The steps in order.</summary>
    public static readonly IReadOnlyList<GuideStage> Stages = (GuideStage[])Enum.GetValues(typeof(GuideStage));

    private readonly HashSet<GuideStage> _left = new HashSet<GuideStage>();

    /// <summary>The step the clerk is on.</summary>
    public GuideStage Current { get; private set; }

    /// <summary>The current step's position, 1-based ("Step 2 of 5").</summary>
    public int Number => (int)Current + 1;

    /// <summary>True on the first step (Back has nowhere to go).</summary>
    public bool IsFirst => Current == Stages[0];

    /// <summary>True on the last step, the decision (Next has nowhere to go).</summary>
    public bool IsLast => Current == Stages[Stages.Count - 1];

    /// <summary>A new case: the first step, none left behind.</summary>
    public void Reset()
    {
        Current = Stages[0];
        _left.Clear();
    }

    /// <summary>Goes to <paramref name="stage"/> (any step, at any time); the step left is remembered. False when it is the current one.</summary>
    public bool Go(GuideStage stage)
    {
        if (stage == Current)
            return false;
        _left.Add(Current);
        Current = stage;
        return true;
    }

    /// <summary>The next step (false on the decision).</summary>
    public bool Next() => !IsLast && Go(Current + 1);

    /// <summary>The previous step (false on the first).</summary>
    public bool Back() => !IsFirst && Go(Current - 1);

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
