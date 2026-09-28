using System.Collections.Generic;
using System.Linq;

/// <summary>
/// What a travel rule forbids: a closure (the first three) or a standing
/// procedure (traveller types P3). Serialized in the rule assets
/// (TravelRuleSO.type): append only (SerializedEnumsTests pins every value).
/// </summary>
public enum TravelRuleType
{
    /// <summary>No travel to a specific era today.</summary>
    EraForbidden,

    /// <summary>No travel to a specific nation today.</summary>
    NationForbidden,

    /// <summary>No travel to a specific nation+era combination today.</summary>
    NationEraForbidden,

    /// <summary>
    /// A standing procedure (traveller types P3, P5): a traveller must be
    /// dressed for their destination, or they would cause a panic there. It
    /// closes no destination; a 2150 citizen's costume error breaks it, a
    /// deviation fault proven against the Costume Guide (CostumeErrors).
    /// </summary>
    DressForDestination,

    /// <summary>
    /// A procedure line with no predicate (traveller types §5.3): what the
    /// desk checks for a kind ("Leisure departures: a Leisure Visa and a
    /// Departure Manifest. Every paper must match the Citizen Account."). It
    /// closes no destination and plans no violator; the liars break it (L1,
    /// L2: record lies proven against the account, RecordLies).
    /// </summary>
    Procedure,

    /// <summary>
    /// The paper set of a kind (traveller types §5.3, phase 9): a Premium visa
    /// travels on a Premium transponder; a Standard visa needs an Economy
    /// transponder, a signed Stranding Waiver and a proof of means; a Debt
    /// Relief departure needs a Labour Contract, an Economy transponder and a
    /// signed waiver. Broken by a missing or unsigned form or the wrong
    /// transponder class, read from the papers (DirectiveFault.IncompletePapers).
    /// </summary>
    PaperSet,

    /// <summary>
    /// The debt standing (traveller types §5.3, phase 9): a Frozen account may
    /// not depart, read from the Citizen Account's Standing row
    /// (DirectiveFault.FrozenAccount).
    /// </summary>
    DebtStanding
}

/// <summary>
/// One way a traveller's paper set can be wrong (traveller types §5.4, the
/// PaperSet maker's variants): what the maker leaves out, leaves unsigned or
/// swaps. Runtime only (not serialized).
/// </summary>
public enum PaperSetBreak
{
    /// <summary>The set is complete.</summary>
    None,

    /// <summary>A Premium visa on an Economy transponder: the account really holds an Economy unit, the manifest prints it honestly (a rich tourist).</summary>
    EconomyManifest,

    /// <summary>The Stranding Waiver left out (a poor tourist or a labourer).</summary>
    WaiverMissing,

    /// <summary>The Stranding Waiver handed over unsigned (its signature reads "UNSIGNED").</summary>
    WaiverUnsigned,

    /// <summary>The proof of means left out (a poor tourist).</summary>
    ProofMissing
}

/// <summary>One of today's Directives as the rules see it: its type and the kinds it applies to (empty: every kind).</summary>
public readonly struct Directive
{
    /// <summary>What the rule forbids.</summary>
    public readonly TravelRuleType Type;

    /// <summary>The kinds the rule applies to; empty for every kind.</summary>
    public readonly IReadOnlyList<TravellerKind> Kinds;

    /// <summary>Creates a directive.</summary>
    public Directive(TravelRuleType type, IReadOnlyList<TravellerKind> kinds)
    {
        Type = type;
        Kinds = kinds ?? new TravellerKind[0];
    }

    /// <summary>True when the rule applies to a traveller of <paramref name="kind"/>: listed, or no kind is listed.</summary>
    public bool AppliesTo(TravellerKind kind) => Kinds.Count == 0 || Kinds.Contains(kind);
}

