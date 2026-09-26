using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The agency's citizen records (redesign phase 2; the traveller-types spec's
/// R1 and R3): a record is rows (label, value and, for evidence, a category)
/// in titled groups; the lookup takes an exact agency number first, then an
/// exact name, then the first name containing the query.
/// </summary>
public class CitizenRegistryTests
{
    /// <summary>A registry entry as today's travellers have it: one group, Name and Born are evidence, Origin and Note are not.</summary>
    private static CitizenRecord Entry(string name, string born, string number = null) =>
        new CitizenRecord(name, number, new[]
        {
            new RecordGroup("REGISTRY ENTRY", new[]
            {
                new RecordRow("Name", name, ClueCategory.Name),
                new RecordRow("Born", born, ClueCategory.BirthDate),
                new RecordRow("Origin", "Norvik (Medieval)"),
                new RecordRow("Note", "No remarks on file.")
            })
        });

    private static CitizenRegistry Registry(params CitizenRecord[] records)
    {
        var r = new CitizenRegistry();
        foreach (CitizenRecord record in records)
            r.Add(record);
        return r;
    }

    private static CitizenRegistry Registry() =>
        Registry(Entry("Bjorn", "3 May 1131"), Entry("Zara-7", "14 Sep 2401"));

    [Test]
    public void Record_KeepsItsGroupsAndRowsInOrder()
    {
        var record = new CitizenRecord("Oren Hale", "552-1804-33", new[]
        {
            new RecordGroup("RECORDS", new[] { new RecordRow("Name", "Oren Hale", ClueCategory.Name), new RecordRow("Standing", "Good") }),
            new RecordGroup("TRAVEL", new[] { new RecordRow("History", "none") }),
            new RecordGroup("", new[] { new RecordRow("Note", "No remarks on file.") })
        });

        Assert.AreEqual("Oren Hale", record.FullName);
        Assert.AreEqual("552-1804-33", record.Number);
        CollectionAssert.AreEqual(new[] { "RECORDS", "TRAVEL", "" }, record.Groups.Select(g => g.Title));
        CollectionAssert.AreEqual(new[] { "Name", "Standing" }, record.Groups[0].Rows.Select(r => r.Label));
        CollectionAssert.AreEqual(new[] { "Oren Hale", "Good" }, record.Groups[0].Rows.Select(r => r.Value));
    }

    [Test]
    public void Row_IsEvidenceOnlyWithACategory()
    {
        var born = new RecordRow("Born", "3 May 1131", ClueCategory.BirthDate);
        var note = new RecordRow("Note", "No remarks on file.");
        Assert.IsTrue(born.IsEvidence);
        Assert.AreEqual(ClueCategory.BirthDate, born.Category);
        Assert.IsFalse(note.IsEvidence);
    }

    [Test]
    public void Record_CopiesWhatItIsGiven()
    {
        var rows = new List<RecordRow> { new RecordRow("Name", "Bjorn", ClueCategory.Name) };
        var groups = new List<RecordGroup> { new RecordGroup("ENTRY", rows) };
        var record = new CitizenRecord("Bjorn", null, groups);
        rows.Add(new RecordRow("Note", "late"));
        groups.Add(new RecordGroup("MORE", rows));
        Assert.AreEqual(1, record.Groups.Count);
        Assert.AreEqual(1, record.Groups[0].Rows.Count);
    }

    [Test]
    public void Record_NullGroupsAndRowsAreEmpty()
    {
        CollectionAssert.IsEmpty(new CitizenRecord("Bjorn", null, null).Groups);
        CollectionAssert.IsEmpty(new RecordGroup("ENTRY", null).Rows);
    }

    [Test]
    public void Id_IsTheNumber_ElseTheName()
    {
        Assert.AreEqual("552-1804-33", Entry("Oren Hale", "2 Feb 2117", " 552-1804-33 ").Id);
        Assert.AreEqual("Oren Hale", Entry("Oren Hale", "2 Feb 2117").Id);
        Assert.AreEqual("Oren Hale", Entry("Oren Hale", "2 Feb 2117", "  ").Id);
    }

    [Test]
    public void Find_ExactName_ReturnsRecord()
    {
        Assert.AreEqual("Bjorn", Registry().Find("Bjorn").FullName);
    }

    [Test]
    public void Find_IsCaseAndWhitespaceInsensitive()
    {
        Assert.AreEqual("Bjorn", Registry().Find("  bJORN ").FullName);
    }

    [Test]
    public void Find_PartialName_FallsBackToContains()
    {
        Assert.AreEqual("Zara-7", Registry().Find("zara").FullName);
    }

    [Test]
    public void Find_ExactNameBeatsAnEarlierSubstring()
    {
        // Audit R1-022: NameRoster hands out "Marcus" after "Marcus II".
        Assert.AreEqual("Marcus", Registry(Entry("Marcus II", "1 Jan 100"), Entry("Marcus", "2 Feb 200")).Find("Marcus").FullName);
        Assert.AreEqual("Anna", Registry(Entry("Anna Maria", "1 Jan 100"), Entry("Anna", "2 Feb 200")).Find("anna").FullName);
    }

    [Test]
    public void Find_ByNumber()
    {
        CitizenRegistry r = Registry(Entry("Bjorn", "3 May 1131", "DP-0412-07"), Entry("Oren Hale", "2 Feb 2117", "552-1804-33"));
        Assert.AreEqual("Oren Hale", r.Find("552-1804-33").FullName);
        Assert.AreEqual("Bjorn", r.Find(" dp-0412-07 ").FullName, "trimmed, any case");
    }

    [Test]
    public void Find_NumberFirst_BeforeAnyName()
    {
        // A query equal to one record's number finds it, though an earlier record's name is that text.
        CitizenRegistry r = Registry(Entry("DP-0412-07", "1 Jan 100"), Entry("Bjorn", "3 May 1131", "DP-0412-07"));
        Assert.AreEqual("Bjorn", r.Find("DP-0412-07").FullName);
    }

    [Test]
    public void Find_NumbersMatchWhole_NeverPartly()
    {
        CitizenRegistry r = Registry(Entry("Oren Hale", "2 Feb 2117", "552-1804-33"));
        Assert.IsNull(r.Find("552-1804"));
        Assert.IsNull(r.Find("1804"));
    }

    [Test]
    public void Find_UnknownName_ReturnsNull()
    {
        Assert.IsNull(Registry().Find("Cassia"));
    }

    [Test]
    public void Find_NullOrEmpty_ReturnsNull()
    {
        Assert.IsNull(Registry().Find(null));
        Assert.IsNull(Registry().Find("   "));
    }

    [Test]
    public void Add_IgnoresNullAndUnnamed()
    {
        var r = new CitizenRegistry();
        r.Add(null);
        r.Add(Entry("  ", "1 Jan 100", "X-1"));
        Assert.IsNull(r.Find("X-1"), "an unnamed record is not on file, even by its number");
    }
}
