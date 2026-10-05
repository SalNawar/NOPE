using System.Collections.Generic;

/// <summary>
/// The day's interview, fixed at day start: which questions are askable (the
/// same for every traveller, the personalities spec's W1-W3), which of them
/// may carry a spoken tell, and which narrative dialogs are
/// offered (their conditions pass on the day-start snapshot, their structure
/// is sound, a one-shot dialog is not done yet, and a dialog bound to a
/// premade only while that premade is at the desk). Every availability rule
/// lives here, so the office decides nothing on its own.
/// </summary>
public sealed class InterviewDay
{
    private readonly List<InterviewQuestion> _questions = new List<InterviewQuestion>();
    private readonly List<ClueCategory> _askable = new List<ClueCategory>();
    private readonly List<ClueCategory> _answerTell = new List<ClueCategory>();
    private readonly List<AuthoredDialog> _dayStartDialogs = new List<AuthoredDialog>();
    private readonly List<string> _problems = new List<string>();
    private readonly HashSet<string> _premadeDialogIds = new HashSet<string>();
    private readonly ShiftLedger _ledger;
    private readonly IReadOnlyList<AskableForm> _forms;

    /// <summary>
    /// Decides today's interview once, from the day-start snapshot.
    /// <paramref name="premadeDialogIds"/> are the dialogs premades name
    /// (LegendarySO.dialogId): each is offered only while its premade is at the desk.
    /// <paramref name="forms"/> are the day's papers menu (every on-request
    /// form of the days so far, FormRequests.MetSoFar), offered to every traveller.
    /// <paramref name="clothes"/> is false before the Look menu's garments are
    /// introduced (the desk-first ramp: with the dress rule, Feature.Clothes).
    /// </summary>
    public InterviewDay(InterviewLines lines, IReadOnlyList<Gated<InterviewQuestion>> questions,
                        IReadOnlyList<Gated<AuthoredDialog>> dialogs, GateSnapshot snapshot, ShiftLedger ledger,
                        IEnumerable<string> premadeDialogIds, IReadOnlyList<AskableForm> forms = null, bool clothes = true)
    {
        Clothes = clothes;
        Lines = lines ?? new InterviewLines();
        _ledger = ledger ?? new ShiftLedger();
        _forms = forms ?? new AskableForm[0];
        if (premadeDialogIds != null)
            foreach (string id in premadeDialogIds)
                if (!string.IsNullOrWhiteSpace(id))
                    _premadeDialogIds.Add(id);

        if (questions != null)
        {
            foreach (Gated<InterviewQuestion> q in questions)
            {
                if (q.Item == null || !Gates.AllPass(q.Conditions, snapshot))
                    continue;

                _questions.Add(q.Item);
                _askable.Add(q.Item.category);
                if (Gates.DayOnly(q.Conditions))
                    _answerTell.Add(q.Item.category);
            }
        }

        if (dialogs != null)
        {
            foreach (Gated<AuthoredDialog> d in dialogs)
            {
                if (d.Item == null)
                    continue;

                List<string> problems = DialogChecks.Problems(d.Item, Lines.menuCapacity);
                foreach (string problem in problems)
                    _problems.Add($"Dialog '{d.Item.id}' is not offered: {problem}");

                bool done = d.Item.oneShot && snapshot != null && snapshot.HasFlag(FlagKeys.DialogDone(d.Item.id));
                if (problems.Count == 0 && !done && Gates.AllPass(d.Conditions, snapshot))
                    _dayStartDialogs.Add(d.Item);
            }
        }
    }

    /// <summary>The interview's fixed wording and layout limits.</summary>
    public InterviewLines Lines { get; }

    /// <summary>True once the Look menu lists the traveller's garments (the desk-first ramp, Feature.Clothes); their face shows from day 1.</summary>
    public bool Clothes { get; }

    /// <summary>The day's papers menu (the personalities spec's W4): the forms the desk may ask every traveller of the day for, in first-appearance order.</summary>
    public IReadOnlyList<AskableForm> AskableForms => _forms;

    /// <summary>Today's askable questions, in library order: every traveller of the day is asked each, in the same words (the personalities spec's W3).</summary>
    public IReadOnlyList<InterviewQuestion> Questions => _questions;

    /// <summary>The askable questions' categories, in the same order, each once (one question per category: InterviewQuestions.Problems): every traveller answers each.</summary>
    public IReadOnlyList<ClueCategory> AskableCategories => _askable;

    /// <summary>
    /// The categories of the askable questions gated by day alone, in the same
    /// order: only these may carry an Answer tell (a question gated by an
    /// upgrade, a flag, a counter or stability is hint-only).
    /// </summary>
    public IReadOnlyList<ClueCategory> AnswerTellCategories => _answerTell;

