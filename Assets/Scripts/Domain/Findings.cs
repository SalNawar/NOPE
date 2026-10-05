using System;
using System.Collections.Generic;

/// <summary>
/// What one comparison on the workbench said (the PC workbench spec §4.2,
/// §4.3): the label on the line between the two values and whether the
/// findings column logs it. Runtime only (not serialized).
/// </summary>
public enum FindingKind
{
    /// <summary>Same detail, same value ("Matching data"). Logged.</summary>
    Match,

    /// <summary>Same detail, different values, or a proof by mismatch ("Data differs"). Logged; a difference.</summary>
    Differs,

    /// <summary>A proof by foreign origin: the stated value belongs to another place's reference row ("Belongs elsewhere"). Logged; a difference.</summary>
    Elsewhere,

    /// <summary>Two different details, a name against a date ("Different details"). Not logged.</summary>
    DifferentDetails,

    /// <summary>A reference row of another place than the claimed one, proving nothing ("Not the claimed place"). Not logged.</summary>
    OtherPlace,

    /// <summary>A record row of another person ("Another person's record"). Not logged.</summary>
    OtherPerson,

    /// <summary>Two truths: two book rows, a book row and a record row ("Both are reference data"). Not logged.</summary>
    TwoTruths,

    /// <summary>A rule held against the value it is about, which meets it ("Meets the rule"). Logged.</summary>
    RuleMet,

    /// <summary>A rule held against the value it is about, which breaks it ("Breaks the rule"). Logged; a difference.</summary>
    RuleBroken,

    /// <summary>A rule held against a value it is not about. Not logged.</summary>
    RuleNotAbout,

    /// <summary>A rule checked by comparing values (dress, return home, no 2150 goods, a procedure line). Not logged.</summary>
    RuleByComparison,

    /// <summary>A rule that is not read for the traveller's kind. Not logged.</summary>
    RuleNotForTraveller,

    /// <summary>Two rules, a rule and the calendar, or the calendar twice: nothing to compare. Not logged.</summary>
    NothingToCompare,

    /// <summary>The calendar's today against a departure dated today ("Departs today"). Logged.</summary>
    DepartsToday,

    /// <summary>The calendar's today against a departure dated another day ("Not today's date"). Logged; a difference.</summary>
    NotToday,

    /// <summary>The calendar's today against a Valid Until that has not passed ("Still valid"). Logged.</summary>
    StillValid,

    /// <summary>The calendar's today against a Valid Until that has passed ("Expired"). Logged; a difference.</summary>
    Expired,

    /// <summary>A paper's seal that is not its office's true seal, held against the Seal Register (the document design spec, D4: "Seal incorrect"). Logged; a difference.</summary>
    SealIncorrect,

    /// <summary>A seal held against another office's seal, proving nothing ("Another office"). Not logged.</summary>
    OtherOffice
}

/// <summary>How a finding looks: its line's and plate's colour.</summary>
public enum FindingLook
{
    /// <summary>Grey and dashed: nothing logged.</summary>
    Info,

    /// <summary>Green: a match, a rule met, a date that holds.</summary>
    Match,

    /// <summary>Red: a difference, a rule broken, a date that fails.</summary>
    Differ
}

/// <summary>What a rule held against a value says (RuleChecks.Check).</summary>
public enum RuleVerdict
{
    /// <summary>The rule is not about the value's detail.</summary>
    NotAbout,

    /// <summary>The value meets the rule.</summary>
    Meets,

    /// <summary>The value breaks the rule.</summary>
    Breaks,

    /// <summary>The rule is checked by comparing values against the books or the record, not against a rule.</summary>
    ByComparison,

    /// <summary>The rule is not read for the traveller's kind.</summary>
    NotForTraveller
}

