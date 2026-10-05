using System;
using System.Collections.Generic;
using System.Linq;

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

/// <summary>
/// A forced slot's authored directive fault (days 7-15 B6): which procedure
/// the appearance breaks and how, so a beat's scene can name it (an
/// unsigned waiver, a frozen account). Serialized in DayPlanSO's forced slots
/// (world_source.json days[].forced[].directive): append only
/// (SerializedEnumsTests pins every value).
/// </summary>
public enum PlannedDirective
{
    /// <summary>No authored directive fault.</summary>
    None,

    /// <summary>The paper set: a Premium visa on an Economy transponder (a rich tourist).</summary>
    EconomyManifest,

    /// <summary>The paper set: the Stranding Waiver left out (a kind that carries one).</summary>
    WaiverMissing,

    /// <summary>The paper set: the Stranding Waiver handed over unsigned (a kind that carries one).</summary>
    WaiverUnsigned,

    /// <summary>The paper set: the proof of means left out (a poor tourist).</summary>
    ProofMissing,

    /// <summary>The debt standing: a Frozen account (a 2150 citizen).</summary>
    Frozen,

    /// <summary>The papers' dates: a departure dated another day (a kind whose papers print one).</summary>
    DepartureDate,

    /// <summary>The papers' dates: a Valid Until that has passed (a kind whose papers print one).</summary>
    Expired,

    /// <summary>A transponder recall: the traveller's unit is the recalled model (an Economy kind, on a day a recall stands).</summary>
    Recalled
}

/// <summary>What an authored directive fault makes (Directives.Plan): the rule it breaks and the maker's variant, pinned instead of drawn.</summary>
public readonly struct DirectivePlan
{
    /// <summary>True for a fault; false for PlannedDirective.None.</summary>
    public readonly bool IsFault;

    /// <summary>The procedure the traveller breaks (read only when <see cref="IsFault"/>).</summary>
    public readonly TravelRuleType Rule;

    /// <summary>The paper set's pinned variant (None for another rule).</summary>
    public readonly PaperSetBreak PaperBreak;

    /// <summary>The papers' dates' pinned variant (None for another rule).</summary>
    public readonly PaperDateFault DateFault;

    /// <summary>Creates a plan.</summary>
    public DirectivePlan(bool isFault, TravelRuleType rule, PaperSetBreak paperBreak, PaperDateFault dateFault)
    {
        IsFault = isFault;
        Rule = rule;
        PaperBreak = paperBreak;
        DateFault = dateFault;
    }
}

