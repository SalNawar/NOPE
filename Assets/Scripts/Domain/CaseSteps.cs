using System;
using System.Collections.Generic;

/// <summary>
/// What a step of the optional steps checklist waits for (the PC redesign ST2,
/// ST3, §4.4): one of the player's own checks, never what the check found.
/// Serialized in the content library (StepSpec.when): append only
/// (SerializedEnumsTests pins every value).
/// </summary>
public enum StepWhen
{
    /// <summary>The case's papers (those of the step's forms, or all) handed over: one part each.</summary>
    PapersReceived,

    /// <summary>The case's papers (those of the step's forms, or all) read: examined at the desk, or shown in a pane while the player looks at the PC: one part each.</summary>
    PaperRead,

    /// <summary>Asked for through the traveller wheel: any of the step's forms (a request group, one part), or, with no forms, each paper handed over on request (one part each).</summary>
    Requested,

    /// <summary>The Rules tab shown while the traveller is at the desk and the player looks at the PC.</summary>
    RulesViewed,

    /// <summary>A record looked up in Records while the traveller is at the desk, whatever it found.</summary>
    RecordViewed,

    /// <summary>A statement of each of the step's categories paired with a truth of the same category (one part each); a MISMATCH counts as a MATCH.</summary>
    Compared,

    /// <summary>An answer heard in each of the step's categories (none named: today's question categories), one part each.</summary>
    Asked,

    /// <summary>A garment looked at through the wheel's Look menu.</summary>
    LookedAt
}

/// <summary>The statement side of a compared pair, a Compared step's filter. Serialized in the content library: append only.</summary>
public enum StatementKind
{
    /// <summary>Any statement.</summary>
    Any,

    /// <summary>A field of one of the traveller's papers.</summary>
    Field,

    /// <summary>A spoken answer.</summary>
    Answer,

    /// <summary>A garment the traveller wears (the Look menu).</summary>
    Garment
}

/// <summary>The truth side of a compared pair, a Compared step's filter. Serialized in the content library: append only.</summary>
public enum TruthKind
{
    /// <summary>Any truth.</summary>
    Any,

    /// <summary>A reference book's row.</summary>
    Reference,

    /// <summary>A row of a citizen record (whose record it is proves nothing here).</summary>
    Record,

    /// <summary>Another paper's field: a statement against a statement (the traveller-types spec's L4: papers prove each other).</summary>
    Paper
}

/// <summary>Where a click on a step goes (the PC redesign ST4). Serialized in the content library: append only.</summary>
public enum StepLink
{
    /// <summary>Nowhere: the step shows its hint instead.</summary>
    None,

    /// <summary>A tab of the Investigation app (StepSpec.tab).</summary>
    Tab,

    /// <summary>Records, with the primary paper's record looked up (by its Citizen ID, else its name).</summary>
    PrimaryName,

    /// <summary>The first paper field of the step's categories not compared yet: its book in Reference (a Record step: the primary paper's record; a Paper step: Documents).</summary>
    FirstUncheckedField,

    /// <summary>Reference, the Costume Guide (the claimed place's row comes first).</summary>
    CostumeClaimed
}

/// <summary>
/// One step of a checklist (world_source.json pc.steps.sets[].steps[], through
/// Generate World): what it waits for, what it counts, where a click goes.
/// Its label is the UI string "steps.{id}" (StepSets.LabelKey).
/// </summary>
[Serializable]
public sealed class StepSpec
{
    /// <summary>Stable id, unique in its set; a set that inherits replaces its parent's step of the same id.</summary>
    public string id;

    /// <summary>The check it waits for.</summary>
    public StepWhen when;

    /// <summary>
    /// The ClueCategory names a Compared or Asked step counts; empty = derived
    /// (CaseSteps.Categories). Kept by name, so a data-only set can name a
    /// category a later phase appends (it counts once the category exists).
    /// </summary>
    public List<string> categories = new List<string>();

    /// <summary>A Compared step's statement side.</summary>
    public StatementKind statement;

    /// <summary>A Compared step's truth side.</summary>
    public TruthKind truth;

