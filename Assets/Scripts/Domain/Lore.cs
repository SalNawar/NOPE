using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// The citizen file's content (the scanner app spec §3; Saleh: "in citizen
/// lookup we need to have more lore and storytelling"): world_source.json
/// "lore", written by Generate World. Each premade's authored registry notes
/// (real, well-established facts about a famous traveller, phrased as a 2150
/// registry notes them; a story character's own lines); the random
/// travellers' templates, slot-filled by kind, personality and traits, a few
/// of them clues that agree with the case's real fault; the threads a
/// recurring traveller's file grows, one per earlier verdict.
/// </summary>
[Serializable]
public sealed class LoreContent
{
    /// <summary>The longest line a file prints (characters, after filling): one line of the record's box.</summary>
    public const int MaxLineLength = 140;

    /// <summary>The chance a random traveller with a fault gets one clue line among their flavour (0 to 1).</summary>
    public float clueChance = 0.35f;

    /// <summary>The fewest flavour lines a random traveller's file holds.</summary>
    public int linesMin = 2;

    /// <summary>The most flavour lines a random traveller's file holds.</summary>
    public int linesMax = 3;

    /// <summary>The earliest year a drawn {year} or {month} names.</summary>
    public int yearMin = 2140;

    /// <summary>The latest year a drawn {year} or {month} names.</summary>
    public int yearMax = 2149;

    /// <summary>The kin a drawn {relative} names ("sister").</summary>
    public List<string> relatives = new List<string>();

    /// <summary>Each premade's authored lines.</summary>
    public List<LorePremade> premades = new List<LorePremade>();

    /// <summary>The random travellers' templates.</summary>
    public List<LoreTemplate> templates = new List<LoreTemplate>();

    /// <summary>The lines a recurring traveller's file grows, by verdict.</summary>
    public List<LoreThread> threads = new List<LoreThread>();

    /// <summary>The authored lines of premade <paramref name="id"/>, or null.</summary>
    public LorePremade Premade(string id) =>
        string.IsNullOrEmpty(id) ? null : premades?.FirstOrDefault(p => p != null && p.premade == id);
}

/// <summary>One premade's authored file lines (lore.premades).</summary>
[Serializable]
public sealed class LorePremade
{
    /// <summary>The premade's id.</summary>
    public string premade = string.Empty;

    /// <summary>Its lines, in order (a famous traveller's: registry notes of real facts; contested claims as "Archive sources claim ...").</summary>
    public List<string> lines = new List<string>();
}

/// <summary>One random traveller's template (lore.templates): who it may describe and its text with {slots}.</summary>
[Serializable]
public sealed class LoreTemplate
{
    /// <summary>Its id (unique).</summary>
    public string id = string.Empty;

    /// <summary>The kinds it describes (empty: any).</summary>
    public List<TravellerKind> kinds = new List<TravellerKind>();

    /// <summary>The personality it describes (blank: any).</summary>
    public string personality = string.Empty;

    /// <summary>The traits the traveller must have (LoreTraits: debt, nodebt, frozen, good, trips, notrips, premium, standard, eligible).</summary>
    public List<string> traits = new List<string>();

    /// <summary>The fault it is a clue to (a LieKind or a DirectiveFault name; blank: flavour): picked only for a traveller who carries it.</summary>
    public string clue = string.Empty;

    /// <summary>The line, with {slots} (LoreSlots).</summary>
    public string text = string.Empty;
}

/// <summary>One thread line (lore.threads): what a recurring traveller's file says of an earlier visit.</summary>
[Serializable]
public sealed class LoreThread
{
    /// <summary>Its id (unique).</summary>
    public string id = string.Empty;

    /// <summary>The verdict word it follows ("Denied"; blank: any verdict without a line of its own).</summary>
    public string verdict = string.Empty;

    /// <summary>The line: {date}, {place}, {verdict} (lower case), {days} (days since) and the universal slots.</summary>
    public string text = string.Empty;
}