/// <summary>
/// What the Directives read of one traveller (traveller types P3): the
/// kind, whether the destination is closed, the classes the visa and the
/// manifest print, the forms carried and whether the waiver is signed, and
/// the account's standing. Built by CaseFactory from the finished papers and
/// account; every field is a fact, so the predicates are decision tables.
/// </summary>
public sealed class CaseFacts
{
    /// <summary>The traveller's kind.</summary>
    public TravellerKind Kind;

    /// <summary>True when today's closures forbid the claimed destination.</summary>
    public bool ClosedDestination;

    /// <summary>The class the Leisure Visa (TC-101) prints; null without a visa or an unreadable class.</summary>
    public CitizenStatus? VisaClass;

    /// <summary>The class the Departure Manifest (TC-230) prints; null without a manifest or an unreadable class.</summary>
    public TransponderClass? ManifestClass;

    /// <summary>The form numbers of the papers the traveller carries ("TC-520").</summary>
    public IReadOnlyCollection<string> Forms = new string[0];

    /// <summary>True when the Stranding Waiver is carried and signed (false when it is missing or reads "UNSIGNED").</summary>
    public bool WaiverSigned;

    /// <summary>True when the Citizen Account's standing is Frozen.</summary>
    public bool Frozen;

    /// <summary>True when the traveller carries the form numbered <paramref name="formNumber"/>.</summary>
    public bool Carries(string formNumber) => Forms != null && Forms.Contains(formNumber);
}

/// <summary>
/// The Directives' rules (traveller types P3, P4, §5.3-5.4): which rule types
/// are closures and which procedures plan a guaranteed faulty traveller;
/// each type's predicate over CaseFacts (Breaks); the fault a broken rule
/// is; the paper-set maker's variants and their pick; the violation roll;
/// and the content checks Generate World and the validator share (R6-006).
/// Pure, so every decision table is tested headless.
/// </summary>
public static class Directives
{
    /// <summary>The Leisure Departure Visa's form number: its class is what the tourists' paper set is read from.</summary>
    public const string Visa = "TC-101";

    /// <summary>The Departure Manifest's form number: its transponder class is what every paper set is read from.</summary>
    public const string Manifest = "TC-230";

    /// <summary>The Stranding Waiver's form number: a Standard visa and a Debt Relief departure need it, signed.</summary>
    public const string Waiver = "TC-310";

    /// <summary>The Debt Relief Labour Contract's form number: a Debt Relief departure needs it.</summary>
    public const string Contract = "TC-520";

    /// <summary>The three proofs of means (Holiday Credit Agreement, Proof of Funds, Travel Insurance Certificate): a Standard visa needs one.</summary>
    public static readonly IReadOnlyList<string> Proofs = new[] { "TC-415", "TC-416", "TC-417" };

    /// <summary>What an unsigned waiver's signature box reads (the paper-set maker writes it; the facts read it).</summary>
    public const string Unsigned = "UNSIGNED";

    /// <summary>True for a closure (a forbidden era, nation or place): it forbids destinations, and each active one gets a planned violator.</summary>
    public static bool IsClosure(TravelRuleType type) =>
        type == TravelRuleType.EraForbidden || type == TravelRuleType.NationForbidden || type == TravelRuleType.NationEraForbidden;

    /// <summary>
    /// True for a procedure that can plan a guaranteed faulty traveller on
    /// its first day (traveller types P4): the paper set (its maker's
    /// variants), the debt standing (a frozen account) and dress for the
    /// destination (a costume error). A closure is always guaranteed; a
    /// procedure line has no predicate and nothing to plan.
    /// </summary>
    public static bool CanGuarantee(TravelRuleType type) =>
        type == TravelRuleType.PaperSet || type == TravelRuleType.DebtStanding || type == TravelRuleType.DressForDestination;

    /// <summary>True for a procedure the violation roll may break on a later day (traveller types P4): the paper set and the debt standing (dress has its own roll, the costume roll).</summary>
    public static bool IsRolled(TravelRuleType type) =>
        type == TravelRuleType.PaperSet || type == TravelRuleType.DebtStanding;