    /// <summary>The form numbers ("TC-610") a paper step counts; empty = every paper.</summary>
    public List<string> forms = new List<string>();

    /// <summary>Where a click goes (None: the hint shows).</summary>
    public StepLink link;

    /// <summary>The tab a Tab link shows.</summary>
    public AppTab tab;

    /// <summary>The UI string key of the toast a click shows instead of a jump (the work is done at the desk: "Use the traveller wheel"); blank for a jump.</summary>
    public string hint = string.Empty;

    /// <summary>The first day the step is listed (its directive's first day).</summary>
    public int fromDay = 1;
}

/// <summary>A traveller kind's checklist (or the default one): its steps, or a parent's with its own replacing and added.</summary>
[Serializable]
public sealed class StepSet
{
    /// <summary>A TravellerKind name, or CaseSteps.DefaultType (the set of a kind that has none).</summary>
    public string type;

    /// <summary>The type of the set whose steps this one starts from (blank: none).</summary>
    public string inherit = string.Empty;

    /// <summary>
    /// Authored before its kind is in play: its forms and categories may name
    /// what a later phase adds (StepSets.Problems checks them once the mark goes).
    /// </summary>
    public bool dataOnly;

    /// <summary>Its steps, in list order.</summary>
    public List<StepSpec> steps = new List<StepSpec>();
}

/// <summary>Every checklist (the content library's PcContent.steps).</summary>
[Serializable]
public sealed class StepSetData
{
    /// <summary>The sets, one per type.</summary>
    public List<StepSet> sets = new List<StepSet>();
}

/// <summary>One step as the panel shows it: ticked or not, its parts ("Facts 1 of 3") and whether the player set it by hand.</summary>
public readonly struct StepState
{
    /// <summary>A step's state.</summary>
    public StepState(string id, bool done, int have, int need, bool manual)
    {
        Id = id;
        Done = done;
        Have = have;
        Need = need;
        Manual = manual;
    }

    /// <summary>The step's id.</summary>
    public string Id { get; }

    /// <summary>Ticked: every part checked, or ticked by hand.</summary>
    public bool Done { get; }

    /// <summary>The parts checked.</summary>
    public int Have { get; }

    /// <summary>The parts this case has (at least 1; a step with none is not listed).</summary>
    public int Need { get; }

    /// <summary>Ticked or unticked by hand: it holds for the rest of the case.</summary>
    public bool Manual { get; }
}

/// <summary>What a click on a step does.</summary>
public enum StepTargetKind
{
    /// <summary>Nothing.</summary>
    None,

    /// <summary>A toast with a hint (StepTarget.HintKey).</summary>
    Hint,

    /// <summary>The app shows a tab.</summary>
    Tab,

    /// <summary>Records, with a record looked up (StepTarget.Query).</summary>
    Record,

    /// <summary>Reference, with a book chosen (StepTarget.Book).</summary>
    Book
}

/// <summary>Where a click on a step goes, in the active pane (the PC redesign ST4).</summary>
public readonly struct StepTarget
{
    private StepTarget(StepTargetKind kind, AppTab tab, string query, ClueCategory book, string hintKey)
    {
        Kind = kind;
        Tab = tab;
        Query = query;
        Book = book;
        HintKey = hintKey;
    }

    /// <summary>What the click does.</summary>
    public StepTargetKind Kind { get; }

    /// <summary>The tab shown (Records for a Record target, Reference for a Book).</summary>
    public AppTab Tab { get; }

    /// <summary>A Record target's lookup (a number or a name).</summary>
    public string Query { get; }

    /// <summary>A Book target's book, by its category.</summary>
    public ClueCategory Book { get; }

    /// <summary>A Hint target's UI string key.</summary>
    public string HintKey { get; }

    /// <summary>Nothing to do.</summary>
    public static StepTarget Nothing => default;

    /// <summary>A toast with the hint <paramref name="key"/>.</summary>
    public static StepTarget ForHint(string key) => new StepTarget(StepTargetKind.Hint, default, null, default, key);

    /// <summary>The tab.</summary>
    public static StepTarget ForTab(AppTab tab) => new StepTarget(StepTargetKind.Tab, tab, null, default, null);

    /// <summary>Records, with <paramref name="query"/> looked up.</summary>
    public static StepTarget ForRecord(string query) => new StepTarget(StepTargetKind.Record, AppTab.Records, query, default, null);

    /// <summary>Reference, on the book of <paramref name="category"/>.</summary>
    public static StepTarget ForBook(ClueCategory category) => new StepTarget(StepTargetKind.Book, AppTab.Reference, null, category, null);
}