/// <summary>
/// Whom a file is about: the case's registered identity and the values its
/// slots read (a liar's cover, never their true home), and their fault (a
/// clue line must agree with it).
/// </summary>
public sealed class LoreSubject
{
    /// <summary>The traveller's kind.</summary>
    public TravellerKind Kind;

    /// <summary>The premade's id (blank: a random traveller).</summary>
    public string Premade = string.Empty;

    /// <summary>The personality's id (blank: none).</summary>
    public string Personality = string.Empty;

    /// <summary>The registered name.</summary>
    public string Name = string.Empty;

    /// <summary>The claimed place's label.</summary>
    public string Place = string.Empty;

    /// <summary>The claimed era's display name.</summary>
    public string Era = string.Empty;

    /// <summary>Their role ("Merchant"; blank: none).</summary>
    public string Role = string.Empty;

    /// <summary>A citizen's debt in cr.</summary>
    public int Debt;

    /// <summary>A citizen's account status word ("Standard"; blank for the displaced).</summary>
    public string Status = string.Empty;

    /// <summary>True when a citizen's account is frozen in default.</summary>
    public bool Frozen;

    /// <summary>A citizen's past trips on file.</summary>
    public int Trips;

    /// <summary>A labourer's registered employer.</summary>
    public string Employer = string.Empty;

    /// <summary>A labourer's registered day wage ("420 cr").</summary>
    public string Wage = string.Empty;

    /// <summary>A labourer's registered term ("180 days").</summary>
    public string Term = string.Empty;

    /// <summary>A citizen's transponder model.</summary>
    public string Transponder = string.Empty;

    /// <summary>A displaced person's incident.</summary>
    public string Incident = string.Empty;

    /// <summary>A displaced person's found date.</summary>
    public string Found = string.Empty;

    /// <summary>Their directive fault (None: none).</summary>
    public DirectiveFault Fault;

    /// <summary>The lie they carry (null: none).</summary>
    public LieKind? Lie;

    /// <summary>True for a 2150 citizen (every kind but the displaced).</summary>
    public bool IsCitizen => Kind != TravellerKind.Displaced;

    /// <summary>The faults a clue may name: the directive fault's name and the lie's name.</summary>
    public IEnumerable<string> ClueKeys()
    {
        if (Fault != DirectiveFault.None)
            yield return Fault.ToString();
        if (Lie.HasValue)
            yield return Lie.Value.ToString();
    }
}

/// <summary>The traits a template may require (LoreTemplate.traits).</summary>
public static class LoreTraits
{
    /// <summary>Every trait.</summary>
    public static readonly string[] All = { "debt", "nodebt", "frozen", "good", "trips", "notrips", "premium", "standard", "eligible" };

    /// <summary>True when <paramref name="subject"/> has <paramref name="trait"/> (an unknown trait never holds).</summary>
    public static bool Holds(string trait, LoreSubject subject)
    {
        switch ((trait ?? string.Empty).Trim().ToLowerInvariant())
        {
            case "debt": return subject.IsCitizen && subject.Debt > 0;
            case "nodebt": return subject.IsCitizen && subject.Debt <= 0;
            case "frozen": return subject.IsCitizen && subject.Frozen;
            case "good": return subject.IsCitizen && !subject.Frozen;
            case "trips": return subject.IsCitizen && subject.Trips > 0;
            case "notrips": return subject.IsCitizen && subject.Trips <= 0;
            case "premium": return subject.IsCitizen && Is(subject.Status, "Premium");
            case "standard": return subject.IsCitizen && Is(subject.Status, "Standard");
            case "eligible": return subject.IsCitizen && Is(subject.Status, "Eligible");
            default: return false;
        }
    }

    private static bool Is(string a, string b) => string.Equals((a ?? string.Empty).Trim(), b, StringComparison.OrdinalIgnoreCase);
}

/// <summary>The {slots} a file line may hold, and which a template may use (its kinds and traits guarantee their values).</summary>
public static class LoreSlots
{
    /// <summary>Slots every traveller fills: the name, the first name, the claimed place and era, the role, and the drawn year, month (2149-03), count (a word), sector and relative.</summary>
    public static readonly string[] Universal = { "name", "first", "place", "era", "role", "year", "month", "count", "sector", "relative" };

