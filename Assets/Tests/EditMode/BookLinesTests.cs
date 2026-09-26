using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// A reference book's page lines (traveller types C3, the PC spec §2.6): a
/// book lists today's rows as they are; the Costume Guide, grouped by era,
/// lists the claimed row first, then an era heading before each era's rows,
/// the claimed era first. Today's table (nation order, then era order):
/// Egypt ancient and medieval, Greece ancient and medieval, Britain medieval,
/// the present (future).
/// </summary>
public class BookLinesTests
{
    private static readonly string[] Eras = { "ancient", "medieval", "earlymodern", "future" };

    private static FactRow Row(string nation, string era, string value) =>
        new FactRow(nation, era, $"{nation} ({era})", ClueCategory.Culture, value);

    private static List<FactRow> Rows() => new List<FactRow>
    {
        Row("egypt", "ancient", "wesekh collar"),
        Row("egypt", "medieval", "red crown turban"),
        Row("greece", "ancient", "chiton"),
        Row("greece", "medieval", "skaranikon"),
        Row("britain", "medieval", "hood"),
        Row("neutral", "future", "panelled coat-dress")
    };

    /// <summary>Each line as "# era" for a heading, "* place" for the claimed row, "place" otherwise.</summary>
    private static string[] Show(IEnumerable<BookLine> lines) =>
        lines.Select(l => l.IsHeading ? "# " + l.HeadingEraId : (l.IsClaimed ? "* " : "") + l.Row.NationId + "_" + l.Row.EraId).ToArray();

    [Test]
    public void AnUngroupedBook_ListsTheRowsAsTheyAre_ClaimOrNot()
    {
        CollectionAssert.AreEqual(Rows().Select(r => r.NationId + "_" + r.EraId).ToArray(),
                                  Show(BookLines.Arrange(Rows(), false, Eras, "greece", "medieval")));
    }

    [Test]
    public void GroupedByEra_WithoutAClaim_HeadsEachEra_InEraOrder_RowsInTableOrder()
    {
        CollectionAssert.AreEqual(new[]
        {
            "# ancient", "egypt_ancient", "greece_ancient",
            "# medieval", "egypt_medieval", "greece_medieval", "britain_medieval",
            "# future", "neutral_future"
        }, Show(BookLines.Arrange(Rows(), true, Eras, null, null)));
    }

    [Test]
    public void GroupedByEra_TheClaimedRowFirst_ThenItsEra_ThenTheOthers_ThePresentLast()
    {
        CollectionAssert.AreEqual(new[]
        {
            "* greece_medieval",
            "# medieval", "egypt_medieval", "britain_medieval",
            "# ancient", "egypt_ancient", "greece_ancient",
            "# future", "neutral_future"
        }, Show(BookLines.Arrange(Rows(), true, Eras, "greece", "medieval")));
    }

    [Test]
    public void AnEraLeftEmptyByTheClaim_GetsNoHeading_AndAClaimNotInTheTable_IsNotListedFirst()
    {
        List<FactRow> rows = Rows().Where(r => r.EraId != "medieval" || r.NationId == "britain").ToList();
        CollectionAssert.AreEqual(new[] { "* britain_medieval", "# ancient", "egypt_ancient", "greece_ancient", "# future", "neutral_future" },
                                  Show(BookLines.Arrange(rows, true, Eras, "britain", "medieval")));

        CollectionAssert.AreEqual(new[] { "# medieval", "egypt_medieval", "greece_medieval", "britain_medieval", "# ancient", "egypt_ancient", "greece_ancient", "# future", "neutral_future" },
                                  Show(BookLines.Arrange(Rows(), true, Eras, "japan", "medieval")), "no such row: its era still leads");
    }

    [Test]
    public void AnEraOutsideTheOrder_ComesLast_UnderItsOwnHeading()
    {
        List<FactRow> rows = Rows();
        rows.Insert(1, Row("egypt", "bronze", "kilt"));
        CollectionAssert.AreEqual(new[]
        {
            "# ancient", "egypt_ancient", "greece_ancient",
            "# medieval", "egypt_medieval", "greece_medieval", "britain_medieval",
            "# future", "neutral_future",
            "# bronze", "egypt_bronze"
        }, Show(BookLines.Arrange(rows, true, Eras, null, null)));
    }

    [Test]
    public void NoRows_NoLines()
    {
        CollectionAssert.IsEmpty(BookLines.Arrange(new List<FactRow>(), true, Eras, "egypt", "ancient"));
        CollectionAssert.IsEmpty(BookLines.Arrange(null, true, null, null, null));
    }
}