/// <summary>
/// The optional steps checklist's rules (the PC redesign ST1-ST4, §4.4;
/// redesign phase 21): the set a traveller gets (their kind's, inheriting and
/// overriding by id; the default for a kind without one; a step from its first
/// day), each step's parts and tick from what the player has done this case
/// (CaseProgress; a hand-set tick holds), and where a click goes. A step ticks
/// when the player made the check, never on what the check found, so the list
/// gives no answer away. Pure; the Investigation app's StepsPanel draws it.
/// </summary>
public static class CaseSteps
{
    /// <summary>The type of the set a traveller kind without its own set gets.</summary>
    public const string DefaultType = "default";

    /// <summary>
    /// The steps a traveller of <paramref name="type"/> (a TravellerKind name)
    /// gets on <paramref name="day"/>: their set's (the default's when it has
    /// none), starting from its inherited sets' (the root first), a step
    /// replacing the one of the same id in place and a new id appended; a step
    /// before its first day is left out. An inherit cycle stops where it repeats.
    /// </summary>
    public static IReadOnlyList<StepSpec> Resolve(StepSetData data, string type, int day)
    {
        var steps = new List<StepSpec>();
        StepSet set = Find(data, type) ?? Find(data, DefaultType);
        var chain = new List<StepSet>();
        for (StepSet s = set; s != null && !chain.Contains(s); s = string.IsNullOrWhiteSpace(s.inherit) ? null : Find(data, s.inherit))
            chain.Insert(0, s);

        foreach (StepSet s in chain)
            foreach (StepSpec step in s.steps ?? new List<StepSpec>())
            {
                if (step == null)
                    continue;
                int at = steps.FindIndex(x => x.id == step.id);
                if (at >= 0)
                    steps[at] = step;
                else
                    steps.Add(step);
            }

        steps.RemoveAll(s => s.fromDay > day);
        return steps;
    }

    /// <summary>
    /// Each listed step's state this case (a step with no parts is left out):
    /// its parts checked and needed, ticked when all are (or as set by hand,
    /// which holds).
    /// </summary>
    public static List<StepState> Evaluate(IReadOnlyList<StepSpec> steps, CaseProgress progress)
    {
        var states = new List<StepState>();
        foreach (StepSpec step in steps)
        {
            Count(step, progress, out int have, out int need);
            if (need == 0)
                continue;
            bool manual = progress.TryManual(step.id, out bool ticked);
            states.Add(new StepState(step.id, manual ? ticked : have >= need, have, need, manual));
        }
        return states;
    }

    /// <summary>
    /// The categories a Compared or Asked step counts for this case, in order
    /// (none for another step). Asked: the named ones among today's question
    /// categories, or all of them. Compared: the named ones (or, none named,
    /// all) that the case can state as the step's statement (the papers'
    /// categories, today's question categories and those answered, Culture for
    /// a garment); against the reference only those a book holds. An unknown
    /// name counts nothing.
    /// </summary>
    public static IReadOnlyList<ClueCategory> Categories(StepSpec step, CaseProgress progress)
    {
        var result = new List<ClueCategory>();
        List<ClueCategory> named = Named(step);
        if (step.when == StepWhen.Asked)
        {
            foreach (ClueCategory c in named ?? new List<ClueCategory>(progress.QuestionCategories))
                if (Contains(progress.QuestionCategories, c) && !result.Contains(c))
                    result.Add(c);
            return result;
        }
        if (step.when != StepWhen.Compared)
            return result;

        List<ClueCategory> stated = Stated(step.statement, progress);
        foreach (ClueCategory c in named ?? stated)
            if (stated.Contains(c) && (step.truth != TruthKind.Reference || progress.IsBookCategory(c)) && !result.Contains(c))
                result.Add(c);
        return result;
    }

