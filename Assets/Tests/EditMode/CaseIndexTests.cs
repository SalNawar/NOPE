using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Search's index (redesign phase 19, the PC spec's SE2, SE5, SE6 and §4.2):
/// the day layer (Reference rows, records, rules) and the case layer (papers
/// and their fields, transcript lines, deviations); results grouped by source
/// in the given order, a cap per group with the rest counted, a source filter,
/// the ranking; an untranslated line found only by its key words and speaker,
/// or glyph to glyph by a chip of the same tongue; papers always plain; and
/// the Records lookup, which is the index scoped to Records (the old
/// CitizenRegistry.Find rules, moved here).
/// </summary>
public class CaseIndexTests
{
    private static readonly IReadOnlyList<AppTab> Order = TabOrder.Default;

    /// <summary>A registry entry as CaseFactory.BuildRegistry writes one: the Displacement No. row only with a number.</summary>
    private static CitizenRecord Entry(string name, string born, string number = null)
    {
        var rows = new List<RecordRow> { new RecordRow("Name", name, ClueCategory.Name) };
        if (number != null)
            rows.Add(new RecordRow("Displacement No.", number, ClueCategory.CitizenId));
        rows.Add(new RecordRow("Born", born, ClueCategory.BirthDate));
        rows.Add(new RecordRow("Origin", "Norvik (Medieval)", ClueCategory.Destination));
        rows.Add(new RecordRow("Note", "No remarks on file."));
        return new CitizenRecord(name, number, new[] { new RecordGroup("REGISTRY ENTRY", rows) });
    }

    private static CitizenRegistry Registry(params CitizenRecord[] records)
    {
        var r = new CitizenRegistry();
        foreach (CitizenRecord record in records)
            r.Add(record);
        return r;
    }

