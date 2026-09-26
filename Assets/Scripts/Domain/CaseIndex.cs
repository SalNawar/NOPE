using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>
/// One searchable item of the Investigation app (the PC redesign §4.2): where
/// it is (its source tab, the item and row there, its key), what the results
/// show (its title and snippet) and what is matched (its label and text).
/// Filled once, before it is added to the index (IndexEntries builds them).
/// </summary>
public sealed class IndexEntry
{
    /// <summary>The item's key: PickKeys' for a pickable row ("field:0:2", "line:5", "book:Currency:greece:ancient", "record:552-1804-33:BirthDate"); else "doc:0", "rec:{record id}", "rule:0", "dev:Currency".</summary>
    public string Key;

    /// <summary>The tab the item is in.</summary>
    public AppTab Source;

    /// <summary>The item in its source: a paper (Documents), a record (Records: its place in the registry), a book (Reference: its place among the books), a line (Transcript), a deviation (Report), a rule (Rules); -1 for none.</summary>
    public int Item = -1;

    /// <summary>The row in the item: a paper's field, a record's row (in order across its groups); -1 for the item itself.</summary>
    public int Row = -1;

    /// <summary>The result's title ("Departure Manifest · Currency Carried"); ranked, never matched.</summary>
    public string Title;

    /// <summary>The label, matched (always English).</summary>
    public string Label;

    /// <summary>The value or line as shown, matched; for an untranslated line, its English key words only.</summary>
    public string Text;

    /// <summary>True for a transcript line shown untranslated (speech only: papers are always English, TR1).</summary>
    public bool Foreign;

    /// <summary>An untranslated line as shown (its glyphs and key words): the result's snippet.</summary>
    public string ForeignShown;

    /// <summary>An untranslated line's tongue (a chip's match).</summary>
    public string TongueId;

    /// <summary>An untranslated line's canonical text: matched only against a chip of the same tongue; never shown and never matched against typed text.</summary>
    public string Canonical;

    /// <summary>The item's order in its source (ties in rank go by it).</summary>
    public int Order;

    private FoldedEntry _folded;

    /// <summary>The texts the matcher reads, folded once.</summary>
    internal FoldedEntry Folded()
    {
        if (_folded != null)
            return _folded;
        var map = new List<int>();
        string text = TextMatch.Fold(Text, map);
        _folded = new FoldedEntry
        {
            Title = TextMatch.Fold(Title, null),
            Label = TextMatch.Fold(Label, null),
            Text = text,
            TextMap = map,
            TextWords = string.Join(" ", TextMatch.Words(text)),
            Canonical = Foreign ? string.Join(" ", TextMatch.Words(TextMatch.Fold(Canonical, null))) : null
        };
        return _folded;
    }
}

/// <summary>An untranslated transcript line as the index takes it: its tongue and how it shows (its glyphs, its key words in English).</summary>
public readonly struct ForeignLine
{
    /// <summary>A line in <paramref name="tongueId"/> showing as <paramref name="shown"/>.</summary>
    public ForeignLine(string tongueId, string shown)
    {
        TongueId = tongueId;
        Shown = shown;
    }

    /// <summary>The line's tongue.</summary>
    public string TongueId { get; }

    /// <summary>The line as shown.</summary>
    public string Shown { get; }
}

/// <summary>One result: the entry found, its rank and the marked parts of its snippet.</summary>
public readonly struct SearchHit
{
    /// <summary>A hit on <paramref name="entry"/> ranked <paramref name="score"/>, <paramref name="marks"/> in its snippet.</summary>
    public SearchHit(IndexEntry entry, int score, IReadOnlyList<Mark> marks)
    {
        Entry = entry;
        Score = score;
        Marks = marks ?? Array.Empty<Mark>();
    }

    /// <summary>The entry found (where a jump goes).</summary>
    public IndexEntry Entry { get; }

    /// <summary>The rank (TextMatch).</summary>
    public int Score { get; }

    /// <summary>The matched parts of the snippet (none for an untranslated line).</summary>
    public IReadOnlyList<Mark> Marks { get; }

    /// <summary>The entry's title.</summary>
    public string Title => Entry.Title;

    /// <summary>What the result shows under its title: the text, or an untranslated line's glyphs.</summary>
    public string Snippet => Entry.Foreign ? Entry.ForeignShown : Entry.Text;

    /// <summary>True for an untranslated line (its snippet is in its script).</summary>
    public bool Foreign => Entry.Foreign;
}

