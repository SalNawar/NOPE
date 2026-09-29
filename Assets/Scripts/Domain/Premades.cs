using System.Collections.Generic;
using System.Linq;

/// <summary>Where a slot's traveller comes from, as far as premades go.</summary>
public enum PremadeSlot
{
    /// <summary>The slot's forced premade stands here.</summary>
    Forced,

    /// <summary>The slot may roll a premade from the day's pool.</summary>
    Roll,

    /// <summary>An ordinary traveller stands here (no roll).</summary>
    None
}

/// <summary>A forced entry as the content checks see it (Premades.ForcedProblems; Generate World builds it from the source, the validator from the day plans).</summary>
public sealed class ForcedCheck
{
    /// <summary>Its 1-based slot.</summary>
    public int Slot;

    /// <summary>Its id (blank: none).</summary>
    public string Id;

    /// <summary>Its premade's id (blank: a forced blueprint's appearance).</summary>
    public string Premade;

    /// <summary>The kind the entry stands as (its premade's, or its blueprint's).</summary>
    public TravellerKind Kind;

    /// <summary>The form numbers the kind's blueprint carries.</summary>
    public IReadOnlyCollection<string> Forms = new string[0];

    /// <summary>True for a premade with a true place (an authored liar from it).</summary>
    public bool HasTruePlace;

    /// <summary>True for a once-per-run premade.</summary>
    public bool OncePerRun;

    /// <summary>True when the entry's destination is closed that day.</summary>
    public bool ClosedPlace;

    /// <summary>The authored lie, or null.</summary>
    public LieKind? Lie;

    /// <summary>The authored directive fault (None: none).</summary>
    public PlannedDirective Directive;

    /// <summary>The entry's dialog (blank: none of its own).</summary>
    public string Dialog;

    /// <summary>The keys its conditions read (flags, counters, upgrades; blank ones skipped).</summary>
    public IReadOnlyList<string> ConditionKeys = new string[0];

    /// <summary>How many conditions it has.</summary>
    public int Conditions;
}

/// <summary>A day as the forced entries' checks see it.</summary>
public sealed class ForcedDayCheck
{
    /// <summary>The plan's asset name.</summary>
    public string Asset;

    /// <summary>The day number.</summary>
    public int Day;

    /// <summary>The lies the day enables.</summary>
    public IReadOnlyList<LieKind> Lies = new LieKind[0];

    /// <summary>The day's rules as the Domain predicates see them.</summary>
    public IReadOnlyList<Directive> Rules = new Directive[0];

    /// <summary>The premades the day pools.</summary>
    public IReadOnlyCollection<string> Pooled = new string[0];

    /// <summary>The day's forced entries, in the authored order.</summary>
    public IReadOnlyList<ForcedCheck> Forced = new ForcedCheck[0];
}

/// <summary>
/// Premade scheduling rules: which slots hold a premade, who may still roll,
/// and the roll itself (moved from CaseFactory onto the premade stream,
/// Seeds.ForLegendary). Pure, so the decision tables are tested headless.
/// </summary>
public static class Premades
{
    /// <summary>The longest Citizen Records note a premade may carry (the note box holds about three lines of 60 characters).</summary>
    public const int MaxNoteLength = 120;

    /// <summary>
    /// A slot's source: where a premade is forced (<paramref name="forcedHere"/>),
    /// the slot's standing appearance's premade stands there
    /// (<paramref name="premadeStands"/>, <see cref="Appearance"/>), else an
    /// ordinary traveller does (a met premade, or conditions that failed at
    /// the day's start), never a roll; a slot planned for a rule violator
    /// never rolls; every other slot rolls. A slot nothing is forced into
    /// reads no condition.
    /// </summary>
    public static PremadeSlot SlotSource(bool forcedHere, bool premadeStands, bool violatorSlot)
    {
        if (forcedHere)
            return premadeStands ? PremadeSlot.Forced : PremadeSlot.None;
        return violatorSlot ? PremadeSlot.None : PremadeSlot.Roll;
    }

    /// <summary>
    /// Whether one forced entry stands today (days 7-15 B9): its conditions
    /// pass on the day-start snapshot, and its premade is not a once-per-run
    /// premade already met (a repeatable premade is never kept out by a met
    /// flag; an entry with no premade passes <paramref name="oncePerRun"/> false).
    /// </summary>
    public static bool Stands(bool oncePerRun, bool met, bool conditionsPass) => conditionsPass && !(oncePerRun && met);

    /// <summary>
    /// The appearance's voice (days 7-15 B7): the forced slot's own line (its
    /// dialog id, its opener) when authored, else the premade's; a recurring
    /// character gets a new scene each time.
    /// </summary>
    public static string Voice(string slotLine, string premadeLine) => string.IsNullOrWhiteSpace(slotLine) ? premadeLine : slotLine;

