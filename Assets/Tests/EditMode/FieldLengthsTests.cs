using NUnit.Framework;

/// <summary>
/// The longest value a document field can print, by category (redesign phase
/// 4, PC spec FO6): a form's box reserves the lines this needs at the floor,
/// and FormLayout.Check measures a probe of this length.
/// </summary>
public class FieldLengthsTests
{
    [TestCase(ClueCategory.Language)]
    [TestCase(ClueCategory.Currency)]
    [TestCase(ClueCategory.Technology)]
    [TestCase(ClueCategory.Culture)]
    [TestCase(ClueCategory.Name)]
    public void AFactOrAName_RunsToABookRow(ClueCategory category)
    {
        Assert.AreEqual(FactTable.MaxValueLength, FieldLengths.Longest(category));
    }

    [Test]
    public void ABirthDate_IsTheWidestDateBirthDatesWrites()
    {
        Assert.AreEqual("28 Sep 9999 BCE".Length, FieldLengths.Longest(ClueCategory.BirthDate));
        foreach (int year in new[] { -9999, -1505, -1, 1, 830, 2150, 9999 })
            for (int month = 0; month < 12; month++)
                Assert.LessOrEqual(BirthDates.Format(28, month, year).Length, FieldLengths.Longest(ClueCategory.BirthDate));
    }
}
