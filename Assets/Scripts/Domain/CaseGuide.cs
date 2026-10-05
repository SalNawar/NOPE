using System;
using System.Collections.Generic;

/// <summary>
/// The Investigation app's guided steps (the PC workbench spec IA2, IA3), in
/// order. Runtime only (not serialized): Ctrl+1…4 follow this order. The PC
/// only investigates (the PC clean-up of 2026-10-05): the verdict is the
/// stamp on the passport at the desk, so no step decides.
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
    Rules
}

/// <summary>
/// Where the clerk is in a case's guided steps (the PC workbench spec IA2,
/// IA3; W1: steps suggest, they never lock; headless since the desk-first
/// redesign: a step only puts a pair of documents up): the current step. A
/// step owns the items of the day's steps checklist that belong to it
/// (StageOf: papers received, read and asked for, the answers heard, the
/// look and paper-against-paper compares belong to Papers; a record looked
/// up and compares against a record to Records; compares against a book to
/// Books; the rules read to Rules). The steps follow the day's ramp (Papers
/// Please lessons 4 and D7, see Day pacing): a step owning no checklist item
/// on the day (StagesOn: no set lists one yet, so the Books step waits for
/// the dress of day 7) is not shown, and the numbers, Next and Back run over
/// the steps shown; Papers always shows. Pure; the app's GuideBar owns one
/// per case.
/// </summary>
public sealed class CaseGuide
{
    /// <summary>The steps in order.</summary>
    public static readonly IReadOnlyList<GuideStage> Stages = (GuideStage[])Enum.GetValues(typeof(GuideStage));

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

    /// <summary>True on the last step shown (Next has nowhere to go).</summary>
    public bool IsLast => Current == _shown[_shown.Count - 1];

    /// <summary>A new case: the first step; the steps shown are <paramref name="shown"/> (null: every step; Papers always), in order.</summary>
    public void Reset(IEnumerable<GuideStage> shown = null)
    {
        var keep = new HashSet<GuideStage>(shown ?? Stages) { GuideStage.Papers };
        _shown.Clear();
        foreach (GuideStage stage in Stages)
            if (keep.Contains(stage))
                _shown.Add(stage);
        Current = _shown[0];
    }

    /// <summary>True when <paramref name="stage"/> is shown this case.</summary>
    public bool IsShown(GuideStage stage) => _shown.Contains(stage);

    /// <summary>Goes to <paramref name="stage"/> (any step shown, at any time). False when it is the current one or not shown.</summary>
    public bool Go(GuideStage stage)
    {
        if (stage == Current || !IsShown(stage))
            return false;
        Current = stage;
        return true;
    }

    /// <summary>The next step shown (false on the last).</summary>
    public bool Next() => !IsLast && Go(_shown[Number]);

    /// <summary>The previous step shown (false on the first).</summary>
    public bool Back() => !IsFirst && Go(_shown[Number - 2]);

    /// <summary>
    /// The steps shown on <paramref name="day"/>: Papers, and each other step some set of <paramref name="sets"/> (not a data-only
    /// one) lists a checklist item of on that day (CaseSteps.Resolve,
    /// StageOf), in order; every step
    /// when there are no sets. The same for every traveller of the day, so
    /// the steps never name the kind.
    /// </summary>
    public static List<GuideStage> StagesOn(StepSetData sets, int day)
    {
        if (sets == null || sets.sets == null || sets.sets.Count == 0)
            return new List<GuideStage>(Stages);
        var owned = new HashSet<GuideStage> { GuideStage.Papers };
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
}