/// <summary>One of today's Directives as the rules see it: its type, the kinds it applies to (empty: every kind) and, for a closure, the place it names by ids.</summary>
public readonly struct Directive
{
    /// <summary>What the rule forbids.</summary>
    public readonly TravelRuleType Type;

    /// <summary>The kinds the rule applies to; empty for every kind.</summary>
    public readonly IReadOnlyList<TravellerKind> Kinds;

    /// <summary>The nation id a closure names (NationForbidden, NationEraForbidden); null otherwise.</summary>
    public readonly string NationId;

    /// <summary>The era id a closure names (EraForbidden, NationEraForbidden); null otherwise.</summary>
    public readonly string EraId;

    /// <summary>The model a recall grounds (an agency.transponders id; TransponderRecall); null otherwise.</summary>
    public readonly string Transponder;

    /// <summary>The places an open-destinations rule leaves open (<see cref="Directives.PlaceKey"/> keys, "country:era"); empty otherwise.</summary>
    public readonly IReadOnlyList<string> Open;

    /// <summary>Creates a directive.</summary>
    public Directive(TravelRuleType type, IReadOnlyList<TravellerKind> kinds, string nationId = null, string eraId = null, string transponder = null,
                     IReadOnlyList<string> open = null)
    {
        Type = type;
        Kinds = kinds ?? new TravellerKind[0];
        NationId = nationId;
        EraId = eraId;
        Transponder = transponder;
        Open = open ?? new string[0];
    }

    /// <summary>True when the rule applies to a traveller of <paramref name="kind"/>: listed, or no kind is listed.</summary>
    public bool AppliesTo(TravellerKind kind) => Kinds.Count == 0 || Kinds.Contains(kind);

    /// <summary>True when the rule is a closure that forbids the destination <paramref name="nationId"/> in <paramref name="eraId"/> (Directives.Closes).</summary>
    public bool Closes(string nationId, string eraId) =>
        Type == TravelRuleType.OpenDestinations ? !Open.Contains(Directives.PlaceKey(nationId, eraId)) : Directives.Closes(Type, NationId, EraId, nationId, eraId);
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

    /// <summary>The id of the transponder model the manifest prints (RecordLies.ModelIdOf over the agency's models); null without a manifest or an unreadable unit.</summary>
    public string ManifestModelId;

    /// <summary>The form numbers of the papers the traveller carries ("TC-520").</summary>
    public IReadOnlyCollection<string> Forms = new string[0];

    /// <summary>True when the Stranding Waiver is carried and signed (false when it is missing or reads "UNSIGNED").</summary>
    public bool WaiverSigned;

    /// <summary>True when the Citizen Account's standing is Frozen.</summary>
    public bool Frozen;

    /// <summary>The departure dates the papers print (the manifest's, the return order's), as printed; the PaperDates rule reads them against <see cref="Today"/>.</summary>
    public IEnumerable<string> Departures = new string[0];

    /// <summary>The Valid Until dates the papers print, as printed.</summary>
    public IEnumerable<string> ValidUntils = new string[0];

    /// <summary>Today on the agency calendar; null when the calendar cannot count today (the dates are then not read).</summary>
    public DateTime? Today;

    /// <summary>The forms in circulation today (DayPapers, world_source.json days[].papers); empty: every form. A paper set never asks for a form not issued yet.</summary>
    public IReadOnlyCollection<string> Issued = new string[0];

    /// <summary>True when the traveller carries the form numbered <paramref name="formNumber"/>.</summary>
    public bool Carries(string formNumber) => Forms != null && Forms.Contains(formNumber);

    /// <summary>True when the form numbered <paramref name="formNumber"/> is issued today (DayPapers.Issued over <see cref="Issued"/>).</summary>
    public bool IsIssued(string formNumber) => DayPapers.Issued(Issued, formNumber);
}

/// <summary>Which date a planned paper-dates fault falsifies (Directives.PlanDateFault).</summary>
public enum PaperDateFault
{
    /// <summary>No date fault (nothing printed to falsify).</summary>
    None,

    /// <summary>The departure date (the manifest's or the return order's) is another day than today.</summary>
    Departure,

    /// <summary>One Valid Until (the visa's, a proof of means' or the certificate's) has passed.</summary>
    Expiry
}

/// <summary>A traveller's planned paper-dates fault: the variant and, for an expiry, which of the expiring forms (in paper order).</summary>
public readonly struct PaperDatePlan
{
    /// <summary>The variant (None: nothing to falsify).</summary>
    public readonly PaperDateFault Fault;

    /// <summary>The index of the expiring form whose Valid Until has passed, among the expiring forms in paper order; -1 unless <see cref="PaperDateFault.Expiry"/>.</summary>
    public readonly int ExpiryIndex;

    /// <summary>Creates a plan.</summary>
    public PaperDatePlan(PaperDateFault fault, int expiryIndex)
    {
        Fault = fault;
        ExpiryIndex = expiryIndex;
    }
}