/// <summary>The results of one source: its first hits, best first, and how many more there are.</summary>
public sealed class ResultGroup
{
    /// <summary>A group of <paramref name="source"/> showing <paramref name="hits"/>, <paramref name="more"/> left out.</summary>
    public ResultGroup(AppTab source, IReadOnlyList<SearchHit> hits, int more)
    {
        Source = source;
        Hits = hits ?? Array.Empty<SearchHit>();
        More = Math.Max(0, more);
    }

    /// <summary>The source tab.</summary>
    public AppTab Source { get; }

    /// <summary>The hits shown, best first.</summary>
    public IReadOnlyList<SearchHit> Hits { get; }

    /// <summary>The hits left out by the cap ("Show all n").</summary>
    public int More { get; }

    /// <summary>Every hit of the source.</summary>
    public int Total => Hits.Count + More;
}

/// <summary>
/// The Investigation app's search index (the PC redesign SE2, SE6): a day
/// layer, set at the start of the day (every Reference row of today's places,
/// every row of today's citizen records, every rule), and a case layer,
/// filled as the case goes (each scanned paper and its fields, each
/// transcript line as it is spoken, each deviation as it is logged) and
/// emptied when the case ends; other travellers' cases, the Internet, Mail and
/// Notes are not in it. A search gives the hits grouped by source in the
/// order given (the tab order), best first (TextMatch's rank, then the item's
/// order), capped per group; the Records lookup is the same search scoped to
/// Records. Pure.
/// </summary>
public sealed class CaseIndex
{
    private readonly List<IndexEntry> _day = new List<IndexEntry>();
    private readonly List<IndexEntry> _case = new List<IndexEntry>();

    /// <summary>Replaces the day layer with <paramref name="entries"/> (nulls skipped); the case layer stays.</summary>
    public void SetDay(IEnumerable<IndexEntry> entries)
    {
        _day.Clear();
        foreach (IndexEntry e in entries ?? Enumerable.Empty<IndexEntry>())
            if (e != null)
                _day.Add(e);
    }

    /// <summary>Adds <paramref name="entry"/> to the case layer (a paper scanned, a line spoken, a deviation logged; null is ignored).</summary>
    public void Add(IndexEntry entry)
    {
        if (entry != null)
            _case.Add(entry);
    }

    /// <summary>The case ended: its layer empties (the day layer stays).</summary>
    public void EndCase() => _case.Clear();

    /// <summary>
    /// The hits of <paramref name="q"/> grouped by source in
    /// <paramref name="order"/> (a source not listed is left out; with
    /// <paramref name="only"/>, that source alone), each group best first
    /// (rank, then the item's order, the day layer before the case layer) and
    /// capped at <paramref name="perGroup"/> hits with the rest counted. A
    /// source without hits has no group; a query too short to search
    /// (SearchQuery.IsSearchable) gives none.
    /// </summary>
    public IReadOnlyList<ResultGroup> Search(SearchQuery q, IReadOnlyList<AppTab> order, int perGroup, AppTab? only)
    {
        var groups = new List<ResultGroup>();
        if (q == null || !q.IsSearchable || order == null)
            return groups;

        foreach (AppTab source in order)
        {
            if (only.HasValue && only.Value != source)
                continue;
            var hits = new List<(SearchHit hit, int seq)>();
            int seq = 0;
            foreach (IndexEntry e in _day.Concat(_case))
            {
                seq++;
                if (e.Source != source)
                    continue;
                var marks = new List<Mark>();
                if (TextMatch.Matches(q, e, out int score, marks))
                    hits.Add((new SearchHit(e, score, marks), seq));
            }
            if (hits.Count == 0)
                continue;
            hits.Sort((a, b) => a.hit.Score != b.hit.Score ? b.hit.Score.CompareTo(a.hit.Score)
                : a.hit.Entry.Order != b.hit.Entry.Order ? a.hit.Entry.Order.CompareTo(b.hit.Entry.Order)
                : a.seq.CompareTo(b.seq));
            int shown = Math.Min(Math.Max(0, perGroup), hits.Count);
            groups.Add(new ResultGroup(source, hits.Take(shown).Select(h => h.hit).ToList(), hits.Count - shown));
        }
        return groups;
    }
}

