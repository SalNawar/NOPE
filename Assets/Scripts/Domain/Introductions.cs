using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The keys of everything the day ramp introduces (the desk-first redesign,
/// Saleh 2026-10-05, items 2, 9, 10: "app features hidden until their rule is
/// unlocked"; Papers Please lessons 4 and D7). A key is "kind:id": a paper
/// (<see cref="Paper"/>, a form number of days[].papers), a directive
/// (<see cref="Rule"/>, a rule asset of days[].rules), a lie (<see cref="Lie"/>,
/// a LieKind of days[].lies), a question (<see cref="Question"/>, a
/// questions[].id at its fromDay), or one of the named desk tools, wheel
/// entries, PC apps, menus, reference books and document fields below, listed
/// in days[].introduces on the day they arrive. Pure.
/// </summary>
public static class Feature
{
    /// <summary>A paper's key: "paper:" and its form number (TC-101 the Passport).</summary>
    public static string Paper(string formNumber) => "paper:" + formNumber;

    /// <summary>A directive's key: "rule:" and its rule asset (Rule_PaperDates).</summary>
    public static string Rule(string asset) => "rule:" + asset;

    /// <summary>A lie's key: "lie:" and its LieKind name.</summary>
    public static string Lie(LieKind lie) => "lie:" + lie;

    /// <summary>A wheel question's key: "question:" and its questions[].id (q_currency).</summary>
    public static string Question(string id) => "question:" + id;

    /// <summary>A PC app's key: "app:" and its DesktopAppIds id.</summary>
    public static string App(string appId) => "app:" + appId;

    /// <summary>A document field's key on every form: "field:" and its ClueCategory name (field:TransponderClass).</summary>
    public static string Field(ClueCategory category) => "field:" + category;

    /// <summary>A document field's key on one form only: "field:", the form number, "/" and the category (field:TC-620/Currency).</summary>
    public static string Field(string formNumber, ClueCategory category) => "field:" + formNumber + "/" + category;

    /// <summary>A reference book's key: "book:" and the ClueCategory the book lists (book:Seal, book:Culture).</summary>
    public static string Book(ClueCategory category) => "book:" + category;

    // ---- Desk tools ----

    /// <summary>The scanner and what it feeds (the copy on the PC; day 5 of the ramp).</summary>
    public const string Scanner = "tool:scanner";

    /// <summary>The Departure Board's open destinations (the second destination, day 3).</summary>
    public const string Board = "tool:board";

    /// <summary>The calendar (today's date, against expiry and departure dates): from day 1, on the desk and the PC (Saleh 2026-10-05).</summary>
    public const string Calendar = "tool:calendar";

    /// <summary>The rulebook: today's Directive Memo (the open destinations, from day 1).</summary>
    public const string Rulebook = "tool:rulebook";

    // ---- Wheel entries (the requests follow the papers, the questions their own keys) ----

    /// <summary>The Look menu's garments (their clothes, against the Costume Guide; with the dress rule). Their face shows from day 1.</summary>
    public const string Clothes = "wheel:clothes";

    // ---- PC ----

    /// <summary>The traveller's citizen records on the PC (the Records chip; day 5 with the scanner).</summary>
    public const string Records = "pc:records";

    /// <summary>The Citizen Account's Standing row (the debt standing's day).</summary>
    public const string Standing = "pc:standing";
}

/// <summary>
/// The one introduction registry (the desk-first redesign, items 2, 9, 10):
/// for any day, which papers, rules, lies, questions, desk tools, wheel
/// entries, PC apps, menus, books and document fields have been introduced.
/// Built once from the day plans (papers, rules, lies, introduces; the
/// questions' fromDay), so the ramp has one source: world_source.json days[].
/// A feature is introduced on the first day any plan lists it and stays
/// introduced (nothing is withdrawn); a feature no day lists is never
/// introduced, so it stays hidden. Tracks read it through
/// <see cref="Has"/> and <see cref="FirstDay"/>. Pure; tested headless.
/// </summary>
public sealed class Introductions
{
    /// <summary>No day introduces anything (an empty library).</summary>
    public static readonly Introductions None = new Introductions(Array.Empty<(int, IEnumerable<string>)>());

    private readonly Dictionary<string, int> _first = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>True for the cheat menu's "unlock everything" copy (<see cref="WithEverything"/>): every known key counts as introduced on any day.</summary>
    private readonly bool _everything;

    /// <summary>The registry over <paramref name="days"/>: each day's number and every key it lists (null days and keys count as none).</summary>
    public Introductions(IEnumerable<(int day, IEnumerable<string> keys)> days)
    {
        foreach ((int day, IEnumerable<string> keys) in days ?? Array.Empty<(int, IEnumerable<string>)>())
            foreach (string key in keys ?? Enumerable.Empty<string>())
                if (!string.IsNullOrWhiteSpace(key) && (!_first.TryGetValue(key, out int first) || day < first))
                    _first[key] = day;
    }

    /// <summary>A copy of <paramref name="source"/>'s keys and first days, every one of them introduced on any day when <paramref name="everything"/> (WithEverything).</summary>
    private Introductions(Introductions source, bool everything)
    {
        foreach (KeyValuePair<string, int> pair in source._first)
            _first[pair.Key] = pair.Value;
        _everything = everything;
    }

    /// <summary>
    /// The cheat menu's "unlock everything" (Saleh 2026-10-06: "unlock
    /// everything for today (all Introductions, apps, scanner, books)"): the
    /// same registry, where every key some day lists counts as introduced on
    /// any day (<see cref="Has"/>, <see cref="ShowsField"/>), while
    /// <see cref="FirstDay"/>, <see cref="IsNew"/> and <see cref="NewOn"/> keep
    /// the ramp's days (the guide's pages and the bulletins stay the day's own).
    /// A key no day lists stays hidden. ContentLibrarySO hands it out while
    /// DevToolsState.UnlockEverything is on.
    /// </summary>
    public Introductions WithEverything() => new Introductions(this, true);

    /// <summary>The day <paramref name="feature"/> is introduced; 0 when no day introduces it.</summary>
    public int FirstDay(string feature) => feature != null && _first.TryGetValue(feature, out int day) ? day : 0;

    /// <summary>True when <paramref name="feature"/> is introduced on or before <paramref name="day"/>.</summary>
    public bool Has(int day, string feature)
    {
        int first = FirstDay(feature);
        return first > 0 && (_everything || day >= first);
    }

    /// <summary>True when <paramref name="feature"/> is introduced exactly on <paramref name="day"/> (today's new things).</summary>
    public bool IsNew(int day, string feature) => FirstDay(feature) == day && day > 0;

    /// <summary>
    /// True when a field of <paramref name="category"/> on the form
    /// <paramref name="formNumber"/> shows on <paramref name="day"/>: its
    /// category's key or the form's own key (<see cref="Feature.Field(string, ClueCategory)"/>)
    /// is introduced. A hidden field draws nothing, is never picked and is
    /// never forged (the desk-first redesign, item 3).
    /// </summary>
    public bool ShowsField(int day, string formNumber, ClueCategory category) =>
        Has(day, Feature.Field(category)) || Has(day, Feature.Field(formNumber, category));

    /// <summary>Every key introduced exactly on <paramref name="day"/>, in key order (today's new things: the bulletin names them).</summary>
    public List<string> NewOn(int day) => _first.Where(p => p.Value == day).Select(p => p.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();

    /// <summary>Every key the registry knows, in key order.</summary>
    public IReadOnlyCollection<string> Keys => _first.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    /// <summary>
    /// One day's keys from its content: its papers (<see cref="Feature.Paper"/>),
    /// its rules (<see cref="Feature.Rule"/>), its lies (<see cref="Feature.Lie"/>),
    /// the questions whose fromDay is that day (<see cref="Feature.Question"/>)
    /// and its own introduces list (tools, wheel entries, apps, menus, books,
    /// fields: the keys as written).
    /// </summary>
    public static IEnumerable<string> DayKeys(IEnumerable<string> papers, IEnumerable<string> rules, IEnumerable<LieKind> lies,
                                              IEnumerable<string> questions, IEnumerable<string> introduces)
    {
        foreach (string paper in papers ?? Enumerable.Empty<string>())
            if (!string.IsNullOrWhiteSpace(paper))
                yield return Feature.Paper(paper);
        foreach (string rule in rules ?? Enumerable.Empty<string>())
            if (!string.IsNullOrWhiteSpace(rule))
                yield return Feature.Rule(rule);
        foreach (LieKind lie in lies ?? Enumerable.Empty<LieKind>())
            yield return Feature.Lie(lie);
        foreach (string question in questions ?? Enumerable.Empty<string>())
            if (!string.IsNullOrWhiteSpace(question))
                yield return Feature.Question(question);
        foreach (string key in introduces ?? Enumerable.Empty<string>())
            if (!string.IsNullOrWhiteSpace(key))
                yield return key.Trim();
    }

    /// <summary>
    /// The named keys a day's introduces list may hold: every desk tool, wheel
    /// entry and PC key of <see cref="Feature"/>, an app of
    /// <see cref="DesktopAppIds.DefaultOrder"/>, a book of any ClueCategory, or
    /// a field of any ClueCategory, on every form or on one ("field:TC-620/Currency";
    /// the form number is not checked here). Generate World and the validator refuse any other key, in
    /// the same words (<see cref="Problems"/>).
    /// </summary>
    public static bool IsNamedKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return false;
        if (Named.Contains(key))
            return true;
        if (key.StartsWith("app:", StringComparison.Ordinal))
            return DesktopAppIds.DefaultOrder.Contains(key.Substring(4));
        if (key.StartsWith("book:", StringComparison.Ordinal))
            return IsCategory(key.Substring(5));
        if (key.StartsWith("field:", StringComparison.Ordinal))
        {
            string field = key.Substring(6);
            int slash = field.IndexOf('/');
            if (slash == 0)
                return false;
            return IsCategory(slash < 0 ? field : field.Substring(slash + 1));
        }
        return false;
    }

    /// <summary>True for a ClueCategory's name (never a number).</summary>
    private static bool IsCategory(string name) => !string.IsNullOrEmpty(name) && !int.TryParse(name, out _) && Enum.TryParse(name, false, out ClueCategory _);

    /// <summary>The fixed named keys (tools, wheel entries, PC).</summary>
    private static readonly HashSet<string> Named = new HashSet<string>(StringComparer.Ordinal)
    {
        Feature.Scanner, Feature.Board, Feature.Calendar, Feature.Rulebook, Feature.Clothes, Feature.Records, Feature.Standing
    };

    /// <summary>One message per key of a day's introduces list that is not a named key (<see cref="IsNamedKey"/>) or is listed twice.</summary>
    public static List<string> Problems(string asset, IReadOnlyList<string> introduces)
    {
        var problems = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string key in introduces ?? Array.Empty<string>())
        {
            if (!IsNamedKey(key))
                problems.Add($"Day '{asset}' introduces '{key}', which is no feature key (a tool:, wheel:, pc: key of Feature, app:<desktop app id>, book:<ClueCategory> or field:<ClueCategory>).");
            else if (!seen.Add(key))
                problems.Add($"Day '{asset}' introduces '{key}' twice.");
        }
        return problems;
    }
}