/// <summary>
/// The Directives' rules over the rule types (traveller types P3, P4, §5.3):
/// which types close destinations, which day a rule first stands on, and
/// which rules guarantee a faulty traveller in the first half of the queue
/// (CaseFactory.PlanViolators asks each such rule's maker for its slot); and
/// the PaperDates procedure's predicate and makers (F7, §5.4): depart only on
/// the date on the manifest or the return order, never on an expired paper,
/// read against the agency calendar; its faulty traveller has one date
/// falsified. NoPresentGoods has no predicate: a smuggler breaks it, a
/// deviation fault (LieKind.Smuggling). The paper set and the debt standing
/// (phase 9) have their predicates over CaseFacts (Breaks; Fault reads every
/// rule once over the finished papers and account), the paper-set maker's
/// variants, the violation roll of a later day, and the content checks
/// Generate World and the validator share (R6-006). Pure, so the decision
/// tables and the draws are tested headless.
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

    /// <summary>The Signature box of a waiver nobody signed (the paper-set maker's PaperSetBreak.WaiverUnsigned).</summary>
    public const string UnsignedMark = "UNSIGNED";

    /// <summary>
    /// True when a waiver's <paramref name="paper"/> is signed: false when its
    /// Signature box is blank or reads <see cref="UnsignedMark"/> (any case,
    /// trimmed); a paper with no Signature box states nothing unsigned. The
    /// number is not read here: a number the account never registered is a
    /// forgery (L3, RecordLies), not a paper-set fault.
    /// </summary>
    public static bool IsSigned(IReadOnlyList<DocumentField> paper)
    {
        foreach (DocumentField field in paper ?? Array.Empty<DocumentField>())
            if (field != null && field.category == ClueCategory.Signature)
                return !string.IsNullOrWhiteSpace(field.value) && !string.Equals(field.value.Trim(), UnsignedMark, StringComparison.OrdinalIgnoreCase);
        return true;
    }

    /// <summary>How many days off, at most, a falsified departure is (1 to 3 days before or after today, §5.4).</summary>
    public const int DepartureOffsetMaxDays = 3;

    /// <summary>How many days ago, at most, a falsified Valid Until passed (1 to 30 days before today, §5.4).</summary>
    public const int ExpiredMaxDays = 30;

    /// <summary>True for the closure types (a forbidden era, nation or place, or the open destinations, which forbid every other place), which forbid destinations; false for a standing procedure.</summary>
    public static bool IsClosure(TravelRuleType type) =>
        type == TravelRuleType.EraForbidden || type == TravelRuleType.NationForbidden || type == TravelRuleType.NationEraForbidden
        || type == TravelRuleType.OpenDestinations;

    /// <summary>A place's key, "country:era" (an open-destinations rule's places, world_source.json rules[].places); blank ids give a key no place has.</summary>
    public static string PlaceKey(string nationId, string eraId) => (nationId ?? string.Empty) + ":" + (eraId ?? string.Empty);

    /// <summary>
    /// The closure predicate, by ids (one home for TravelRuleSO.Allows and
    /// the content checks): an era closure forbids every destination of its
    /// era (<paramref name="ruleEraId"/>), a nation closure every one of its
    /// nation, a nation-era closure its one place; a procedure closes nothing.
    /// </summary>
    public static bool Closes(TravelRuleType type, string ruleNationId, string ruleEraId, string nationId, string eraId)
    {
        switch (type)
        {
            case TravelRuleType.EraForbidden:
                return string.Equals(eraId, ruleEraId, StringComparison.Ordinal);
            case TravelRuleType.NationForbidden:
                return string.Equals(nationId, ruleNationId, StringComparison.Ordinal);
            case TravelRuleType.NationEraForbidden:
                return string.Equals(nationId, ruleNationId, StringComparison.Ordinal) && string.Equals(eraId, ruleEraId, StringComparison.Ordinal);
            default:
                return false;
        }
    }

    /// <summary>
    /// A rule's first day: the smallest of <paramref name="daysListed"/>
    /// (the day numbers of the plans that list it); 0 when no plan lists it
    /// (a null list counts as empty).
    /// </summary>
    public static int FirstDay(IEnumerable<int> daysListed)
    {
        int first = 0;
        if (daysListed == null)
            return first;

        foreach (int day in daysListed)
            if (first == 0 || day < first)
                first = day;

        return first;
    }

    /// <summary>
    /// Whether an active rule plans a guaranteed faulty traveller in the
    /// first half of today's queue (P4): a closure every day it is active
    /// (a traveller bound for a place it forbids); the displaced's return
    /// home (a false-origin liar, L7 or L8), no 2150 goods (a smuggler, L6),
    /// the papers' dates (a falsified date), the paper set (a breaker made by
    /// <see cref="PickPaperSetBreak"/>), the debt standing (a frozen debtor)
    /// and dress for the destination (a costume error) each on its first day,
    /// the day the rule is announced; never a procedure line.
    /// </summary>
    public static bool Guarantees(TravelRuleType type, int today, int firstDay) =>
        IsClosure(type) || (HasMaker(type) && today == firstDay);

    /// <summary>True for the procedures with a maker, guaranteed one faulty traveller on their first day: the return home, no 2150 goods, the papers' dates, the paper set, the debt standing, dress for the destination (a costume error) and a transponder recall (days 7-15); never a procedure line.</summary>
    public static bool HasMaker(TravelRuleType type) =>
        type == TravelRuleType.ReturnHome || type == TravelRuleType.NoPresentGoods || type == TravelRuleType.PaperDates ||
        type == TravelRuleType.PaperSet || type == TravelRuleType.DebtStanding || type == TravelRuleType.DressForDestination ||
        type == TravelRuleType.TransponderRecall;

    /// <summary>True for a procedure the violation roll may break on a later day (traveller types P4): the paper set, the debt standing, the papers' dates and a transponder recall (dress has its own roll, the costume roll; the return home and no 2150 goods are broken by the lie roll).</summary>
    public static bool IsRolled(TravelRuleType type) =>
        type == TravelRuleType.PaperSet || type == TravelRuleType.DebtStanding || type == TravelRuleType.PaperDates || type == TravelRuleType.TransponderRecall;

    /// <summary>The directive fault a broken rule of <paramref name="type"/> is: a closed destination, incomplete papers, a frozen account, a wrong departure date (the papers' dates' first fault; an expired paper is their second, <see cref="PaperDates"/>); None for the types with no predicate.</summary>
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
            case TravelRuleType.PaperDates:
                return DirectiveFault.WrongDepartureDate;
            case TravelRuleType.TransponderRecall:
                return DirectiveFault.RecalledTransponder;
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
    /// (a form not carried states nothing about its class; a form not issued
    /// yet, CaseFacts.Issued, is never asked for: lesson D7); the debt
    /// standing breaks on a Frozen account; the papers' dates break on a
    /// departure dated another day or an expired Valid Until (<see cref="PaperDates"/>,
    /// when the calendar counts today); the dress rule, the return home, no
    /// 2150 goods and a procedure line never break here (a costume error, a
    /// false origin and smuggling are deviation faults; a line has no
    /// predicate). The displaced break no paper set.
    /// </summary>
    public static bool Breaks(TravelRuleType type, CaseFacts facts) => facts != null && FaultOf(type, facts) != DirectiveFault.None;

    /// <summary>
    /// <see cref="Breaks(TravelRuleType, CaseFacts)"/> for one rule, which a
    /// transponder recall needs (its model): the recall breaks when the
    /// manifest prints the recalled model (CaseFacts.ManifestModelId); no
    /// manifest breaks nothing (the paper set catches it).
    /// </summary>
    public static bool Breaks(Directive rule, CaseFacts facts) => facts != null && FaultOf(rule, facts) != DirectiveFault.None;

    /// <summary>The fault one rule finds in <paramref name="facts"/> (a recall reads its model; every other type, <see cref="FaultOf(TravelRuleType, CaseFacts)"/>).</summary>
    public static DirectiveFault FaultOf(Directive rule, CaseFacts facts)
    {
        if (facts == null)
            return DirectiveFault.None;
        if (rule.Type == TravelRuleType.TransponderRecall)
            return !string.IsNullOrEmpty(rule.Transponder) && string.Equals(facts.ManifestModelId, rule.Transponder, StringComparison.Ordinal)
                ? DirectiveFault.RecalledTransponder
                : DirectiveFault.None;
        return FaultOf(rule.Type, facts);
    }

    /// <summary>The fault a rule of <paramref name="type"/> finds in <paramref name="facts"/>: the type's fault when it breaks (<see cref="FaultOf(TravelRuleType)"/>; the papers' dates the first of their two), None otherwise.</summary>
    public static DirectiveFault FaultOf(TravelRuleType type, CaseFacts facts)
    {
        if (facts == null)
            return DirectiveFault.None;
        if (IsClosure(type))
            return facts.ClosedDestination ? DirectiveFault.ClosedDestination : DirectiveFault.None;

        switch (type)
        {
            case TravelRuleType.PaperSet:
                return PaperSetBroken(facts) ? DirectiveFault.IncompletePapers : DirectiveFault.None;
            case TravelRuleType.DebtStanding:
                return TravellerKinds.IsCitizen(facts.Kind) && facts.Frozen ? DirectiveFault.FrozenAccount : DirectiveFault.None;
            case TravelRuleType.PaperDates:
                return facts.Today.HasValue ? PaperDates(facts.Departures, facts.ValidUntils, facts.Today.Value) : DirectiveFault.None;
            default:
                return DirectiveFault.None;
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
                return facts.ManifestClass == TransponderClass.Premium || (facts.IsIssued(Waiver) && !facts.WaiverSigned)
                       || (Proofs.Any(facts.IsIssued) && !Proofs.Any(facts.Carries));
            case TravellerKind.Labourer:
                return (facts.IsIssued(Contract) && !facts.Carries(Contract)) || facts.ManifestClass == TransponderClass.Premium
                       || (facts.IsIssued(Waiver) && !facts.WaiverSigned);
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
        {
            if (!rule.AppliesTo(facts.Kind))
                continue;
            DirectiveFault fault = FaultOf(rule, facts);
            if (fault != DirectiveFault.None)
                return fault;
        }

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
    /// can, None with no draw when none can or without a stream. A
    /// <paramref name="pinned"/> variant (a forced slot's authored fault)
    /// is taken with no draw when it can show, else None.
    /// </summary>
    public static PaperSetBreak PickPaperSetBreak(TravellerKind kind, IReadOnlyCollection<string> forms, IRandomSource rng, PaperSetBreak pinned = PaperSetBreak.None)
    {
        List<PaperSetBreak> breaks = PaperSetBreaks(kind, forms);
        if (pinned != PaperSetBreak.None)
            return breaks.Contains(pinned) ? pinned : PaperSetBreak.None;
        if (breaks.Count == 0 || rng == null)
            return PaperSetBreak.None;
        return breaks.Count == 1 ? breaks[0] : breaks[rng.Range(0, breaks.Count)];
    }

    /// <summary>
    /// A forced slot's authored directive fault (days 7-15 B6) as its maker
    /// runs it: the procedure it breaks and the variant pinned for the
    /// maker (the paper set's missing, unsigned or Economy variant, the
    /// papers' dates' departure or expiry); not a fault for None.
    /// </summary>
    public static DirectivePlan Plan(PlannedDirective planned)
    {
        switch (planned)
        {
            case PlannedDirective.EconomyManifest:
                return new DirectivePlan(true, TravelRuleType.PaperSet, PaperSetBreak.EconomyManifest, PaperDateFault.None);
            case PlannedDirective.WaiverMissing:
                return new DirectivePlan(true, TravelRuleType.PaperSet, PaperSetBreak.WaiverMissing, PaperDateFault.None);
            case PlannedDirective.WaiverUnsigned:
                return new DirectivePlan(true, TravelRuleType.PaperSet, PaperSetBreak.WaiverUnsigned, PaperDateFault.None);
            case PlannedDirective.ProofMissing:
                return new DirectivePlan(true, TravelRuleType.PaperSet, PaperSetBreak.ProofMissing, PaperDateFault.None);
            case PlannedDirective.Frozen:
                return new DirectivePlan(true, TravelRuleType.DebtStanding, PaperSetBreak.None, PaperDateFault.None);
            case PlannedDirective.DepartureDate:
                return new DirectivePlan(true, TravelRuleType.PaperDates, PaperSetBreak.None, PaperDateFault.Departure);
            case PlannedDirective.Expired:
                return new DirectivePlan(true, TravelRuleType.PaperDates, PaperSetBreak.None, PaperDateFault.Expiry);
            case PlannedDirective.Recalled:
                return new DirectivePlan(true, TravelRuleType.TransponderRecall, PaperSetBreak.None, PaperDateFault.None);
            default:
                return new DirectivePlan(false, TravelRuleType.EraForbidden, PaperSetBreak.None, PaperDateFault.None);
        }
    }

    /// <summary>
    /// True when a traveller of <paramref name="kind"/> carrying
    /// <paramref name="forms"/> can break a rule of <paramref name="type"/>
    /// through its maker (§5.4): the paper set when a variant can show, the
    /// debt standing for a 2150 citizen (a frozen account), dress for a 2150
    /// citizen (a costume error), the papers' dates for every kind (each
    /// prints a departure or a Valid Until; the maker warns when none is
    /// printed). Never a closure, the return home or no 2150 goods (the lie
    /// roll's) or a procedure line here.
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
            case TravelRuleType.PaperDates:
                return true;
            case TravelRuleType.TransponderRecall:
                return forms != null && forms.Contains(Manifest) && AccountMaker.StatusOf(kind, out CitizenStatus status) && AccountMaker.ClassOf(status) == TransponderClass.Economy;
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

    /// <summary>A rule as the content checks see it: its asset name, type, kinds and first day (Directives.FirstDay over the days listing it).</summary>
    public readonly struct RuleEntry
    {
        /// <summary>The rule asset's name.</summary>
        public readonly string Asset;

        /// <summary>What it forbids.</summary>
        public readonly TravelRuleType Type;

        /// <summary>The kinds it applies to (empty: every kind).</summary>
        public readonly IReadOnlyList<TravellerKind> Kinds;

        /// <summary>The first day a plan lists it (0: none).</summary>
        public readonly int FirstDay;

        /// <summary>The model a recall grounds (an agency.transponders id); null for every other rule.</summary>
        public readonly string Transponder;

        /// <summary>Creates an entry.</summary>
        public RuleEntry(string asset, TravelRuleType type, IReadOnlyList<TravellerKind> kinds, int firstDay, string transponder = null)
        {
            Asset = asset;
            Type = type;
            Kinds = kinds ?? new TravellerKind[0];
            FirstDay = firstDay;
            Transponder = transponder;
        }
    }

    /// <summary>
    /// What Generate World and the validator refuse of one rule (R6-006): a
    /// procedure names no country or era and has its line; a closure needs
    /// no line and may list the kinds it closes for (none: every kind; the
    /// Economy range limit, days 7-15 §6.1); a paper set or debt standing lists at least one kind, since
    /// each has a kind's paper set to read; a transponder recall (days 7-15
    /// V9) names an Economy model of <paramref name="transponders"/> and lists
    /// only kinds that travel on an Economy unit; only a recall names a
    /// <paramref name="transponder"/>. Empty when sound.
    /// </summary>
    public static List<string> RuleProblems(string asset, TravelRuleType type, IReadOnlyList<TravellerKind> kinds, bool hasPlace, bool hasLine,
                                            string transponder = null, IReadOnlyList<TransponderModel> transponders = null, IReadOnlyList<string> openPlaces = null)
    {
        var problems = new List<string>();
        kinds = kinds ?? new TravellerKind[0];
        if (type != TravelRuleType.TransponderRecall && !string.IsNullOrWhiteSpace(transponder))
            problems.Add($"Rule '{asset}' ({type}) names the transponder '{transponder}'; only a recall (TransponderRecall) names one.");
        if (type == TravelRuleType.OpenDestinations)
        {
            if (hasPlace)
                problems.Add($"Rule '{asset}' lists the open destinations (OpenDestinations): it names its places in \"places\", not a country or era.");
            if (openPlaces == null || openPlaces.Count == 0)
                problems.Add($"Rule '{asset}' (OpenDestinations) lists no places: it would close every destination.");
            if (!hasLine)
                problems.Add($"Rule '{asset}' (OpenDestinations) needs its directive line (\"description\"), naming the open destinations.");
        }
        else if (openPlaces != null && openPlaces.Count > 0)
            problems.Add($"Rule '{asset}' ({type}) lists places; only the open destinations (OpenDestinations) do.");
        if (IsClosure(type))
            return problems;

        if (hasPlace)
            problems.Add($"Rule '{asset}' is a standing procedure ({type}), but names a country or era; a procedure names none.");
        if (!hasLine)
            problems.Add($"Rule '{asset}' is a standing procedure ({type}): it needs its directive line (\"description\").");
        if ((type == TravelRuleType.PaperSet || type == TravelRuleType.DebtStanding || type == TravelRuleType.TransponderRecall) && kinds.Count == 0)
            problems.Add($"Rule '{asset}' ({type}) lists no kinds; a paper set, debt standing or recall names the kinds it is read for.");
        if (type == TravelRuleType.TransponderRecall)
        {
            TransponderModel model = (transponders ?? new TransponderModel[0]).FirstOrDefault(t => t != null && t.id == transponder);
            if (string.IsNullOrWhiteSpace(transponder))
                problems.Add($"Rule '{asset}' is a recall that names no model (\"transponder\", an agency.transponders id).");
            else if (model == null)
                problems.Add($"Rule '{asset}' recalls the model '{transponder}', which agency.transponders does not have.");
            else if (model.transponderClass != TransponderClass.Economy)
                problems.Add($"Rule '{asset}' recalls '{transponder}', a {model.transponderClass} model; a recall grounds an Economy model, the unit its kinds travel on.");
            foreach (TravellerKind kind in kinds.Where(k => !(AccountMaker.StatusOf(k, out CitizenStatus status) && AccountMaker.ClassOf(status) == TransponderClass.Economy)))
                problems.Add($"Rule '{asset}' lists the kind {kind}, which travels on no Economy unit, so the recall could never ground it.");
        }
        return problems;
    }

    /// <summary>
    /// What Generate World and the validator refuse of one day's Directives
    /// (R6-006): a paper set or debt standing active on day <paramref name="today"/>
    /// that no kind of the day (<paramref name="kinds"/>, those with a
    /// positive weight, each with its blueprint's form numbers) can break
    /// through its maker (<see cref="CanBreak"/>), so nobody could ever test
    /// it; and a procedure guaranteed a breaker today (<see cref="Guarantees"/>,
    /// its first day) that no kind of the day can break, so its guarantee
    /// would plan nobody (the return home and no 2150 goods have their own
    /// checks, the day's displaced and lies); a closure listing kinds none of
    /// the day's kinds is (its guaranteed violator could never be drawn); and
    /// recalls that ground every
    /// Economy model of <paramref name="transponders"/> (days 7-15 V9: nobody
    /// could travel Economy). Empty when sound.
    /// </summary>
    public static List<string> DayProblems(string day, int today, IReadOnlyList<RuleEntry> active, IReadOnlyList<(TravellerKind kind, IReadOnlyCollection<string> forms)> kinds,
                                           IReadOnlyList<TransponderModel> transponders = null)
    {
        var problems = new List<string>();
        active = active ?? new RuleEntry[0];
        kinds = kinds ?? new (TravellerKind, IReadOnlyCollection<string>)[0];

        bool Breakable(RuleEntry rule) =>
            kinds.Any(k => new Directive(rule.Type, rule.Kinds).AppliesTo(k.kind) && CanBreak(rule.Type, k.kind, k.forms));

        foreach (RuleEntry rule in active)
        {
            if (IsRolled(rule.Type) && !Breakable(rule))
                problems.Add($"Day '{day}' lists the rule '{rule.Asset}' ({rule.Type}), which none of its kinds can break: no traveller could ever test it.");
            else if (rule.Type == TravelRuleType.DressForDestination && Guarantees(rule.Type, today, rule.FirstDay) && !Breakable(rule))
                problems.Add($"Day '{day}' is the first day of the rule '{rule.Asset}' ({rule.Type}), which guarantees a breaker none of its kinds can be: list a 2150 citizen kind.");
            else if (IsClosure(rule.Type) && rule.Kinds.Count > 0 && !kinds.Any(k => rule.Kinds.Contains(k.kind)))
                problems.Add($"Day '{day}' lists the closure '{rule.Asset}' for [{string.Join(", ", rule.Kinds)}], none of which the day weights: its guaranteed violator could never be drawn.");
        }

        List<string> recalled = active.Where(r => r.Type == TravelRuleType.TransponderRecall && !string.IsNullOrWhiteSpace(r.Transponder)).Select(r => r.Transponder).ToList();
        if (recalled.Count > 0 && transponders != null && transponders.Any(t => t != null && t.transponderClass == TransponderClass.Economy) &&
            !Unrecalled(transponders, recalled).Any(t => t.transponderClass == TransponderClass.Economy))
            problems.Add($"Day '{day}' recalls every Economy model ({string.Join(", ", recalled)}): nobody could travel Economy. Leave one unrecalled.");

        return problems;
    }

    /// <summary>
    /// The PaperDates directive (F7): the first fault it finds, in this
    /// order: a readable departure date that is not <paramref name="today"/>
    /// (DirectiveFault.WrongDepartureDate), then a readable Valid Until before
    /// today (DirectiveFault.ExpiredPaper); None when every readable date
    /// passes. A date the calendar cannot read (a placeholder) is skipped:
    /// a broken calendar is the content's problem, never the traveller's.
    /// Null lists count as empty.
    /// </summary>
    public static DirectiveFault PaperDates(IEnumerable<string> departures, IEnumerable<string> validUntils, DateTime today)
    {
        if (departures != null)
            foreach (string text in departures)
                if (AgencyCalendar.TryRead(text, out DateTime departure) && departure.Date != today.Date)
                    return DirectiveFault.WrongDepartureDate;

        if (validUntils != null)
            foreach (string text in validUntils)
                if (AgencyCalendar.TryRead(text, out DateTime validUntil) && validUntil.Date < today.Date)
                    return DirectiveFault.ExpiredPaper;

        return DirectiveFault.None;
    }

    /// <summary>
    /// The maker's variant (§5.4), one Range draw over the dates the papers
    /// print: the departure when <paramref name="hasDeparture"/>, then each of
    /// the <paramref name="expiringForms"/> Valid Untils in paper order. None,
    /// with no draw, when nothing is printed or <paramref name="rng"/> is null.
    /// A <paramref name="pinned"/> variant (a forced slot's authored fault)
    /// skips the choice: the departure with no draw (None when none is
    /// printed), or an expiry drawn over the expiring forms only.
    /// </summary>
    public static PaperDatePlan PlanDateFault(bool hasDeparture, int expiringForms, IRandomSource rng, PaperDateFault pinned = PaperDateFault.None)
    {
        if (pinned == PaperDateFault.Departure)
            return new PaperDatePlan(hasDeparture ? PaperDateFault.Departure : PaperDateFault.None, -1);
        if (pinned == PaperDateFault.Expiry)
            hasDeparture = false;

        int options = (hasDeparture ? 1 : 0) + Math.Max(0, expiringForms);
        if (rng == null || options == 0)
            return new PaperDatePlan(PaperDateFault.None, -1);

        int pick = rng.Range(0, options);
        if (hasDeparture && pick == 0)
            return new PaperDatePlan(PaperDateFault.Departure, -1);

        return new PaperDatePlan(PaperDateFault.Expiry, hasDeparture ? pick - 1 : pick);
    }

    /// <summary>A departure 1 to <see cref="DepartureOffsetMaxDays"/> days before or after <paramref name="today"/>, never today: one Range draw over the six offsets.</summary>
    public static DateTime OffsetDeparture(DateTime today, IRandomSource rng)
    {
        int pick = rng.Range(0, DepartureOffsetMaxDays * 2);
        int offset = pick < DepartureOffsetMaxDays ? pick - DepartureOffsetMaxDays : pick - DepartureOffsetMaxDays + 1;
        return today.AddDays(offset);
    }

    /// <summary>
    /// The recall's maker (days 7-15 §6): the traveller's unit becomes the
    /// recalled <paramref name="model"/> with a serial nobody holds today
    /// (AgencyNumbers.TakeUnique over AccountMaker.Serial, which joins
    /// <paramref name="takenToday"/>), printed honestly on the manifest and the
    /// waiver: one draw per attempt.
    /// </summary>
    public static string RecalledUnit(TransponderModel model, ISet<string> takenToday, IRandomSource rng) =>
        AccountMaker.TransponderName(model.model, AgencyNumbers.TakeUnique(takenToday, () => AccountMaker.Serial(model.prefix, rng)));

    /// <summary>
    /// The transponder models a unit may be drawn from while recalls stand
    /// (days 7-15 §6, K5): <paramref name="models"/> without the recalled ids,
    /// in order, so nobody holds a recalled unit except through the recall's
    /// maker and every pick stays one draw. Empty for null models.
    /// </summary>
    public static List<TransponderModel> Unrecalled(IReadOnlyList<TransponderModel> models, ICollection<string> recalledIds) =>
        (models ?? new TransponderModel[0]).Where(m => m != null && (recalledIds == null || !recalledIds.Contains(m.id))).ToList();

    /// <summary>A Valid Until 1 to <see cref="ExpiredMaxDays"/> days before <paramref name="today"/>: one draw (AgencyNumbers.DaysAgo).</summary>
    public static DateTime ExpiredValidUntil(DateTime today, IRandomSource rng) => AgencyNumbers.DaysAgo(today, ExpiredMaxDays, rng);
}