    /// <summary>
    /// A slot's appearance among its forced entries (days 7-15 B9, the
    /// alternatives of a beat): the index of the first entry that stands
    /// (<see cref="Stands"/>, in the authored order), so an author writes
    /// if / else-if top to bottom; -1 when none stands (an ordinary traveller)
    /// or there are none.
    /// </summary>
    public static int Appearance(IReadOnlyList<bool> standing)
    {
        if (standing == null)
            return -1;
        for (int i = 0; i < standing.Count; i++)
            if (standing[i])
                return i;
        return -1;
    }

    /// <summary>
    /// Whether a pooled premade may roll: not met this run and their name free
    /// today. Forced premades' names are reserved before slot 1, which is what
    /// keeps them out of the day's roll.
    /// </summary>
    public static bool IsRollable(bool met, bool nameTaken) => !met && !nameTaken;

    /// <summary>
    /// What Generate World refuses (<paramref name="errors"/>) and warns about
    /// (<paramref name="warnings"/>) in the days' forced entries, and the
    /// validator reports in the same words (days 7-15 V2-V5):
    /// V2, an authored fault: at most one of a lie and a directive fault; none
    /// on a premade with a true place or bound for a place the day closes; a
    /// lie that fits the entry's kind (LieKinds.AppliesTo; not enabled that
    /// day: a warning, the player has not been told of it); a directive fault
    /// whose rule stands that day for the kind (Directives.Plan) and whose
    /// variant the kind's papers can show (a paper-set variant of
    /// Directives.PaperSetBreaks; a Frozen account on a 2150 citizen).
    /// V3: a dialog of <paramref name="dialogIds"/>; an id of lower-case
    /// letters, digits and '_', unique in the day; a slot with alternatives
    /// names each entry, and every entry but its last has a condition.
    /// Also: one premade in two slots of a day.
    /// V4: a premade flag a condition reads names a premade of
    /// <paramref name="premadeIds"/>, which (a warning) stands on an earlier
    /// day, forced or pooled, else the condition is fixed.
    /// V5 (warnings): a once-per-run premade forced on two days; a repeatable
    /// premade forced again with no condition. Days in any order.
    /// </summary>
    public static void ForcedProblems(IReadOnlyList<ForcedDayCheck> days, ICollection<string> premadeIds, ICollection<string> dialogIds, List<string> errors, List<string> warnings)
    {
        if (days == null)
            return;

        List<ForcedDayCheck> ordered = days.Where(d => d != null).OrderBy(d => d.Day).ToList();
        var standsBefore = new HashSet<string>();
        var forcedOn = new Dictionary<string, int>();
        foreach (ForcedDayCheck day in ordered)
        {
            string owner = $"Day '{day.Asset}'";
            List<ForcedCheck> forced = (day.Forced ?? new ForcedCheck[0]).Where(f => f != null).ToList();

            // V3: ids, alternatives, one premade per slot.
            var ids = new HashSet<string>();
            foreach (ForcedCheck f in forced.Where(f => !string.IsNullOrEmpty(f.Id)))
            {
                if (!f.Id.All(ch => (ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '_'))
                    errors.Add($"{owner} names a forced entry '{f.Id}'; an entry's id is lower-case letters, digits and '_'.");
                if (!ids.Add(f.Id))
                    errors.Add($"{owner} names two forced entries '{f.Id}'; an entry's id is unique in the day.");
            }
            foreach (IGrouping<int, ForcedCheck> slot in forced.GroupBy(f => f.Slot).Where(g => g.Count() > 1))
            {
                List<ForcedCheck> alternatives = slot.ToList();
                if (alternatives.Any(f => string.IsNullOrEmpty(f.Id)))
                    errors.Add($"{owner} forces slot {slot.Key} {alternatives.Count} times; a slot's alternatives each need an id (days[].forced[].id).");
                foreach (ForcedCheck f in alternatives.Take(alternatives.Count - 1).Where(f => f.Conditions <= 0))
                    errors.Add($"{owner} slot {slot.Key}: the entry '{f.Id}' has no condition, so it always stands and the entries after it never do.");
            }
            foreach (IGrouping<string, ForcedCheck> premade in forced.Where(f => !string.IsNullOrEmpty(f.Premade)).GroupBy(f => f.Premade))
                if (premade.Select(f => f.Slot).Distinct().Count() > 1)
                    errors.Add($"{owner} forces premade '{premade.Key}' into two slots [{string.Join(", ", premade.Select(f => f.Slot).Distinct())}]; a premade stands in one slot a day (a slot may list them as alternatives).");

            foreach (ForcedCheck f in forced)
            {
                string entry = $"{owner} slot {f.Slot}{(string.IsNullOrEmpty(f.Id) ? string.Empty : $" ('{f.Id}')")}";

                // V2: the authored fault.
                bool directive = f.Directive != PlannedDirective.None;
                if (f.Lie != null && directive)
                    errors.Add($"{entry} authors both a lie ({f.Lie}) and a directive fault ({f.Directive}); an appearance carries one fault.");
                if ((f.Lie != null || directive) && f.HasTruePlace)
                    errors.Add($"{entry} authors a fault on premade '{f.Premade}', who has a true place and lies from it; a premade with a true place takes no other fault.");
                if ((f.Lie != null || directive) && f.ClosedPlace)
                    errors.Add($"{entry} authors a fault on a traveller bound for a place closed that day; the closure is their fault already.");
                if (f.Lie != null)
                {
                    if (!LieKinds.AppliesTo(f.Lie.Value, f.Kind))
                        errors.Add($"{entry} authors the lie {f.Lie}, which a {f.Kind} cannot tell (LieKinds.AppliesTo).");
                    else if (day.Lies == null || !day.Lies.Contains(f.Lie.Value))
                        warnings.Add($"{entry} authors the lie {f.Lie}, which the day does not enable: the player has not been told of it.");
                }
                if (directive)
                {
                    DirectivePlan plan = Directives.Plan(f.Directive);
                    if (!(day.Rules ?? new Directive[0]).Any(r => r.Type == plan.Rule && r.AppliesTo(f.Kind)))
                        errors.Add($"{entry} authors {f.Directive}, but no {plan.Rule} rule stands that day for a {f.Kind}.");
                    if (plan.PaperBreak != PaperSetBreak.None && !Directives.PaperSetBreaks(f.Kind, f.Forms).Contains(plan.PaperBreak))
                        errors.Add($"{entry} authors {f.Directive}, which a {f.Kind}'s papers cannot show.");
                    if (plan.Rule == TravelRuleType.DebtStanding && !TravellerKinds.IsCitizen(f.Kind))
                        errors.Add($"{entry} authors {f.Directive}, but a {f.Kind} holds no Citizen Account to freeze.");
                }

                if (!string.IsNullOrEmpty(f.Dialog) && (dialogIds == null || !dialogIds.Contains(f.Dialog)))
                    errors.Add($"{entry} names unknown dialog '{f.Dialog}'.");

                // V4: the conditions' premade flags.
                foreach (string key in f.ConditionKeys ?? new string[0])
                {
                    if (!FlagKeys.TryParsePremade(key, out string premade, out _))
                        continue;
                    if (premadeIds == null || !premadeIds.Contains(premade))
                        errors.Add($"{entry} reads the flag '{key}', whose premade '{premade}' does not exist, so it never passes.");
                    else if (!standsBefore.Contains(premade))
                        warnings.Add($"{entry} reads the flag '{key}', but '{premade}' stands on no earlier day (forced or pooled), so the condition never changes.");
                }
            }

            // V5, then this day's premades join those that stood before the next day.
            foreach (IGrouping<string, ForcedCheck> premade in forced.Where(f => !string.IsNullOrEmpty(f.Premade)).GroupBy(f => f.Premade))
            {
                if (forcedOn.TryGetValue(premade.Key, out int earlier))
                {
                    if (premade.Any(f => f.OncePerRun))
                        warnings.Add($"{owner} forces the once-per-run premade '{premade.Key}', already forced on day {earlier}: this slot stands only if that one was never reached.");
                    else if (premade.Any(f => f.Conditions <= 0))
                        warnings.Add($"{owner} forces the repeatable premade '{premade.Key}' again (first forced on day {earlier}) with no condition: they return whatever the earlier verdict.");
                }
                else
                {
                    forcedOn[premade.Key] = day.Day;
                }
                standsBefore.Add(premade.Key);
            }
            foreach (string pooled in day.Pooled ?? new string[0])
                standsBefore.Add(pooled);
        }
    }

    /// <summary>
    /// The roll: -1 with no draw when there is no candidate; otherwise, unless
    /// <paramref name="forcedByCheat"/>, one Value draw and -1 when it is not
    /// below <paramref name="chance"/>; then one Range draw picks the candidate.
    /// </summary>
    public static int Roll(float chance, int candidates, bool forcedByCheat, IRandomSource rng)
    {
        if (candidates <= 0 || rng == null)
            return -1;

        if (!forcedByCheat && !(rng.Value() < chance))
            return -1;

        return rng.Range(0, candidates);
    }
}