/// <summary>
/// The index's entries, one builder per kind of item (the PC redesign §4.2's
/// table): what each item is keyed by, where it is, its title (from the
/// caller's format, a UI string) and what of it is matched. Papers' fields are
/// always plain (TR1); an untranslated line is matched by its key words only.
/// </summary>
public static class IndexEntries
{
    /// <summary>
    /// A scanned paper (Documents, case layer): the paper itself (titled and
    /// matched by its name) and each of its <paramref name="fields"/> (label
    /// and value, in the paper's order; one blank in both is skipped, its
    /// index kept) titled by <paramref name="titleFormat"/> ({0} the paper, {1}
    /// the label), keyed as the field's pick.
    /// </summary>
    public static IEnumerable<IndexEntry> Paper(int paper, string name, IReadOnlyList<(string label, string value)> fields, string titleFormat)
    {
        yield return new IndexEntry
        {
            Key = EntryKeys.Document(paper), Source = AppTab.Documents, Item = paper, Title = name, Label = string.Empty, Text = name, Order = paper * 1000
        };
        for (int f = 0; fields != null && f < fields.Count; f++)
        {
            (string label, string value) = fields[f];
            if (string.IsNullOrWhiteSpace(label) && string.IsNullOrWhiteSpace(value))
                continue;
            yield return new IndexEntry
            {
                Key = PickKeys.Field(paper, f), Source = AppTab.Documents, Item = paper, Row = f,
                Title = Format(titleFormat, name, label), Label = label, Text = value, Order = paper * 1000 + f + 1
            };
        }
    }

    /// <summary>
    /// Every row of every record in <paramref name="registry"/> (Records, day
    /// layer), in order: titled by <paramref name="titleFormat"/> ({0} the
    /// record's name, {1} the row's label), matched by label and value (a row
    /// without a value is skipped, its place kept); an evidence row keyed as
    /// its pick, any other as its record.
    /// </summary>
    public static IEnumerable<IndexEntry> Records(CitizenRegistry registry, string titleFormat)
    {
        if (registry == null)
            yield break;
        for (int r = 0; r < registry.Records.Count; r++)
        {
            CitizenRecord record = registry.Records[r];
            int row = 0;
            foreach (RecordGroup group in record.Groups)
                foreach (RecordRow line in group.Rows)
                {
                    int at = row++;
                    if (string.IsNullOrWhiteSpace(line.Value))
                        continue;
                    yield return new IndexEntry
                    {
                        Key = line.IsEvidence ? PickKeys.Record(line.Category, record.Id) : EntryKeys.RecordCard(record.Id),
                        Source = AppTab.Records, Item = r, Row = at,
                        Title = Format(titleFormat, record.FullName, line.Label), Label = line.Label, Text = line.Value, Order = r * 1000 + at
                    };
                }
        }
    }