    /// <summary>A 2150 citizen's: the account's status, past trips and transponder.</summary>
    public static readonly string[] Citizen = { "status", "trips", "transponder" };

    /// <summary>A debtor's: the debt.</summary>
    public static readonly string[] Debtor = { "debt" };

    /// <summary>A labourer's registered contract: employer, wage and term.</summary>
    public static readonly string[] Labourer = { "employer", "wage", "term" };

    /// <summary>A displaced person's agency file: incident and found date.</summary>
    public static readonly string[] Displaced = { "incident", "found" };

    /// <summary>A thread's: the earlier visit's date, place, verdict and the days since.</summary>
    public static readonly string[] Thread = { "date", "verdict", "days" };

    private static readonly Regex SlotPattern = new Regex(@"\{([^{}]*)\}", RegexOptions.CultureInvariant);

    /// <summary>The {slots} a text names, in order (repeats kept).</summary>
    public static List<string> In(string text) =>
        SlotPattern.Matches(text ?? string.Empty).Cast<Match>().Select(m => m.Groups[1].Value).ToList();

    /// <summary>The slots a template for <paramref name="kinds"/> (empty: any) with <paramref name="traits"/> may use.</summary>
    public static HashSet<string> For(IReadOnlyCollection<TravellerKind> kinds, IEnumerable<string> traits)
    {
        var slots = new HashSet<string>(Universal);
        bool any = kinds == null || kinds.Count == 0;
        if (!any && kinds.All(k => k != TravellerKind.Displaced))
            slots.UnionWith(Citizen);
        if (!any && kinds.All(k => k == TravellerKind.Labourer))
            slots.UnionWith(Labourer);
        if (!any && kinds.All(k => k == TravellerKind.Displaced))
            slots.UnionWith(Displaced);
        if (traits != null && traits.Any(t => string.Equals(t, "debt", StringComparison.OrdinalIgnoreCase)))
            slots.UnionWith(Debtor);
        return slots;
    }
}

/// <summary>
/// The citizen file (the scanner app spec §3), pure and seeded: a premade's
/// authored lines, or a random traveller's flavour lines drawn from the
/// templates their kind, personality and traits fit, one of them a clue
/// that agrees with their real fault at the content's chance; then a thread
/// line for each of their two latest earlier visits. Every draw is a value of
/// the file's seed (Seeds.ForLore of the traveller's first visit), so a
/// replay and a return print the same lines, and nothing else draws on it.
/// </summary>
public static class CitizenFile
{
    /// <summary>The most earlier visits a file tells (the latest ones).</summary>
    public const int ThreadsShown = 2;

    /// <summary>
    /// The file of <paramref name="subject"/> from <paramref name="content"/>
    /// (null: none), on <paramref name="seed"/>, growing a line per earlier
    /// visit in <paramref name="earlier"/> (oldest first; the latest
    /// <see cref="ThreadsShown"/>) as of <paramref name="today"/>.
    /// </summary>
    public static List<string> Lines(LoreContent content, LoreSubject subject, int seed, IReadOnlyList<VisitEntry> earlier, int today)
    {
        var lines = new List<string>();
        if (content == null || subject == null)
            return lines;

        LorePremade authored = content.Premade(subject.Premade);
        if (authored != null)
            lines.AddRange((authored.lines ?? new List<string>()).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Trim()));
        else if (string.IsNullOrEmpty(subject.Premade))
            lines.AddRange(Flavour(content, subject, seed));