    /// <summary>Every structural problem of every dialog, whether or not its conditions pass ("Dialog 'x' is not offered: ...").</summary>
    public IReadOnlyList<string> ContentProblems => _problems;

    /// <summary>
    /// The dialogs offered to the traveller at the desk: those offered at day
    /// start, minus those completed this shift, minus every premade-bound
    /// dialog except <paramref name="premadeDialogId"/> (the dialog of the
    /// premade at the desk; null or blank for an ordinary traveller).
    /// </summary>
    public IReadOnlyList<AuthoredDialog> OfferedDialogs(string premadeDialogId)
    {
        var offered = new List<AuthoredDialog>();
        foreach (AuthoredDialog d in _dayStartDialogs)
            if (!CompletedThisShift(d.id) && (!_premadeDialogIds.Contains(d.id) || d.id == premadeDialogId))
                offered.Add(d);
        return offered;
    }

    /// <summary>
    /// Records a finished dialog in the shift ledger (its effect is applied at
    /// the end of the shift; DialogOutcomes). False, with nothing recorded,
    /// for a dialog not offered today or already completed this shift.
    /// </summary>
    public bool Complete(string dialogId, string effectName)
    {
        AuthoredDialog dialog = null;
        foreach (AuthoredDialog d in _dayStartDialogs)
        {
            if (d.id == dialogId)
            {
                dialog = d;
                break;
            }
        }

        if (dialog == null || CompletedThisShift(dialogId))
            return false;

        _ledger.dialogOutcomes.Add(new DialogOutcome { dialogId = dialogId, effectName = effectName, oneShot = dialog.oneShot });
        return true;
    }

    /// <summary>True when the ledger already holds this dialog.</summary>
    private bool CompletedThisShift(string dialogId)
    {
        foreach (DialogOutcome o in _ledger.dialogOutcomes)
            if (o != null && o.dialogId == dialogId)
                return true;
        return false;
    }
}

/// <summary>
/// The questions' content rules (the personalities spec's W3: one question
/// per category, asked of every traveller in the same words), the one rule
/// Generate World and the content validator share: one question per
/// category, so every traveller has one answer per category, and the ask
/// menu's worst case is every question. Pure.
/// </summary>
public static class InterviewQuestions
{
    /// <summary>
    /// The questions that ask about a category an earlier question already
    /// asks: "Question 'q_trip_currency' asks about Currency, as 'q_currency'
    /// does (one question per category: every traveller is asked each)."
    /// Null entries are skipped; empty when sound.
    /// </summary>
    public static List<string> Problems(IReadOnlyList<InterviewQuestion> questions)
    {
        var problems = new List<string>();
        var first = new Dictionary<ClueCategory, string>();
        foreach (InterviewQuestion q in questions ?? new InterviewQuestion[0])
        {
            if (q == null)
                continue;
            if (first.TryGetValue(q.category, out string earlier))
                problems.Add($"Question '{q.id}' asks about {q.category}, as '{earlier}' does (one question per category: every traveller is asked each).");
            else
                first.Add(q.category, q.id);
        }

        return problems;
    }

    /// <summary>How many questions there are (null entries skipped; 0 for null): every traveller is asked each, so this is the ask menu's worst case.</summary>
    public static int Count(IReadOnlyList<InterviewQuestion> questions)
    {
        int count = 0;
        foreach (InterviewQuestion q in questions ?? new InterviewQuestion[0])
            if (q != null)
                count++;
        return count;
    }
}

/// <summary>
/// What the end of the shift applies for the dialogs completed during it:
/// the run-level done flags and the effects, each once. Pure, so the one-shot
/// memory and the apply-once rule are tested headless.
/// </summary>
public static class DialogOutcomes
{
    /// <summary>FlagKeys.DialogDone for every one-shot outcome, once per dialog id, in ledger order (empty for null).</summary>
    public static IReadOnlyList<string> FlagsToSet(IReadOnlyList<DialogOutcome> outcomes)
    {
        var flags = new List<string>();
        if (outcomes == null)
            return flags;

        foreach (DialogOutcome o in outcomes)
        {
            if (o == null || !o.oneShot)
                continue;

            string flag = FlagKeys.DialogDone(o.dialogId);
            if (!flags.Contains(flag))
                flags.Add(flag);
        }

        return flags;
    }

    /// <summary>The outcomes that name an effect, the first per dialog id, in ledger order (empty for null).</summary>
    public static IReadOnlyList<DialogOutcome> EffectsToApply(IReadOnlyList<DialogOutcome> outcomes)
    {
        var effects = new List<DialogOutcome>();
        if (outcomes == null)
            return effects;

        var seen = new HashSet<string>();
        foreach (DialogOutcome o in outcomes)
        {
            if (o == null || string.IsNullOrWhiteSpace(o.effectName) || !seen.Add(o.dialogId ?? string.Empty))
                continue;

            effects.Add(o);
        }

        return effects;
    }
}
