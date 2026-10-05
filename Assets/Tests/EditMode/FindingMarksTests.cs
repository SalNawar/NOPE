using NUnit.Framework;

/// <summary>
/// The clear mistakes a finding marks on the desk's papers (the desk-first
/// redesign, item 11): a difference marks each of its sides that is a
/// paper's field; a match or a note marks nothing.
/// </summary>
public class FindingMarksTests
{
    private static Finding F(FindingKind kind, string a, string b) => new Finding(kind, a, b, "A", "1", "B", "2", "Subject", null);

    [Test]
    public void ADifference_MarksBothPapersFields()
    {
        var marks = FindingMarks.Fields(F(FindingKind.Differs, PickKeys.Field(0, 3), PickKeys.Field(1, 2)));
        CollectionAssert.AreEqual(new[] { (0, 3), (1, 2) }, marks);
    }

    [Test]
    public void ARuleBrokenOrAnExpiredDate_MarksTheFieldSideOnly()
    {
        CollectionAssert.AreEqual(new[] { (0, 4) }, FindingMarks.Fields(F(FindingKind.RuleBroken, EntryKeys.Rule(0), PickKeys.Field(0, 4))));
        CollectionAssert.AreEqual(new[] { (2, 1) }, FindingMarks.Fields(F(FindingKind.Expired, EntryKeys.CalendarToday, PickKeys.Field(2, 1))));
        CollectionAssert.AreEqual(new[] { (0, 5) }, FindingMarks.Fields(F(FindingKind.Differs, PickKeys.Face, PickKeys.Field(0, 5))), "the photo against the face");
    }

    [Test]
    public void AMatchOrANote_MarksNothing()
    {
        Assert.IsEmpty(FindingMarks.Fields(F(FindingKind.Match, PickKeys.Field(0, 3), PickKeys.Field(1, 2))));
        Assert.IsEmpty(FindingMarks.Fields(F(FindingKind.DifferentDetails, PickKeys.Field(0, 3), PickKeys.Field(1, 2))));
        Assert.IsEmpty(FindingMarks.Fields(null));
    }
}