    /// <summary>
    /// Where a click on <paramref name="step"/> goes: its hint (a toast), its
    /// tab, the primary paper's record in Records (by its Citizen ID, else its
    /// name; Records alone without a paper), the Costume Guide, or the first
    /// paper field of its categories not compared yet as the step compares it
    /// (its book; all compared: the first category's book; a Record step: the
    /// primary record; a Paper step: Documents).
    /// </summary>
    public static StepTarget Target(StepSpec step, CaseProgress progress)
    {
        if (!string.IsNullOrWhiteSpace(step.hint))
            return StepTarget.ForHint(step.hint);

        switch (step.link)
        {
            case StepLink.Tab:
                return StepTarget.ForTab(step.tab);
            case StepLink.PrimaryName:
                return PrimaryRecord(progress);
            case StepLink.CostumeClaimed:
                return StepTarget.ForBook(ClueCategory.Culture);
            case StepLink.FirstUncheckedField:
                if (step.truth == TruthKind.Record)
                    return PrimaryRecord(progress);
                if (step.truth == TruthKind.Paper)
                    return StepTarget.ForTab(AppTab.Documents);
                IReadOnlyList<ClueCategory> categories = Categories(step, progress);
                foreach (StepPaper paper in progress.Papers)
                    foreach (DocumentField field in paper.Fields)
                        if (field != null && Contains(categories, field.category) && !progress.WasCompared(step.statement, field.category, step.truth))
                            return StepTarget.ForBook(field.category);
                return categories.Count > 0 ? StepTarget.ForBook(categories[0]) : StepTarget.ForTab(AppTab.Reference);
            default:
                return StepTarget.Nothing;
        }
    }

    /// <summary>
    /// What a compared pair checked, whichever side was picked first: its
    /// statement (a paper's field, an answer, a garment), its one category and
    /// its truth (a book row, a record row, or another paper's field for a
    /// statement against a paper). False for a pair that checks nothing: a
    /// side without evidence, two categories, two truths, or two statements
    /// neither of which is a paper's field.
    /// </summary>
    public static bool Classify(CompareEvidence a, CompareEvidence b, out StatementKind statement, out ClueCategory category, out TruthKind truth)
    {
        statement = StatementKind.Any;
        category = a.category;
        truth = TruthKind.Any;
        if (a.kind == EvidenceKind.None || b.kind == EvidenceKind.None || a.category != b.category)
            return false;

        StatementKind sa = StatementOf(a.kind), sb = StatementOf(b.kind);
        TruthKind ta = TruthOf(a.kind), tb = TruthOf(b.kind);
        if (sa != StatementKind.Any && tb != TruthKind.Any)
        {
            statement = sa;
            truth = tb;
            return true;
        }
        if (sb != StatementKind.Any && ta != TruthKind.Any)
        {
            statement = sb;
            truth = ta;
            return true;
        }
        if (sa == StatementKind.Any || sb == StatementKind.Any || (sa != StatementKind.Field && sb != StatementKind.Field))
            return false;

        statement = sa != StatementKind.Field ? sa : sb;
        truth = TruthKind.Paper;
        return true;
    }

    // ---- the parts ----

    private static void Count(StepSpec step, CaseProgress p, out int have, out int need)
    {
        have = 0;
        need = 0;
        switch (step.when)
        {
            case StepWhen.PapersReceived:
            case StepWhen.PaperRead:
                for (int i = 0; i < p.Papers.Count; i++)
                    if (OfForms(step, p.Papers[i]))
                    {
                        need++;
                        if (step.when == StepWhen.PapersReceived ? p.HasReceived(i) : p.HasRead(i))
                            have++;
                    }
                return;
            case StepWhen.Requested:
                if (step.forms != null && step.forms.Count > 0)
                {
                    need = 1;
                    have = step.forms.Exists(p.WasRequested) ? 1 : 0;
                    return;
                }
                foreach (StepPaper paper in p.Papers)
                    if (paper.OnRequest)
                    {
                        need++;
                        if (p.WasRequested(paper.Kind))
                            have++;
                    }
                return;
            case StepWhen.RulesViewed:
                Single(p.ViewedRules, out have, out need);
                return;
            case StepWhen.RecordViewed:
                Single(p.ViewedRecord, out have, out need);
                return;
            case StepWhen.LookedAt:
                Single(p.Looked, out have, out need);
                return;
            case StepWhen.Compared:
            case StepWhen.Asked:
                foreach (ClueCategory c in Categories(step, p))
                {
                    need++;
                    if (step.when == StepWhen.Asked ? p.WasAsked(c) : p.WasCompared(step.statement, c, step.truth))
                        have++;
                }
                return;
        }
    }

