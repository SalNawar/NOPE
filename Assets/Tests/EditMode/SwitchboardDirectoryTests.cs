using System.Linq;
using NUnit.Framework;

/// <summary>
/// Today's places on the switchboard's blank plates (LOOP_v5; the
/// seconds-to-learn rule: labels change per day on fixed slots): the first
/// places given win the 4 name plates and 3 period jacks each, every
/// destination's periods read oldest first, two periods with the same words
/// read their years, and a call for a place not printed takes a free jack
/// without moving one already printed.
/// </summary>
public class SwitchboardDirectoryTests
{
    private static SwitchPlace P(string nation, string era, int order, int year, string eraLabel = null) =>
        new SwitchPlace(nation, era, nation.ToUpperInvariant(), eraLabel ?? era.ToUpperInvariant(), order, year);

    [Test]
    public void TheFirstPlacesGiven_FillTheBoard_EachDestinationOldestFirst()
    {
        SwitchboardDirectory d = SwitchboardDirectory.Build(new[]
        {
            P("greece", "medieval", 1, 1430), P("greece", "ancient", 0, -430), P("italy", "ancient", 0, -50),
            P("egypt", "ancient", 0, -1470), P("iraq", "ancient", 0, -1760), P("china", "ancient", 0, 105),
            P("greece", "modern", 4, 1975), P("greece", "industrial", 3, 1860), P("greece", "ancient", 0, -430),
        }, 4, 3);
        CollectionAssert.AreEqual(new[] { "greece", "italy", "egypt", "iraq" }, d.Destinations.Select(x => x.NationId).ToArray(), "4 name plates; China is left off");
        CollectionAssert.AreEqual(new[] { "ancient", "medieval", "modern" }, d.Destinations[0].Periods.Select(p => p.EraId).ToArray(),
                                  "3 period jacks, oldest first; the fourth era and the repeat are left off");
        Assert.AreEqual("GREECE", d.Destinations[0].Label);
        Assert.IsTrue(d.TryFind("greece", "medieval", out SwitchRoute route));
        Assert.AreEqual(new SwitchRoute(0, 1), route);
        Assert.IsFalse(d.TryFind("china", "ancient", out _));
        Assert.AreEqual(1, d.Destinations[0].RowOf("medieval"));
        Assert.AreEqual(-1, d.Destinations[0].RowOf("industrial"));
    }

    [Test]
    public void TwoPeriodsWithTheSameWords_ReadTheirYears()
    {
        SwitchboardDirectory d = SwitchboardDirectory.Build(new[]
        {
            P("egypt", "ancient", 0, -1470, "ANCIENT"), P("egypt", "ancient3", 0, -45, "ANCIENT"), P("egypt", "medieval", 1, 1340, "MEDIEVAL"),
        }, 4, 3);
        CollectionAssert.AreEqual(new[] { "1470 BCE", "45 BCE", "MEDIEVAL" }, d.Destinations[0].Periods.Select(p => p.Label).ToArray());
    }

    [Test]
    public void ACallForAPlaceNotPrinted_TakesAFreeJack_WithoutMovingOneAlreadyPrinted()
    {
        SwitchboardDirectory d = SwitchboardDirectory.Build(new[] { P("greece", "medieval", 1, 1430), P("italy", "ancient", 0, -50) }, 3, 2);
        Assert.IsTrue(d.Place(P("greece", "ancient", 0, -430), out SwitchRoute route));
        Assert.AreEqual(new SwitchRoute(0, 1), route, "the next free row, after the printed Medieval");
        Assert.IsTrue(d.Place(P("greece", "medieval", 1, 1430), out route));
        Assert.AreEqual(new SwitchRoute(0, 0), route, "found where it was printed");
        Assert.IsFalse(d.Place(P("greece", "modern", 4, 1975), out _), "Greece's 2 jacks are full");
        Assert.IsTrue(d.Place(P("egypt", "ancient", 0, -1470), out route));
        Assert.AreEqual(new SwitchRoute(2, 0), route, "the next free slot");
        Assert.IsFalse(d.Place(P("iraq", "ancient", 0, -1760), out _), "3 slots, all taken");
        Assert.IsFalse(d.Place(P("", "ancient", 0, 0), out _));
        Assert.AreEqual(3, d.MaxDestinations);
        Assert.AreEqual(2, d.MaxPeriods);
    }
}