    private static FactTable Facts()
    {
        var facts = new FactTable();
        facts.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Currency, "Drachma");
        facts.Add("egypt", "ancient", "Thebes (Ancient)", ClueCategory.Currency, "Deben");
        facts.Add("greece", "medieval", "Mystras (Medieval)", ClueCategory.Currency, "Hyperpyron");
        facts.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Language, "Attic Greek");
        return facts;
    }

    private static readonly IReadOnlyList<(ClueCategory category, string name)> Books = new[]
    {
        (ClueCategory.Currency, "Currency Ledger"), (ClueCategory.Language, "Lexicon")
    };

    private static string Country(string nationId) => nationId == "greece" ? "Greece" : nationId == "egypt" ? "Egypt" : null;

    /// <summary>A day with the four facts in two books, the given records and two rules.</summary>
    private static CaseIndex Day(CitizenRegistry registry = null)
    {
        var index = new CaseIndex();
        var day = new List<IndexEntry>();
        day.AddRange(IndexEntries.BookRows(Books, Facts(), Country, "revised", "{0} · {1}"));
        day.AddRange(IndexEntries.Records(registry ?? Registry(Entry("Bjorn", "3 May 1131", "DP-0412-07"), Entry("Oren Hale", "2 Feb 2117", "552-1804-33")), "{0} · {1}"));
        day.Add(IndexEntries.Rule(0, "Rule 1", "Deny travellers carrying Drachma."));
        day.Add(IndexEntries.Rule(1, "Rule 2", "Deny anyone born before 1100."));
        index.SetDay(day);
        return index;
    }

    private static IReadOnlyList<ResultGroup> Search(CaseIndex index, string typed, int perGroup = 5, AppTab? only = null, SearchChip? chip = null) =>
        index.Search(SearchQuery.Parse(typed, chip), Order, perGroup, only);

    private static List<string> Titles(ResultGroup group) => group.Hits.Select(h => h.Title).ToList();

    [Test]
    public void DayLayer_ReferenceRecordsAndRules()
    {
        CaseIndex index = Day();
        IReadOnlyList<ResultGroup> groups = Search(index, "drachma");
        CollectionAssert.AreEqual(new[] { AppTab.Reference, AppTab.Rules }, groups.Select(g => g.Source));
        CollectionAssert.AreEqual(new[] { "Currency Ledger · Periclean Athens (Ancient)" }, Titles(groups[0]));
        Assert.AreEqual("Drachma", groups[0].Hits[0].Snippet);
        CollectionAssert.AreEqual(new[] { "Rule 1" }, Titles(groups[1]));

        CollectionAssert.AreEqual(new[] { "Bjorn · Born" }, Titles(Search(index, "1131").Single()));
    }

    [Test]
    public void Reference_APlaceIsFoundByItsNameAndCountry_AndARevisedRowByTheWord()
    {
        CaseIndex index = Day();
        ResultGroup athens = Search(index, "athens").Single();
        CollectionAssert.AreEqual(new[] { "Currency Ledger · Periclean Athens (Ancient)", "Lexicon · Periclean Athens (Ancient)" }, Titles(athens));
        Assert.AreEqual(3, Search(index, "greece").Single().Total, "the country's rows in every book");

        FactTable facts = Facts();
        facts.MarkChanged("egypt", "ancient", ClueCategory.Currency);
        var revised = new CaseIndex();
        revised.SetDay(IndexEntries.BookRows(Books, facts, Country, "revised", "{0} · {1}"));
        CollectionAssert.AreEqual(new[] { "Currency Ledger · Thebes (Ancient)" }, Titles(Search(revised, "revised").Single()));
    }

    [Test]
    public void Reference_RowsKeepTheirPickKeysAndBooks()
    {
        IndexEntry deben = IndexEntries.BookRows(Books, Facts(), Country, "revised", "{0} · {1}").Single(e => e.Text == "Deben");
        Assert.AreEqual(PickKeys.BookRow(ClueCategory.Currency, "egypt", "ancient"), deben.Key);
        Assert.AreEqual(AppTab.Reference, deben.Source);
        Assert.AreEqual(0, deben.Item, "the book's place among the books");
    }

    [Test]
    public void CaseLayer_PapersLinesAndDeviations_ClearedAtTheCasesEnd_TheDayKept()
    {
        CaseIndex index = Day();
        foreach (IndexEntry e in IndexEntries.Paper(1, "Intake Declaration", new[] { ("Coin of Home", "Drachma"), ("Native Tongue", "Attic Greek") }, "{0} · {1}"))
            index.Add(e);
        index.Add(IndexEntries.Line(4, "Lysimache · line 5", "Lysimache", "We paid in Drachma.", null, null));
        index.Add(IndexEntries.Deviation(0, ClueCategory.Currency, "Deviation · Currency", "Currency", "Coin of Home: Drachma is not of Thebes."));

        IReadOnlyList<ResultGroup> groups = Search(index, "drachma");
        CollectionAssert.AreEqual(new[] { AppTab.Documents, AppTab.Reference, AppTab.Transcript, AppTab.Report, AppTab.Rules }, groups.Select(g => g.Source),
                                  "grouped in the given source order");
        CollectionAssert.AreEqual(new[] { "Intake Declaration · Coin of Home" }, Titles(groups[0]));
        Assert.AreEqual(PickKeys.Field(1, 0), groups[0].Hits[0].Entry.Key);
        Assert.AreEqual((1, 0), (groups[0].Hits[0].Entry.Item, groups[0].Hits[0].Entry.Row));
        Assert.AreEqual(PickKeys.Line(4), groups[2].Hits[0].Entry.Key);

        CollectionAssert.AreEqual(new[] { "Intake Declaration" }, Titles(Search(index, "intake").Single()), "a paper by its name");

        index.EndCase();
        CollectionAssert.AreEqual(new[] { AppTab.Reference, AppTab.Rules }, Search(index, "drachma").Select(g => g.Source));
    }

    [Test]
    public void SetDay_ReplacesTheDayLayer_KeepsTheCase()
    {
        CaseIndex index = Day();
        index.Add(IndexEntries.Line(0, "Desk · line 1", "Desk", "Papers, please: Drachma?", null, null));
        index.SetDay(new[] { IndexEntries.Rule(0, "Rule 1", "Nothing today.") });
        CollectionAssert.AreEqual(new[] { AppTab.Transcript }, Search(index, "drachma").Select(g => g.Source));
        CollectionAssert.IsEmpty(Search(index, "1131"));
    }

    [Test]
    public void Groups_CapTheirHits_AndCountTheRest_AFilterShowsOneSource()
    {
        var index = new CaseIndex();
        for (int i = 0; i < 7; i++)
            index.Add(IndexEntries.Line(i, "Desk · line " + (i + 1), "Desk", "Coin " + i, null, null));
        index.SetDay(new[] { IndexEntries.Rule(0, "Rule 1", "Coin rules.") });

        IReadOnlyList<ResultGroup> all = Search(index, "coin", perGroup: 5);
        Assert.AreEqual(5, all[0].Hits.Count);
        Assert.AreEqual(2, all[0].More);
        Assert.AreEqual(7, all[0].Total);

        IReadOnlyList<ResultGroup> only = Search(index, "coin", perGroup: int.MaxValue, only: AppTab.Transcript);
        Assert.AreEqual(AppTab.Transcript, only.Single().Source);
        Assert.AreEqual(7, only[0].Hits.Count);
        Assert.AreEqual(0, only[0].More);
    }

    [Test]
    public void Ranking_WholeValueFirst_ThenItemOrder()
    {
        var index = new CaseIndex();
        index.Add(IndexEntries.Line(0, "Desk · line 1", "Desk", "Marcus II", null, null));
        index.Add(IndexEntries.Line(1, "Desk · line 2", "Desk", "Marcus", null, null));
        index.Add(IndexEntries.Line(2, "Desk · line 3", "Desk", "and Marcus", null, null));
        CollectionAssert.AreEqual(new[] { "Desk · line 2", "Desk · line 1", "Desk · line 3" }, Titles(Search(index, "marcus").Single()));
    }

    [Test]
    public void TooShortQueries_GiveNoGroups()
    {
        CaseIndex index = Day();
        CollectionAssert.IsEmpty(Search(index, ""));
        CollectionAssert.IsEmpty(Search(index, "d"));
        CollectionAssert.IsNotEmpty(Search(index, "3"), "one digit is enough");
    }

    [Test]
    public void UntranslatedLine_TypedTextFindsOnlyItsKeyWordsAndSpeaker_NeverTheHiddenEnglish()
    {
        const string said = "We paid in Drachma, of course.";
        IReadOnlyList<(int start, int length)> keyWords = new[] { (11, 7) };
        var index = new CaseIndex();
        index.Add(IndexEntries.Line(6, "Lysimache · line 7", "Lysimache", said, keyWords, new ForeignLine("greek", "ΩΨ ΞΣΦ ΘΛ Drachma, ΠΔ ΓΒΚ.")));

        IndexEntry line = Search(index, "drachma").Single().Hits.Single().Entry;
        Assert.IsTrue(line.Foreign);
        Assert.AreEqual("Drachma", line.Text, "indexed by its key words only");
        Assert.AreEqual("ΩΨ ΞΣΦ ΘΛ Drachma, ΠΔ ΓΒΚ.", Search(index, "drachma")[0].Hits[0].Snippet, "the hit shows the glyphs");
        CollectionAssert.IsNotEmpty(Search(index, "lysimache"));
        CollectionAssert.IsEmpty(Search(index, "paid"));
        CollectionAssert.IsEmpty(Search(index, "\"of course\""));
    }

    [Test]
    public void Chip_FindsOnlyUntranslatedLinesOfTheSameTongueThatAreEqual()
    {
        const string said = "We paid in Drachma, of course.";
        var index = new CaseIndex();
        index.Add(IndexEntries.Line(2, "Lysimache · line 3", "Lysimache", said, null, new ForeignLine("greek", "ΩΨ ΞΣΦ")));
        index.Add(IndexEntries.Line(5, "Lysimache · line 6", "Lysimache", "Something else.", null, new ForeignLine("greek", "ΣΦΩ")));
        index.Add(IndexEntries.Line(7, "Lysimache · line 8", "Lysimache", said, null, new ForeignLine("greek", "ΩΨ ΞΣΦ")));
        index.Add(IndexEntries.Line(8, "Nikias · line 9", "Nikias", said, null, new ForeignLine("latin", "ΛΛ")));
        index.Add(IndexEntries.Line(9, "Desk · line 10", "Desk", said, null, null));
        foreach (IndexEntry e in IndexEntries.Paper(0, "Intake Declaration", new[] { ("Statement", said) }, "{0} · {1}"))
            index.Add(e);

        ResultGroup found = Search(index, null, chip: new SearchChip("greek", said)).Single();
        CollectionAssert.AreEqual(new[] { PickKeys.Line(2), PickKeys.Line(7) }, found.Hits.Select(h => h.Entry.Key));
    }

    [Test]
    public void PaperFields_AreAlwaysPlain()
    {
        foreach (IndexEntry e in IndexEntries.Paper(0, "Displacement Certificate", new[] { ("Full Name", "Nikias"), ("Origin", "Periclean Athens (Ancient)") }, "{0} · {1}"))
        {
            Assert.IsFalse(e.Foreign);
            Assert.IsNull(e.Canonical);
            Assert.IsNull(e.TongueId);
        }
    }

    [Test]
    public void ALineHeardInEnglish_IsPlain_EveryWordFindsIt()
    {
        var index = new CaseIndex();
        index.Add(IndexEntries.Line(6, "Lysimache · line 7", "Lysimache", "We paid in drachmas, of course.", new[] { (11, 8) }, null));
        CollectionAssert.IsNotEmpty(Search(index, "paid"));
        Assert.AreEqual("We paid in drachmas, of course.", Search(index, "paid")[0].Hits[0].Snippet);
    }

    // The Records lookup (SE6): the index scoped to Records, its best hit (the rules CitizenRegistry.Find had, now the one matcher's).

    private static IndexEntry Lookup(CaseIndex index, string query) =>
        index.Search(SearchQuery.Parse(query), Order, 1, AppTab.Records).FirstOrDefault()?.Hits[0].Entry;

    private static string LookupName(CaseIndex index, CitizenRegistry registry, string query)
    {
        IndexEntry hit = Lookup(index, query);
        return hit != null ? registry.Records[hit.Item].FullName : null;
    }

    private static (CaseIndex index, CitizenRegistry registry) RecordsDay(params CitizenRecord[] records)
    {
        CitizenRegistry registry = Registry(records);
        var index = new CaseIndex();
        index.SetDay(IndexEntries.Records(registry, "{0} · {1}"));
        return (index, registry);
    }

    [Test]
    public void Lookup_ExactName_AnyCaseAndSpacing()
    {
        (CaseIndex index, CitizenRegistry registry) = RecordsDay(Entry("Bjorn", "3 May 1131"), Entry("Zara-7", "14 Sep 2401"));
        Assert.AreEqual("Bjorn", LookupName(index, registry, "Bjorn"));
        Assert.AreEqual("Bjorn", LookupName(index, registry, "  bJORN "));
        Assert.AreEqual("Zara-7", LookupName(index, registry, "zara"), "a word start of the name");
    }

    [Test]
    public void Lookup_ExactNameBeatsAnEarlierName_ThatStartsWithIt()
    {
        // Audit R1-022: NameRoster hands out "Marcus" after "Marcus II".
        (CaseIndex index, CitizenRegistry registry) = RecordsDay(Entry("Marcus II", "1 Jan 100"), Entry("Marcus", "2 Feb 200"));
        Assert.AreEqual("Marcus", LookupName(index, registry, "Marcus"));
        (index, registry) = RecordsDay(Entry("Anna Maria", "1 Jan 100"), Entry("Anna", "2 Feb 200"));
        Assert.AreEqual("Anna", LookupName(index, registry, "anna"));
    }

    [Test]
    public void Lookup_ByNumber_AtTheNumbersRow()
    {
        (CaseIndex index, CitizenRegistry registry) = RecordsDay(Entry("Bjorn", "3 May 1131", "DP-0412-07"), Entry("Oren Hale", "2 Feb 2117", "552-1804-33"));
        Assert.AreEqual("Oren Hale", LookupName(index, registry, "552-1804-33"));
        Assert.AreEqual("Bjorn", LookupName(index, registry, " dp-0412-07 "));
        IndexEntry hit = Lookup(index, "552-1804-33");
        Assert.AreEqual(PickKeys.Record(ClueCategory.CitizenId, "552-1804-33"), hit.Key);
        Assert.AreEqual(1, hit.Row, "the Displacement No. row, after the name");
    }

    [Test]
    public void Lookup_APartOfANumber_FindsItByItsWordStarts()
    {
        // One matcher (SE6): what search finds, the lookup finds; the whole number still ranks first.
        (CaseIndex index, CitizenRegistry registry) = RecordsDay(Entry("Oren Hale", "2 Feb 2117", "552-1804-33"), Entry("Bjorn", "3 May 1131", "552-1804"));
        Assert.AreEqual("Oren Hale", LookupName(index, registry, "1804 33"));
        Assert.AreEqual("Bjorn", LookupName(index, registry, "552-1804"), "the whole value first");
        Assert.IsNull(LookupName(index, registry, "804"), "never the middle of a number");
    }

    [Test]
    public void Lookup_UnknownOrBlank_FindsNothing()
    {
        (CaseIndex index, CitizenRegistry registry) = RecordsDay(Entry("Bjorn", "3 May 1131"));
        Assert.IsNull(LookupName(index, registry, "Cassia"));
        Assert.IsNull(LookupName(index, registry, null));
        Assert.IsNull(LookupName(index, registry, "   "));
    }

    [Test]
    public void Records_RowsTitledByNameAndLabel_EvidenceRowsKeyedByTheirRecord()
    {
        CitizenRegistry registry = Registry(Entry("Bjorn", "3 May 1131", "DP-0412-07"));
        List<IndexEntry> rows = IndexEntries.Records(registry, "{0} · {1}").ToList();
        CollectionAssert.AreEqual(new[] { "Bjorn · Name", "Bjorn · Displacement No.", "Bjorn · Born", "Bjorn · Origin", "Bjorn · Note" }, rows.Select(r => r.Title));
        CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, rows.Select(r => r.Row));
        Assert.AreEqual(PickKeys.Record(ClueCategory.BirthDate, "DP-0412-07"), rows[2].Key);
        Assert.AreEqual("rec:DP-0412-07", rows[4].Key, "a row that is not evidence is the record's");
        Assert.IsTrue(rows.All(r => r.Source == AppTab.Records && r.Item == 0));
    }

    [Test]
    public void Rules_AndDeviations_AreIndexedByTheirText()
    {
        CaseIndex index = Day();
        index.Add(IndexEntries.Deviation(0, ClueCategory.BirthDate, "Deviation · Date of birth", "Date of birth", "Born 3 May 1131 against the record's 3 May 1129."));
        ResultGroup report = Search(index, "1129").Single();
        Assert.AreEqual(AppTab.Report, report.Source);
        Assert.AreEqual("dev:BirthDate", report.Hits[0].Entry.Key);
        Assert.AreEqual("rule:1", Search(index, "born before").Single().Hits[0].Entry.Key);
    }
}