    private static void Single(bool done, out int have, out int need)
    {
        have = done ? 1 : 0;
        need = 1;
    }

    private static bool OfForms(StepSpec step, StepPaper paper) =>
        step.forms == null || step.forms.Count == 0 || step.forms.Contains(paper.Kind);

    /// <summary>The categories a statement of <paramref name="kind"/> can be in this case.</summary>
    private static List<ClueCategory> Stated(StatementKind kind, CaseProgress p)
    {
        var stated = new List<ClueCategory>();
        if (kind == StatementKind.Any || kind == StatementKind.Field)
            AddAll(stated, p.PaperCategories);
        if (kind == StatementKind.Any || kind == StatementKind.Answer)
        {
            AddAll(stated, p.QuestionCategories);
            AddAll(stated, p.AnsweredCategories);
        }
        if ((kind == StatementKind.Any || kind == StatementKind.Garment) && !stated.Contains(ClueCategory.Culture))
            stated.Add(ClueCategory.Culture);
        return stated;
    }

    /// <summary>The step's named categories that exist (null when it names none: derived).</summary>
    private static List<ClueCategory> Named(StepSpec step)
    {
        if (step.categories == null || step.categories.Count == 0)
            return null;
        var named = new List<ClueCategory>();
        foreach (string name in step.categories)
            if (StepSets.TryCategory(name, out ClueCategory c))
                named.Add(c);
        return named;
    }

    private static StepTarget PrimaryRecord(CaseProgress p)
    {
        StepPaper primary = null;
        foreach (StepPaper paper in p.Papers)
            if (paper.Primary)
            {
                primary = paper;
                break;
            }
        if (primary == null && p.Papers.Count > 0)
            primary = p.Papers[0];

        string query = ValueOf(primary, ClueCategory.CitizenId);
        if (string.IsNullOrWhiteSpace(query))
            query = ValueOf(primary, ClueCategory.Name);
        return string.IsNullOrWhiteSpace(query) ? StepTarget.ForTab(AppTab.Records) : StepTarget.ForRecord(query.Trim());
    }

    private static string ValueOf(StepPaper paper, ClueCategory category)
    {
        if (paper != null)
            foreach (DocumentField f in paper.Fields)
                if (f != null && f.category == category)
                    return f.value;
        return null;
    }

    private static StatementKind StatementOf(EvidenceKind kind) =>
        kind == EvidenceKind.DocumentField ? StatementKind.Field
        : kind == EvidenceKind.Answer ? StatementKind.Answer
        : kind == EvidenceKind.Appearance ? StatementKind.Garment
        : StatementKind.Any;

    private static TruthKind TruthOf(EvidenceKind kind) =>
        kind == EvidenceKind.ReferenceEntry ? TruthKind.Reference
        : kind == EvidenceKind.RecordField ? TruthKind.Record
        : TruthKind.Any;

    private static StepSet Find(StepSetData data, string type)
    {
        if (data?.sets == null || string.IsNullOrWhiteSpace(type))
            return null;
        return data.sets.Find(s => s != null && string.Equals(s.type, type.Trim(), StringComparison.Ordinal));
    }

    private static bool Contains(IReadOnlyList<ClueCategory> list, ClueCategory c)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == c)
                return true;
        return false;
    }

    private static void AddAll(List<ClueCategory> into, IReadOnlyList<ClueCategory> from)
    {
        foreach (ClueCategory c in from)
            if (!into.Contains(c))
                into.Add(c);
    }
}
