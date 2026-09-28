using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The agency's citizen records (redesign phase 2; the traveller-types spec's
/// R1 and R3): a record is rows (label, value and, for evidence, a category)
/// in titled groups; the registry keeps its records in order (the lookup by
/// name or number is the search index's, scoped to Records: CaseIndexTests).
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
    public void Add_IgnoresNullAndUnnamed()
    {
        var r = new CitizenRegistry();
        r.Add(null);
        r.Add(Entry("  ", "1 Jan 100", "X-1"));
        r.Add(Entry("Bjorn", "3 May 1131"));
        CollectionAssert.AreEqual(new[] { "Bjorn" }, r.Records.Select(x => x.FullName), "an unnamed record is not on file, even by its number");
    }

    [Test]
    public void Records_KeepTheOrderAdded()
    {
        CitizenRegistry r = Registry(Entry("Zara-7", "14 Sep 2401"), Entry("Bjorn", "3 May 1131"));
        CollectionAssert.AreEqual(new[] { "Zara-7", "Bjorn" }, r.Records.Select(x => x.FullName));
    }
}