    /// <summary>The directive fault a broken rule of <paramref name="type"/> is: a closed destination, incomplete papers, a frozen account; None for the types with no predicate.</summary>
    public static DirectiveFault FaultOf(TravelRuleType type)
    {
        if (IsClosure(type))
            return DirectiveFault.ClosedDestination;
        switch (type)
        {
            case TravelRuleType.PaperSet:
                return DirectiveFault.IncompletePapers;
            case TravelRuleType.DebtStanding:
                return DirectiveFault.FrozenAccount;
            default:
                return DirectiveFault.None;
        }
    }

    /// <summary>
    /// The decision table (traveller types §5.1, §5.3): a closure breaks on a
    /// closed destination; the paper set breaks for a tourist when a Premium
    /// visa rides an Economy manifest, or a Standard visa rides a Premium
    /// manifest or lacks a signed waiver or a proof of means, and for a
    /// labourer without a contract, a signed waiver or on a Premium manifest
    /// (a form not carried states nothing about its class); the debt
    /// standing breaks on a Frozen account; the dress rule and a procedure
    /// line never break here (a costume error is a deviation fault; a line
    /// has no predicate). The displaced break no paper set.
    /// </summary>
    public static bool Breaks(TravelRuleType type, CaseFacts facts)
    {
        if (facts == null)
            return false;
        if (IsClosure(type))
            return facts.ClosedDestination;

        switch (type)
        {
            case TravelRuleType.PaperSet:
                return PaperSetBroken(facts);
            case TravelRuleType.DebtStanding:
                return TravellerKinds.IsCitizen(facts.Kind) && facts.Frozen;
            default:
                return false;
        }
    }

    /// <summary>The paper-set row of <see cref="Breaks"/>.</summary>
    private static bool PaperSetBroken(CaseFacts facts)
    {
        switch (facts.Kind)
        {
            case TravellerKind.RichTourist:
            case TravellerKind.PoorTourist:
                if (facts.VisaClass == null)
                    return false;
                if (facts.VisaClass == CitizenStatus.Premium)
                    return facts.ManifestClass == TransponderClass.Economy;
                return facts.ManifestClass == TransponderClass.Premium || !facts.WaiverSigned || !Proofs.Any(facts.Carries);
            case TravellerKind.Labourer:
                return !facts.Carries(Contract) || facts.ManifestClass == TransponderClass.Premium || !facts.WaiverSigned;
            default:
                return false;
        }
    }

    /// <summary>
    /// The traveller's directive fault (traveller types P1): the fault of the
    /// first of <paramref name="rules"/> (today's Directives, in the day's
    /// order) that applies to the traveller's kind and breaks; None when none
    /// does.
    /// </summary>
    public static DirectiveFault Fault(IReadOnlyList<Directive> rules, CaseFacts facts)
    {
        if (rules == null || facts == null)
            return DirectiveFault.None;

        foreach (Directive rule in rules)
            if (rule.AppliesTo(facts.Kind) && Breaks(rule.Type, facts))
                return FaultOf(rule.Type);

        return DirectiveFault.None;
    }