/// <summary>
/// The workbench's rules for a comparison (the PC workbench spec §4.2, §4.3;
/// lesson 2: the player points, the game only confirms): what two values
/// say, what a rule or the calendar's today says of a value, how each result
/// looks and whether it is logged. The proof itself stays DiscrepancyLog's
/// (Prove): a proof always reads as a difference. Pure.
/// </summary>
public static class FindingRules
{
    /// <summary>
    /// What two picked values say (<paramref name="proof"/>: DiscrepancyLog.Prove's
    /// result for the pair, or null): different details when their categories
    /// differ; a proof a difference (Elsewhere for a foreign origin); two
    /// truths nothing; a book row of another place than the claim, or a
    /// record row of another person, nothing; otherwise a match when the
    /// values match (Values.Match) and a difference when they do not. Seals
    /// are read by office (ClassifySeal); a photo against the person is a
    /// match or a difference like any value.
    /// </summary>
    public static FindingKind Classify(CompareEvidence a, CompareEvidence b, Discrepancy proof, string claimedNationId, string claimedEraId, string travellerName)
    {
        if (a.category != b.category)
            return FindingKind.DifferentDetails;
        if (a.category == ClueCategory.Seal)
            return ClassifySeal(a, b, proof);
        if (proof != null)
            return proof.provedBy == DiscrepancyProof.ForeignOrigin ? FindingKind.Elsewhere : FindingKind.Differs;

        bool truthA = IsTruth(a.kind), truthB = IsTruth(b.kind);
        if (truthA && truthB)
            return FindingKind.TwoTruths;
        if (truthA || truthB)
        {
            CompareEvidence truth = truthA ? a : b;
            if (truth.kind == EvidenceKind.ReferenceEntry && !DiscrepancyLog.AppliesToClaim(truth, claimedNationId, claimedEraId))
                return FindingKind.OtherPlace;
            if (truth.kind == EvidenceKind.RecordField && (string.IsNullOrWhiteSpace(travellerName) || !Values.Match(truth.recordOwner, travellerName)))
                return FindingKind.OtherPerson;
        }
        return Values.Match(a.value, b.value) ? FindingKind.Match : FindingKind.Differs;
    }

    /// <summary>
    /// Two seals (the document design spec, D4): a proof (a forged seal, or
    /// another office's seal on a paper, against the Seal Register) is "Seal
    /// incorrect"; two register seals are two truths; two seals of different
    /// offices prove nothing ("Another office", never a difference to deny
    /// on); two of one office match, or the paper's is incorrect.
    /// </summary>
    private static FindingKind ClassifySeal(CompareEvidence a, CompareEvidence b, Discrepancy proof)
    {
        if (proof != null)
            return FindingKind.SealIncorrect;
        if (IsTruth(a.kind) && IsTruth(b.kind))
            return FindingKind.TwoTruths;
        if (string.IsNullOrEmpty(a.issuer) || a.issuer != b.issuer)
            return FindingKind.OtherOffice;
        return Values.Match(a.value, b.value) ? FindingKind.Match : FindingKind.SealIncorrect;
    }

    /// <summary>
    /// What the calendar's today (<paramref name="today"/>) says of a value:
    /// a departure date is today or not, a Valid Until still holds or has
    /// passed (the papers' dates' rule, Directives.PaperDates, over that value
    /// alone); any other detail, or a date the calendar cannot read, is a
    /// different detail.
    /// </summary>
    public static FindingKind AgainstToday(ClueCategory category, string value, DateTime today)
    {
        if (!AgencyCalendar.TryRead(value, out _))
            return FindingKind.DifferentDetails;
        switch (category)
        {
            case ClueCategory.DepartureDate:
                return Directives.PaperDates(new[] { value }, null, today) == DirectiveFault.None ? FindingKind.DepartsToday : FindingKind.NotToday;
            case ClueCategory.Expiry:
                return Directives.PaperDates(null, new[] { value }, today) == DirectiveFault.None ? FindingKind.StillValid : FindingKind.Expired;
            default:
                return FindingKind.DifferentDetails;
        }
    }

    /// <summary>A rule's verdict as a finding.</summary>
    public static FindingKind Of(RuleVerdict verdict)
    {
        switch (verdict)
        {
            case RuleVerdict.Meets: return FindingKind.RuleMet;
            case RuleVerdict.Breaks: return FindingKind.RuleBroken;
            case RuleVerdict.ByComparison: return FindingKind.RuleByComparison;
            case RuleVerdict.NotForTraveller: return FindingKind.RuleNotForTraveller;
            default: return FindingKind.RuleNotAbout;
        }
    }

    /// <summary>How a finding looks.</summary>
    public static FindingLook Look(FindingKind kind)
    {
        switch (kind)
        {
            case FindingKind.Match:
            case FindingKind.RuleMet:
            case FindingKind.DepartsToday:
            case FindingKind.StillValid:
                return FindingLook.Match;
            case FindingKind.Differs:
            case FindingKind.Elsewhere:
            case FindingKind.RuleBroken:
            case FindingKind.NotToday:
            case FindingKind.Expired:
            case FindingKind.SealIncorrect:
                return FindingLook.Differ;
            default:
                return FindingLook.Info;
        }
    }

    /// <summary>True when the findings column logs the result (a match or a difference; never a note).</summary>
    public static bool IsLogged(FindingKind kind) => Look(kind) != FindingLook.Info;