    /// <summary>
    /// Every row of <paramref name="facts"/> in each of <paramref name="books"/>
    /// (Reference, day layer; a book's place in the list is its item): titled
    /// by <paramref name="titleFormat"/> ({0} the book, {1} the place), its
    /// value matched as its text and the book, the place (its era in it), the
    /// country (<paramref name="country"/> of the nation id; null or blank
    /// leaves it out) and, for a row history changed, <paramref name="revisedWord"/>
    /// matched as its label; keyed as the row's pick.
    /// </summary>
    public static IEnumerable<IndexEntry> BookRows(IReadOnlyList<(ClueCategory category, string name)> books, FactTable facts, Func<string, string> country,
                                                   string revisedWord, string titleFormat)
    {
        if (books == null || facts == null)
            yield break;
        for (int b = 0; b < books.Count; b++)
        {
            (ClueCategory category, string name) = books[b];
            IReadOnlyList<FactRow> rows = facts.Rows(category);
            for (int i = 0; i < rows.Count; i++)
            {
                FactRow row = rows[i];
                string label = string.Join(" · ", new[]
                {
                    name, row.OriginLabel, country?.Invoke(row.NationId),
                    facts.IsChanged(row.NationId, row.EraId, row.Category) ? revisedWord : null
                }.Where(s => !string.IsNullOrWhiteSpace(s)));
                yield return new IndexEntry
                {
                    Key = PickKeys.BookRow(category, row.NationId, row.EraId), Source = AppTab.Reference, Item = b,
                    Title = Format(titleFormat, name, row.OriginLabel), Label = label, Text = row.Value, Order = b * 10000 + i
                };
            }
        }
    }

    /// <summary>
    /// Transcript line <paramref name="index"/> (case layer, as it is spoken),
    /// its speaker as its label. Plain (<paramref name="foreign"/> null): its
    /// text is matched. Untranslated: only its key words (the
    /// <paramref name="english"/> spans of <paramref name="text"/>) are
    /// matched, it shows as the foreign line shows, and its text is kept as
    /// the canonical a chip is matched against.
    /// </summary>
    public static IndexEntry Line(int index, string title, string speaker, string text, IReadOnlyList<(int start, int length)> english, ForeignLine? foreign) =>
        new IndexEntry
        {
            Key = PickKeys.Line(index), Source = AppTab.Transcript, Item = index, Title = title, Label = speaker,
            Text = foreign.HasValue ? KeyWordText(text, english) : text,
            Foreign = foreign.HasValue,
            ForeignShown = foreign?.Shown,
            TongueId = foreign?.TongueId,
            Canonical = foreign.HasValue ? text : null,
            Order = index
        };

    /// <summary>Deviation <paramref name="index"/> of the case's log (Report, case layer, as it is logged): its category word as its label, its report line as its text.</summary>
    public static IndexEntry Deviation(int index, ClueCategory category, string title, string categoryWord, string text) =>
        new IndexEntry
        {
            Key = "dev:" + category, Source = AppTab.Report, Item = index, Title = title, Label = categoryWord, Text = text, Order = index
        };

    /// <summary>Rule <paramref name="index"/> of the day's directives (Rules, day layer): its summary as its text.</summary>
    public static IndexEntry Rule(int index, string title, string summary) =>
        new IndexEntry
        {
            Key = "rule:" + index.ToString(CultureInfo.InvariantCulture), Source = AppTab.Rules, Item = index, Title = title,
            Label = string.Empty, Text = summary, Order = index
        };

    /// <summary>The line's key words: its English spans, in order, joined by spaces (spans outside the text are cut to it).</summary>
    private static string KeyWordText(string text, IReadOnlyList<(int start, int length)> english)
    {
        if (string.IsNullOrEmpty(text) || english == null)
            return string.Empty;
        var sb = new StringBuilder();
        foreach ((int start, int length) span in english)
        {
            int from = Math.Max(0, span.start);
            int to = Math.Min(text.Length, span.start + span.length);
            if (to <= from)
                continue;
            if (sb.Length > 0)
                sb.Append(' ');
            sb.Append(text, from, to - from);
        }
        return sb.ToString();
    }

    /// <summary>The title from its format ({0}, {1}); the parts alone, joined, when the format is blank.</summary>
    private static string Format(string format, string first, string second) =>
        string.IsNullOrWhiteSpace(format) ? first + " " + second : string.Format(CultureInfo.InvariantCulture, format, first, second);
}
