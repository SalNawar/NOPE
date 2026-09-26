using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// A reference book's register in the Investigation app (the PC redesign AP5,
/// §2.6; the traveller-types spec's Q9 for the Costume Guide): the claimed
/// place's row first and flagged, the others in today's order; "Claimed place
/// only" shows that row alone; a book grouped by era (the Costume Guide) puts
/// the others under era headings, the claimed place's era first, then the
/// eras in their order, the present's era last. The rows themselves (and so
/// their picks) are today's FactTable rows, unchanged.
/// </summary>
public class ReferenceRowsTests
{
    private static readonly string[] Eras = { "ancient", "medieval", "industrial", "future" };

    private static FactRow Row(string nation, string era) =>
        new FactRow(nation, era, nation + " (" + era + ")", ClueCategory.Culture, nation + "-" + era + "-dress");

    /// <summary>Today's rows in the table's order: country, then era.</summary>
    private static readonly FactRow[] Today =
    {
        Row("egypt", "ancient"), Row("egypt", "medieval"), Row("greece", "ancient"),
        Row("greece", "industrial"), Row("italy", "medieval"), Row("britain", "future")
    };

    private static string Show(IEnumerable<ReferenceLine> lines) =>
        string.Join(" | ", lines.Select(l => l.IsHeading ? "#" + l.EraHeading : (l.Claimed ? "*" : "") + l.Row.NationId + "/" + l.Row.EraId));

    [Test]
    public void APlainBook_PutsTheClaimedRowFirst_TheOthersInTodaysOrder()
    {
        IReadOnlyList<ReferenceLine> lines = ReferenceRows.Arrange(Today, "greece", "ancient", false, false, Eras, "future");
        Assert.AreEqual("*greece/ancient | egypt/ancient | egypt/medieval | greece/industrial | italy/medieval | britain/future", Show(lines));
    }

    [TestCase(null, null)]
    [TestCase("japan", "ancient")]
    [TestCase("greece", "medieval")]
    public void NoClaimInTodaysRows_KeepsTodaysOrder_WithNothingFlagged(string nation, string era)
    {
        IReadOnlyList<ReferenceLine> lines = ReferenceRows.Arrange(Today, nation, era, false, false, Eras, "future");
        Assert.AreEqual("egypt/ancient | egypt/medieval | greece/ancient | greece/industrial | italy/medieval | britain/future", Show(lines));
    }

    [Test]
    public void ClaimedPlaceOnly_ShowsThatRowAlone()
    {
        Assert.AreEqual("*italy/medieval", Show(ReferenceRows.Arrange(Today, "italy", "medieval", true, false, Eras, "future")));
        Assert.AreEqual("*italy/medieval", Show(ReferenceRows.Arrange(Today, "italy", "medieval", true, true, Eras, "future")));
    }

    [Test]
    public void ClaimedPlaceOnly_WithoutAClaimedRow_ShowsEveryRow()
    {
        IReadOnlyList<ReferenceLine> lines = ReferenceRows.Arrange(Today, null, null, true, false, Eras, "future");
        Assert.AreEqual(Today.Length, lines.Count);
    }

    [Test]
    public void ByEra_TheClaimedRow_ThenItsEra_ThenTheErasInOrder_ThePresentLast()
    {
        IReadOnlyList<ReferenceLine> lines = ReferenceRows.Arrange(Today, "italy", "medieval", false, true, Eras, "future");
        Assert.AreEqual("*italy/medieval | #medieval | egypt/medieval | #ancient | egypt/ancient | greece/ancient | #industrial | greece/industrial | #future | britain/future",
                        Show(lines));
    }

    [Test]
    public void ByEra_AClaimedEraWithNoOtherPlace_GetsNoHeading()
    {
        IReadOnlyList<ReferenceLine> lines = ReferenceRows.Arrange(Today, "greece", "industrial", false, true, Eras, "future");
        Assert.AreEqual("*greece/industrial | #ancient | egypt/ancient | greece/ancient | #medieval | egypt/medieval | italy/medieval | #future | britain/future",
                        Show(lines));
    }

    [Test]
    public void ByEra_ThePresentClosesThePage_WhereverTheEraOrderPutsIt()
    {
        string[] presentFirst = { "future", "ancient", "medieval", "industrial" };
        IReadOnlyList<ReferenceLine> lines = ReferenceRows.Arrange(Today, null, null, false, true, presentFirst, "future");
        Assert.AreEqual("#ancient | egypt/ancient | greece/ancient | #medieval | egypt/medieval | italy/medieval | #industrial | greece/industrial | #future | britain/future",
                        Show(lines));
    }

    [Test]
    public void ByEra_AnEraMissingFromTheOrder_ComesAfterTheListedOnes_BeforeThePresent()
    {
        FactRow[] rows = { Row("egypt", "bronze"), Row("egypt", "ancient"), Row("britain", "future") };
        IReadOnlyList<ReferenceLine> lines = ReferenceRows.Arrange(rows, null, null, false, true, Eras, "future");
        Assert.AreEqual("#ancient | egypt/ancient | #bronze | egypt/bronze | #future | britain/future", Show(lines));
    }

    [Test]
    public void TheLines_CarryTodaysRowsUnchanged()
    {
        IReadOnlyList<ReferenceLine> lines = ReferenceRows.Arrange(Today, "greece", "ancient", false, true, Eras, "future");
        CollectionAssert.AreEquivalent(Today, lines.Where(l => !l.IsHeading).Select(l => l.Row).ToArray());
    }

    [Test]
    public void NoRows_NoLines()
    {
        Assert.AreEqual(0, ReferenceRows.Arrange(null, "greece", "ancient", false, true, Eras, "future").Count);
        Assert.AreEqual(0, ReferenceRows.Arrange(new FactRow[0], "greece", "ancient", true, false, null, null).Count);
    }
}