    /// <summary>True for a difference: what Deny can cite.</summary>
    public static bool IsDifference(FindingKind kind) => Look(kind) == FindingLook.Differ;

    /// <summary>
    /// True for a directive fault's evidence (Saleh, 2026-10-05: a denial with
    /// no logged evidence earns a citation, a directive fault's too): a rule
    /// held against the value it breaks, the calendar against a departure
    /// dated another day or a Valid Until that has passed. A deviation's
    /// evidence is its proof (DiscrepancyLog), never counted here.
    /// </summary>
    public static bool IsDirectiveEvidence(FindingKind kind) =>
        kind == FindingKind.RuleBroken || kind == FindingKind.NotToday || kind == FindingKind.Expired;

    /// <summary>The ui string key of the line's label ("finding.link.Match").</summary>
    public static string LinkKey(FindingKind kind) => "finding.link." + kind;

    /// <summary>A truth source: a reference book's row or a record's row.</summary>
    private static bool IsTruth(EvidenceKind kind) => kind == EvidenceKind.ReferenceEntry || kind == EvidenceKind.RecordField;
}

/// <summary>
/// The detail a rule is about and the verdict of a rule held against a value
/// (the PC workbench spec §4.3). The verdict reads the case's facts (the
/// Directives' own predicates): a missing paper is not a value to click, so
/// a paper-set rule held against any paper-set detail says whether the set
/// is complete. Pure.
/// </summary>
public static class RuleChecks
{
    /// <summary>The details the paper-set rule is about: the visa's class, the transponder's class, the waiver's signature and number, the proofs of means, the contract.</summary>
    private static readonly ClueCategory[] PaperSetDetails =
    {
        ClueCategory.AccountStatus, ClueCategory.TransponderClass, ClueCategory.Signature, ClueCategory.WaiverNo,
        ClueCategory.Credit, ClueCategory.Funds, ClueCategory.PolicyNo, ClueCategory.Employer, ClueCategory.Term, ClueCategory.Wage
    };

    /// <summary>
    /// What <paramref name="rule"/> says of a value of <paramref name="category"/>
    /// (<paramref name="value"/>) for the traveller of <paramref name="facts"/>
    /// who claims <paramref name="claimedNationId"/> in <paramref name="claimedEraId"/>:
    /// not for them when the rule is not read for their kind; a closure is
    /// about the destination and breaks when it closes the claim; the paper
    /// set is about its details and breaks when the set is incomplete or wrong;
    /// the debt standing is about the status and the debt and breaks on a
    /// frozen account; the papers' dates are about a departure date (breaks
    /// when not today) and a Valid Until (breaks when passed), each read alone
    /// (a date the calendar cannot read is not about it; no calendar: meets);
    /// a recall is about the transponder and breaks when the manifest prints
    /// the recalled model; dress, the return home, no 2150 goods and a
    /// procedure line are checked by comparing values.
    /// </summary>
    public static RuleVerdict Check(Directive rule, ClueCategory category, string value, CaseFacts facts, string claimedNationId, string claimedEraId)
    {
        if (facts != null && !rule.AppliesTo(facts.Kind))
            return RuleVerdict.NotForTraveller;

        if (Directives.IsClosure(rule.Type))
            return category != ClueCategory.Destination ? RuleVerdict.NotAbout
                 : rule.Closes(claimedNationId, claimedEraId) ? RuleVerdict.Breaks : RuleVerdict.Meets;

        switch (rule.Type)
        {
            case TravelRuleType.PaperSet:
                return Array.IndexOf(PaperSetDetails, category) < 0 ? RuleVerdict.NotAbout : Verdict(Directives.FaultOf(rule, facts));
            case TravelRuleType.DebtStanding:
                return category != ClueCategory.AccountStatus && category != ClueCategory.Debt ? RuleVerdict.NotAbout : Verdict(Directives.FaultOf(rule, facts));
            case TravelRuleType.TransponderRecall:
                return category != ClueCategory.TransponderId ? RuleVerdict.NotAbout : Verdict(Directives.FaultOf(rule, facts));
            case TravelRuleType.PaperDates:
                if ((category != ClueCategory.DepartureDate && category != ClueCategory.Expiry) || !AgencyCalendar.TryRead(value, out _))
                    return RuleVerdict.NotAbout;
                if (facts == null || !facts.Today.HasValue)
                    return RuleVerdict.Meets;
                return Verdict(category == ClueCategory.DepartureDate
                    ? Directives.PaperDates(new[] { value }, null, facts.Today.Value)
                    : Directives.PaperDates(null, new[] { value }, facts.Today.Value));
            default:
                return RuleVerdict.ByComparison;
        }
    }