    /// <summary>
    /// The paper-set breaks that can show for a traveller of <paramref name="kind"/>
    /// carrying <paramref name="forms"/> (their blueprint's form numbers), in
    /// the maker's fixed order (§5.4): a rich tourist's Economy manifest (with a
    /// visa and a manifest); a poor tourist's missing or unsigned waiver (with
    /// a waiver) and missing proof (with a proof); a labourer's missing or
    /// unsigned waiver (with a waiver). Empty for the displaced or a kind whose
    /// forms hold nothing to break.
    /// </summary>
    public static List<PaperSetBreak> PaperSetBreaks(TravellerKind kind, IReadOnlyCollection<string> forms)
    {
        var breaks = new List<PaperSetBreak>();
        forms = forms ?? new string[0];
        bool waiver = forms.Contains(Waiver);
        switch (kind)
        {
            case TravellerKind.RichTourist:
                if (forms.Contains(Visa) && forms.Contains(Manifest))
                    breaks.Add(PaperSetBreak.EconomyManifest);
                break;
            case TravellerKind.PoorTourist:
                if (waiver)
                {
                    breaks.Add(PaperSetBreak.WaiverMissing);
                    breaks.Add(PaperSetBreak.WaiverUnsigned);
                }
                if (Proofs.Any(forms.Contains))
                    breaks.Add(PaperSetBreak.ProofMissing);
                break;
            case TravellerKind.Labourer:
                if (waiver)
                {
                    breaks.Add(PaperSetBreak.WaiverMissing);
                    breaks.Add(PaperSetBreak.WaiverUnsigned);
                }
                break;
        }
        return breaks;
    }

    /// <summary>
    /// The paper-set maker's variant for a traveller of <paramref name="kind"/>
    /// carrying <paramref name="forms"/>: one Range draw over
    /// <see cref="PaperSetBreaks"/> when two or more can show, none when one
    /// can, None with no draw when none can or without a stream.
    /// </summary>
    public static PaperSetBreak PickPaperSetBreak(TravellerKind kind, IReadOnlyCollection<string> forms, IRandomSource rng)
    {
        List<PaperSetBreak> breaks = PaperSetBreaks(kind, forms);
        if (breaks.Count == 0 || rng == null)
            return PaperSetBreak.None;
        return breaks.Count == 1 ? breaks[0] : breaks[rng.Range(0, breaks.Count)];
    }

    /// <summary>
    /// True when a traveller of <paramref name="kind"/> carrying
    /// <paramref name="forms"/> can break a rule of <paramref name="type"/>
    /// through its maker (§5.4): the paper set when a variant can show, the
    /// debt standing for a 2150 citizen (a frozen account), dress for a 2150
    /// citizen (a costume error). Never a closure or a procedure line here.
    /// </summary>
    public static bool CanBreak(TravelRuleType type, TravellerKind kind, IReadOnlyCollection<string> forms)
    {
        switch (type)
        {
            case TravelRuleType.PaperSet:
                return PaperSetBreaks(kind, forms).Count > 0;
            case TravelRuleType.DebtStanding:
            case TravelRuleType.DressForDestination:
                return TravellerKinds.IsCitizen(kind);
            default:
                return false;
        }
    }

    /// <summary>
    /// The violation roll (traveller types P4, K5; on the traveller's fault
    /// stream, before the costume roll): with probability <paramref name="chance"/>
    /// (one Value draw) the traveller breaks one of <paramref name="breakable"/>
    /// (today's rolled procedures they can break, in the day's order),
    /// picked uniformly with one Range draw when two or more are listed. The
    /// index of the rule broken, or -1: an honest roll, no breakable rule or
    /// no stream (no draw). The caller skips the call (no draw) for a
    /// traveller with another fault.
    /// </summary>
    public static int Roll(float chance, int breakable, IRandomSource rng)
    {
        if (rng == null || breakable <= 0)
            return -1;
        if (!(rng.Value() < chance))
            return -1;
        return breakable == 1 ? 0 : rng.Range(0, breakable);
    }

    /// <summary>A rule as the content checks see it: its asset name, type and kinds, and whether its closure or line is authored.</summary>
    public readonly struct RuleEntry
    {
        /// <summary>The rule asset's name.</summary>
        public readonly string Asset;

        /// <summary>What it forbids.</summary>
        public readonly TravelRuleType Type;

        /// <summary>The kinds it applies to (empty: every kind).</summary>
        public readonly IReadOnlyList<TravellerKind> Kinds;

        /// <summary>Creates an entry.</summary>
        public RuleEntry(string asset, TravelRuleType type, IReadOnlyList<TravellerKind> kinds)
        {
            Asset = asset;
            Type = type;
            Kinds = kinds ?? new TravellerKind[0];
        }
    }