        if (earlier != null)
        {
            List<VisitEntry> told = earlier.Where(v => v != null && v.day < today).OrderBy(v => v.day).ToList();
            foreach (VisitEntry visit in told.Skip(Math.Max(0, told.Count - ThreadsShown)))
            {
                LoreThread thread = ThreadFor(content, visit.verdict, Seeds.Mix(seed, visit.day));
                if (thread != null)
                    lines.Add(Fill(content, thread.text, subject, Seeds.Mix(seed, 1000 + visit.day), visit, today));
            }
        }
        return lines;
    }

    /// <summary>True when <paramref name="template"/> may describe <paramref name="subject"/> as flavour (no clue) or as a clue (its clue is one of theirs).</summary>
    public static bool Fits(LoreTemplate template, LoreSubject subject, bool clue)
    {
        if (template == null || string.IsNullOrWhiteSpace(template.text))
            return false;
        if (template.kinds != null && template.kinds.Count > 0 && !template.kinds.Contains(subject.Kind))
            return false;
        if (!string.IsNullOrWhiteSpace(template.personality) && template.personality != subject.Personality)
            return false;
        if (template.traits != null && template.traits.Any(t => !LoreTraits.Holds(t, subject)))
            return false;
        bool isClue = !string.IsNullOrWhiteSpace(template.clue);
        if (isClue != clue)
            return false;
        return !clue || subject.ClueKeys().Contains(template.clue.Trim());
    }

    /// <summary>A random traveller's flavour lines: linesMin to linesMax distinct fitting templates, one replaced by a fitting clue at the clue chance.</summary>
    private static List<string> Flavour(LoreContent content, LoreSubject subject, int seed)
    {
        var rng = new SeededRandom(seed);
        int min = Math.Max(0, content.linesMin), max = Math.Max(min, content.linesMax);
        int count = min + rng.Range(0, max - min + 1);
        List<LoreTemplate> pool = (content.templates ?? new List<LoreTemplate>()).Where(t => Fits(t, subject, false)).ToList();
        var picked = new List<LoreTemplate>();
        while (picked.Count < count && pool.Count > 0)
        {
            int i = rng.Range(0, pool.Count);
            picked.Add(pool[i]);
            pool.RemoveAt(i);
        }

        List<LoreTemplate> clues = (content.templates ?? new List<LoreTemplate>()).Where(t => Fits(t, subject, true)).ToList();
        bool clue = clues.Count > 0 && rng.Value() < content.clueChance;
        int clueAt = rng.Range(0, Math.Max(1, picked.Count));
        LoreTemplate clueTemplate = clues.Count > 0 ? clues[rng.Range(0, clues.Count)] : null;
        if (clue)
        {
            if (picked.Count == 0)
                picked.Add(clueTemplate);
            else
                picked[clueAt] = clueTemplate;
        }

        var lines = new List<string>(picked.Count);
        for (int n = 0; n < picked.Count; n++)
            lines.Add(Fill(content, picked[n].text, subject, Seeds.Mix(seed, n + 1), null, 0));
        return lines;
    }

    /// <summary>The thread for <paramref name="verdict"/>: one of the lines of that verdict (ignoring case), else of a blank verdict, picked by <paramref name="value"/>; null when none.</summary>
    private static LoreThread ThreadFor(LoreContent content, string verdict, int value)
    {
        List<LoreThread> all = (content.threads ?? new List<LoreThread>()).Where(t => t != null && !string.IsNullOrWhiteSpace(t.text)).ToList();
        List<LoreThread> own = all.Where(t => string.Equals((t.verdict ?? string.Empty).Trim(), (verdict ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        if (own.Count == 0)
            own = all.Where(t => string.IsNullOrWhiteSpace(t.verdict)).ToList();
        return own.Count == 0 ? null : own[new SeededRandom(value).Range(0, own.Count)];
    }

    /// <summary>The words of a drawn count ({count}).</summary>
    private static readonly string[] Counts = { "two", "three", "four", "five" };

    /// <summary>
    /// <paramref name="text"/> with its slots filled from <paramref name="subject"/>,
    /// the drawn ones on <paramref name="value"/> (year, month, count, sector,
    /// relative, in that order, always all drawn) and a thread's from
    /// <paramref name="visit"/>; an unknown slot is left as written (the
    /// validator refuses it).
    /// </summary>
    public static string Fill(LoreContent content, string text, LoreSubject subject, int value, VisitEntry visit, int today)
    {
        var rng = new SeededRandom(value);
        int yearMin = Math.Min(content.yearMin, content.yearMax), yearMax = Math.Max(content.yearMin, content.yearMax);
        int year = yearMin + rng.Range(0, yearMax - yearMin + 1);
        int month = 1 + rng.Range(0, 12);
        string count = Counts[rng.Range(0, Counts.Length)];
        int sector = 1 + rng.Range(0, 12);
        List<string> kin = (content.relatives ?? new List<string>()).Where(r => !string.IsNullOrWhiteSpace(r)).ToList();
        string relative = kin.Count > 0 ? kin[rng.Range(0, kin.Count)].Trim() : "cousin";
        string name = (subject.Name ?? string.Empty).Trim();

        var values = new Dictionary<string, string>
        {
            { "name", name },
            { "first", name.Split(' ').FirstOrDefault() ?? string.Empty },
            { "place", subject.Place ?? string.Empty },
            { "era", subject.Era ?? string.Empty },
            { "role", (subject.Role ?? string.Empty).ToLowerInvariant() },
            { "year", year.ToString(CultureInfo.InvariantCulture) },
            { "month", year.ToString(CultureInfo.InvariantCulture) + "-" + month.ToString("00", CultureInfo.InvariantCulture) },
            { "count", count },
            { "sector", sector.ToString(CultureInfo.InvariantCulture) },
            { "relative", relative },
            { "status", subject.Status ?? string.Empty },
            { "trips", subject.Trips.ToString(CultureInfo.InvariantCulture) },
            { "transponder", subject.Transponder ?? string.Empty },
            { "debt", AccountMaker.Credits(subject.Debt) },
            { "employer", subject.Employer ?? string.Empty },
            { "wage", subject.Wage ?? string.Empty },
            { "term", subject.Term ?? string.Empty },
            { "incident", subject.Incident ?? string.Empty },
            { "found", subject.Found ?? string.Empty }
        };
        if (visit != null)
        {
            values["date"] = visit.date ?? string.Empty;
            values["place"] = string.IsNullOrWhiteSpace(visit.place) ? values["place"] : visit.place;
            values["verdict"] = (visit.verdict ?? string.Empty).ToLowerInvariant();
            values["days"] = Math.Max(0, today - visit.day).ToString(CultureInfo.InvariantCulture);
        }

        string line = text ?? string.Empty;
        foreach (string slot in LoreSlots.In(line).Distinct())
            if (values.TryGetValue(slot, out string v))
                line = line.Replace("{" + slot + "}", v);
        return line.Trim();
    }

    /// <summary>The longest label a file line's own prefix may be ("Registry note", "Declared purpose").</summary>
    public const int MaxLabelLength = 24;

    /// <summary>
    /// A file line as a record row's label and value: a line opening with its
    /// own short label ("Registry note: ...", "Archive sources claim ..." has
    /// none) is split at its first ": " (a label of at most
    /// <see cref="MaxLabelLength"/> characters, no other colon), else the
    /// whole line under <paramref name="fallback"/>.
    /// </summary>
    public static (string Label, string Value) Split(string line, string fallback)
    {
        string text = (line ?? string.Empty).Trim();
        int colon = text.IndexOf(": ", StringComparison.Ordinal);
        if (colon > 0 && colon <= MaxLabelLength && text.Length > colon + 2)
            return (text.Substring(0, colon), text.Substring(colon + 2).Trim());
        return (fallback ?? string.Empty, text);
    }

    /// <summary>
    /// The groups a citizen record ends with (the scanner app spec §2.6, §3):
    /// FILE, a row per file line (Split), and when the traveller was seen
    /// before this run, SEEN BEFORE titled with the flag of their latest
    /// verdict ("SEEN BEFORE · DENIED 3 DAYS AGO") and a row per earlier
    /// visit, newest first (the date; the place, the verdict and any
    /// citation). None is evidence. Words by <paramref name="text"/>'s keys:
    /// records.group.file, records.row.file, records.group.seen (with {0}: the
    /// flag), visit.flag.days, visit.flag.yesterday, visit.verdict.{word}
    /// (missing: the word itself), visit.row (with {0} place, {1} verdict),
    /// visit.citation (with {0} the reason).
    /// </summary>
    public static List<RecordGroup> Groups(IReadOnlyList<string> file, IReadOnlyList<VisitEntry> earlier, int today, Func<string, string> text)
    {
        text = text ?? (k => k);
        var groups = new List<RecordGroup>();
        if (file != null && file.Count > 0)
            groups.Add(new RecordGroup(text("records.group.file"),
                                       file.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => Split(l, text("records.row.file"))).Select(r => new RecordRow(r.Label, r.Value))));

        List<VisitEntry> seen = (earlier ?? Array.Empty<VisitEntry>()).Where(v => v != null && v.day < today).OrderByDescending(v => v.day).ToList();
        SeenBefore? flag = Visits.Flag(seen, today);
        if (!flag.HasValue)
            return groups;
        string Word(string verdict)
        {
            string key = "visit.verdict." + verdict;
            string word = text(key);
            return string.IsNullOrWhiteSpace(word) || word == key ? verdict : word;
        }
        string title = text("records.group.seen").Replace("{0}", Visits.FlagText(flag.Value, Word(flag.Value.Verdict), text("visit.flag.days"), text("visit.flag.yesterday")));
        var rows = new List<RecordRow>();
        foreach (VisitEntry v in seen)
        {
            string value = text("visit.row").Replace("{0}", v.place ?? string.Empty).Replace("{1}", Word(v.verdict));
            if (!string.IsNullOrWhiteSpace(v.citation))
                value += " " + text("visit.citation").Replace("{0}", v.citation.Trim());
            rows.Add(new RecordRow(string.IsNullOrWhiteSpace(v.date) ? v.day.ToString(CultureInfo.InvariantCulture) : v.date, value));
        }
        groups.Add(new RecordGroup(title, rows));
        return groups;
    }

    /// <summary>
    /// What is wrong with <paramref name="content"/>: a blank or repeated id;
    /// a template or thread with no text, an unknown {slot}, a slot its kinds
    /// and traits do not guarantee (Generate World's check of every template
    /// slot), an unknown trait, personality (<paramref name="personalities"/>)
    /// or clue; a premade entry for no premade (<paramref name="premadeIds"/>),
    /// twice, or with a slot; a premade with fewer than 2 or more than 4 lines
    /// (<paramref name="requireEveryPremade"/>: every premade has its entry);
    /// a line longer than <see cref="LoreContent.MaxLineLength"/> or not ASCII; fewer
    /// lines bounds than sense; a chance outside 0 to 1. Empty when sound.
    /// </summary>
    public static List<string> Problems(LoreContent content, IEnumerable<string> premadeIds, IEnumerable<string> personalities, bool requireEveryPremade)
    {
        var problems = new List<string>();
        if (content == null)
        {
            problems.Add("lore: missing.");
            return problems;
        }
        var premadeSet = new HashSet<string>(premadeIds ?? Enumerable.Empty<string>());
        var personalitySet = new HashSet<string>(personalities ?? Enumerable.Empty<string>());
        var clueNames = new HashSet<string>(Enum.GetNames(typeof(LieKind)).Concat(Enum.GetNames(typeof(DirectiveFault)).Where(n => n != nameof(DirectiveFault.None))));

        if (!(content.clueChance >= 0f && content.clueChance <= 1f))
            problems.Add($"lore: clueChance {content.clueChance} is not between 0 and 1.");
        if (content.linesMin < 1 || content.linesMax < content.linesMin || content.linesMax > 4)
            problems.Add($"lore: linesMin {content.linesMin} and linesMax {content.linesMax} must hold 1 <= min <= max <= 4 (a file is 2-4 lines).");
        if (content.yearMax < content.yearMin)
            problems.Add($"lore: yearMax {content.yearMax} is before yearMin {content.yearMin}.");
        if ((content.relatives ?? new List<string>()).All(string.IsNullOrWhiteSpace))
            problems.Add("lore: relatives is empty ({relative} needs a word).");

        var seen = new HashSet<string>();
        foreach (LorePremade p in content.premades ?? new List<LorePremade>())
        {
            if (p == null)
                continue;
            if (!premadeSet.Contains(p.premade))
                problems.Add($"lore.premades: '{p.premade}' is no premade.");
            if (!seen.Add(p.premade ?? string.Empty))
                problems.Add($"lore.premades: '{p.premade}' is listed twice.");
            List<string> lines = (p.lines ?? new List<string>()).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
            if (lines.Count < 2 || lines.Count > 4)
                problems.Add($"lore.premades: '{p.premade}' has {lines.Count} lines (2 to 4).");
            foreach (string line in lines)
            {
                if (LoreSlots.In(line).Count > 0)
                    problems.Add($"lore.premades: '{p.premade}' line \"{line}\" has a {{slot}} (authored lines are printed as written).");
                LineProblems($"lore.premades: '{p.premade}'", line, problems);
            }
        }
        if (requireEveryPremade)
            foreach (string id in premadeSet.Where(id => !seen.Contains(id)))
                problems.Add($"lore.premades: premade '{id}' has no file lines.");

        var ids = new HashSet<string>();
        foreach (LoreTemplate t in content.templates ?? new List<LoreTemplate>())
        {
            if (t == null)
                continue;
            string where = $"lore.templates: '{t.id}'";
            if (string.IsNullOrWhiteSpace(t.id) || !ids.Add(t.id))
                problems.Add($"{where}: a blank or repeated id.");
            if (string.IsNullOrWhiteSpace(t.text))
                problems.Add($"{where}: no text.");
            if (!string.IsNullOrWhiteSpace(t.personality) && !personalitySet.Contains(t.personality))
                problems.Add($"{where}: personality '{t.personality}' is none of the cast.");
            foreach (string trait in t.traits ?? new List<string>())
                if (!LoreTraits.All.Contains((trait ?? string.Empty).Trim().ToLowerInvariant()))
                    problems.Add($"{where}: trait '{trait}' is none of {string.Join(", ", LoreTraits.All)}.");
            if (!string.IsNullOrWhiteSpace(t.clue) && !clueNames.Contains(t.clue.Trim()))
                problems.Add($"{where}: clue '{t.clue}' is no LieKind or DirectiveFault name.");
            HashSet<string> allowed = LoreSlots.For(t.kinds, t.traits);
            foreach (string slot in LoreSlots.In(t.text).Distinct())
                if (!allowed.Contains(slot))
                    problems.Add($"{where}: slot {{{slot}}} is unknown or not guaranteed for its kinds ({(t.kinds == null || t.kinds.Count == 0 ? "any" : string.Join("|", t.kinds))}) and traits.");
            LineProblems(where, t.text, problems);
        }

        var threadSlots = new HashSet<string>(LoreSlots.Universal.Concat(LoreSlots.Thread));
        foreach (LoreThread t in content.threads ?? new List<LoreThread>())
        {
            if (t == null)
                continue;
            string where = $"lore.threads: '{t.id}'";
            if (string.IsNullOrWhiteSpace(t.id) || !ids.Add(t.id))
                problems.Add($"{where}: a blank or repeated id.");
            if (string.IsNullOrWhiteSpace(t.text))
                problems.Add($"{where}: no text.");
            foreach (string slot in LoreSlots.In(t.text).Distinct())
                if (!threadSlots.Contains(slot))
                    problems.Add($"{where}: slot {{{slot}}} is unknown to a thread.");
            LineProblems(where, t.text, problems);
        }
        if ((content.threads ?? new List<LoreThread>()).All(t => t == null || !string.IsNullOrWhiteSpace(t.verdict)))
            problems.Add("lore.threads: no thread with a blank verdict (the line for a verdict with none of its own).");
        return problems;
    }

    /// <summary>A line's length (its slots counted as written, a fair bound) and characters.</summary>
    private static void LineProblems(string where, string line, List<string> problems)
    {
        if ((line ?? string.Empty).Length > LoreContent.MaxLineLength)
            problems.Add($"{where}: \"{line}\" is longer than {LoreContent.MaxLineLength} characters.");
        if ((line ?? string.Empty).Any(c => c > 126 || (c < 32)))
            problems.Add($"{where}: \"{line}\" is not plain ASCII.");
    }
}