    /// <summary>A fault breaks the rule; none meets it.</summary>
    private static RuleVerdict Verdict(DirectiveFault fault) => fault != DirectiveFault.None ? RuleVerdict.Breaks : RuleVerdict.Meets;
}

/// <summary>One logged comparison: what it said, the two values' keys (PickKeys', or a rule's or the calendar's) and what each side read.</summary>
public sealed class Finding
{
    /// <summary>A finding; <paramref name="deviation"/> is the deviation the pair proved (DiscrepancyLog.Prove), or null.</summary>
    public Finding(FindingKind kind, string keyA, string keyB, string titleA, string valueA, string titleB, string valueB, string subject, Discrepancy deviation)
    {
        Kind = kind;
        KeyA = keyA ?? string.Empty;
        KeyB = keyB ?? string.Empty;
        TitleA = titleA ?? string.Empty;
        ValueA = valueA ?? string.Empty;
        TitleB = titleB ?? string.Empty;
        ValueB = valueB ?? string.Empty;
        Subject = subject ?? string.Empty;
        Deviation = deviation;
    }

    /// <summary>What the comparison said.</summary>
    public FindingKind Kind { get; }

    /// <summary>The first side's key (the value held first).</summary>
    public string KeyA { get; }

    /// <summary>The second side's key.</summary>
    public string KeyB { get; }

    /// <summary>Where the first side is ("Leisure Departure Visa"; a rule's line for a rule).</summary>
    public string TitleA { get; }

    /// <summary>The first side's value as shown.</summary>
    public string ValueA { get; }

    /// <summary>Where the second side is.</summary>
    public string TitleB { get; }

    /// <summary>The second side's value as shown.</summary>
    public string ValueB { get; }

    /// <summary>What was compared: the detail's word ("Visa class"), or the rule's line.</summary>
    public string Subject { get; }

    /// <summary>The deviation the pair proved (DiscrepancyLog.Prove: the same proof the Deviation Report documents), or null. What the wheel's question about it reads (Confrontations.About; wave 5, lesson 3).</summary>
    public Discrepancy Deviation { get; }

    /// <summary>True when the pair proved a deviation (DiscrepancyLog): "Logged as evidence".</summary>
    public bool Proof => Deviation != null;

    /// <summary>True when this finding joins the same two values as <paramref name="keyA"/> and <paramref name="keyB"/>, in either order.</summary>
    public bool Joins(string keyA, string keyB) =>
        (KeyA == keyA && KeyB == keyB) || (KeyA == keyB && KeyB == keyA);
}

/// <summary>
/// The case's findings column (the PC workbench spec §4.4; lesson 2: only
/// what the player compares is logged): each logged comparison once per pair
/// of values, in order. A difference enables Deny. Cleared with each case.
/// Pure.
/// </summary>
public sealed class FindingLog
{
    private readonly List<Finding> _items = new List<Finding>();

    /// <summary>The logged findings, oldest first.</summary>
    public IReadOnlyList<Finding> Items => _items;

    /// <summary>How many are logged.</summary>
    public int Count => _items.Count;

    /// <summary>True when a difference is logged (Deny can cite it).</summary>
    public bool HasDifference => FirstDifference != null;

    /// <summary>The first logged difference (what Deny cites), or null.</summary>
    public Finding FirstDifference => _items.Find(f => FindingRules.IsDifference(f.Kind));

    /// <summary>How many logged findings are differences.</summary>
    public int Differences => _items.FindAll(f => FindingRules.IsDifference(f.Kind)).Count;

    /// <summary>How many logged findings are a directive fault's evidence (FindingRules.IsDirectiveEvidence): what a denial counts beside the deviations proven.</summary>
    public int DirectiveEvidence => _items.FindAll(f => FindingRules.IsDirectiveEvidence(f.Kind)).Count;

    /// <summary>Empties the log (a new case).</summary>
    public void Clear() => _items.Clear();

    /// <summary>The finding joining the two keys (either order), or null.</summary>
    public Finding Find(string keyA, string keyB) => _items.Find(f => f.Joins(keyA, keyB));

    /// <summary>
    /// Logs <paramref name="finding"/>: false, with nothing added, when it is
    /// null, not a logged kind (FindingRules.IsLogged) or joins two values
    /// already logged together.
    /// </summary>
    public bool Add(Finding finding)
    {
        if (finding == null || !FindingRules.IsLogged(finding.Kind) || Find(finding.KeyA, finding.KeyB) != null)
            return false;
        _items.Add(finding);
        return true;
    }
}