    /// <summary>
    /// What Generate World and the validator refuse of one rule (R6-006): a
    /// procedure names no country or era and has its line; a closure names
    /// no kinds (it closes a destination for everyone) and no line is
    /// needed; a paper set or debt standing lists at least one kind, since
    /// each has a kind's paper set to read. Empty when sound.
    /// </summary>
    public static List<string> RuleProblems(string asset, TravelRuleType type, IReadOnlyList<TravellerKind> kinds, bool hasPlace, bool hasLine)
    {
        var problems = new List<string>();
        kinds = kinds ?? new TravellerKind[0];
        if (IsClosure(type))
        {
            if (kinds.Count > 0)
                problems.Add($"Rule '{asset}' is a closure ({type}): it closes a destination for every kind and lists none.");
            return problems;
        }

        if (hasPlace)
            problems.Add($"Rule '{asset}' is a standing procedure ({type}), but names a country or era; a procedure names none.");
        if (!hasLine)
            problems.Add($"Rule '{asset}' is a standing procedure ({type}): it needs its directive line (\"description\").");
        if ((type == TravelRuleType.PaperSet || type == TravelRuleType.DebtStanding) && kinds.Count == 0)
            problems.Add($"Rule '{asset}' ({type}) lists no kinds; a paper set or debt standing names the kinds it is read for.");
        return problems;
    }

    /// <summary>
    /// What Generate World and the validator refuse of one day's Directives
    /// (R6-006): a paper set or debt standing active today that no kind of
    /// the day (<paramref name="kinds"/>, those with a positive weight, each
    /// with its blueprint's form numbers) can break through its maker
    /// (<see cref="CanBreak"/>), so nobody could ever test it; a guaranteed
    /// rule (<paramref name="guarantee"/>) that is not active today, is not a
    /// rule of <see cref="CanGuarantee"/>'s types (a closure is always
    /// guaranteed; a line plans nothing), is listed twice, or that no kind of
    /// the day can break. Empty when sound.
    /// </summary>
    public static List<string> DayProblems(string day, IReadOnlyList<RuleEntry> active, IReadOnlyList<(TravellerKind kind, IReadOnlyCollection<string> forms)> kinds,
                                           IReadOnlyList<string> guarantee)
    {
        var problems = new List<string>();
        active = active ?? new RuleEntry[0];
        kinds = kinds ?? new (TravellerKind, IReadOnlyCollection<string>)[0];

        bool Breakable(RuleEntry rule) =>
            kinds.Any(k => new Directive(rule.Type, rule.Kinds).AppliesTo(k.kind) && CanBreak(rule.Type, k.kind, k.forms));

        foreach (RuleEntry rule in active)
            if (IsRolled(rule.Type) && !Breakable(rule))
                problems.Add($"Day '{day}' lists the rule '{rule.Asset}' ({rule.Type}), which none of its kinds can break: no traveller could ever test it.");

        var seen = new HashSet<string>();
        foreach (string name in guarantee ?? new string[0])
        {
            if (!seen.Add(name))
            {
                problems.Add($"Day '{day}' guarantees the rule '{name}' twice.");
                continue;
            }
            RuleEntry rule = active.FirstOrDefault(r => r.Asset == name);
            if (rule.Asset == null)
            {
                problems.Add($"Day '{day}' guarantees the rule '{name}', which is not among its rules.");
                continue;
            }
            if (!CanGuarantee(rule.Type))
                problems.Add($"Day '{day}' guarantees the rule '{name}' ({rule.Type}); a closure is always guaranteed and a procedure line plans nothing: only a paper set, a debt standing or dress for the destination can be guaranteed.");
            else if (!Breakable(rule))
                problems.Add($"Day '{day}' guarantees the rule '{name}' ({rule.Type}), which none of its kinds can break.");
        }

        return problems;
    }
}
