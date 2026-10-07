using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The desktop's icon layout (the PC redesign DK3-DK6, section 4.1): Arrange's
/// column-first order and wrapping, a drop clamped into the icon area, an
/// overlap over the share moving to the nearest free spot (under it staying
/// put), Restore dropping unknown ids, placing new ones and clamping, the
/// Save/Restore round trip in invariant numbers, and Nearest in the four
/// directions.
/// </summary>
public class DesktopLayoutTests
{
    /// <summary>The spec's grid: a 1440 x 988 icon area, 120 x 132 cells from (20, 20), a 132 column step and a 140 row step.</summary>
    private static IconGrid Grid(float areaHeight = 988f) => new IconGrid(1440f, areaHeight, 120f, 132f, 20f, 20f, 132f, 140f);

    private static readonly string[] Eight = DesktopAppIds.DefaultOrder.ToArray();

    private static IconPlace At(IReadOnlyList<IconPlace> places, string id) => places.Single(p => p.Id == id);

    private static void AssertAt(IconPlace place, float x, float y)
    {
        Assert.AreEqual(x, place.X, 1e-3f, place.Id + " x");
        Assert.AreEqual(y, place.Y, 1e-3f, place.Id + " y");
    }

    [Test]
    public void TheDefaultOrder_IsTheEightApps()
    {
        // 2026-10-07 (Saleh's "arrange icons not working"): the case's app, Mail and Notes first; Orders after the account it spends from; Settings last.
        CollectionAssert.AreEqual(new[] { "investigation", "mail", "notes", "portals", "internet", "citizen_account", "orders", "settings" }, Eight);
    }

    [Test]
    public void Arrange_PutsTheEightInColumns_FromTheOrigin_InOrder()
    {
        IReadOnlyList<IconPlace> places = DesktopLayout.Arrange(Eight, Grid());
        CollectionAssert.AreEqual(Eight, places.Select(p => p.Id).ToArray());
        for (int i = 0; i < 6; i++)
            AssertAt(places[i], 20f, 20f + 140f * i);
        // A seventh row would end at 860 + 132 = 992, past the 988 u icon area: Notes heads the next column, Settings under it.
        AssertAt(places[6], 152f, 20f);
        AssertAt(places[7], 152f, 160f);
    }

    [Test]
    public void Arrange_WrapsIntoTheNextColumn_WhenTheAreaIsShort()
    {
        // 500 tall: rows at 20, 160 and 300 fit (300 + 132 <= 500); 440 does not.
        IReadOnlyList<IconPlace> places = DesktopLayout.Arrange(Eight, Grid(500f));
        AssertAt(places[2], 20f, 300f);
        AssertAt(places[3], 152f, 20f);
        AssertAt(places[5], 152f, 300f);
        AssertAt(places[6], 284f, 20f);
    }

    [Test]
    public void ADrop_OnEmptyDesktop_StaysExactlyWhereItWasDropped()
    {
        IReadOnlyList<IconPlace> others = DesktopLayout.Arrange(Eight, Grid()).Where(p => p.Id != "mail").ToList();
        AssertAt(DesktopLayout.Drop("mail", 700.5f, 333.25f, others, Grid(), 0.25f), 700.5f, 333.25f);
    }

    [Test]
    public void ADrop_IsClampedIntoTheIconArea()
    {
        IconPlace[] none = new IconPlace[0];
        AssertAt(DesktopLayout.Drop("mail", -50f, -10f, none, Grid(), 0.25f), 0f, 0f);
        AssertAt(DesktopLayout.Drop("mail", 5000f, 5000f, none, Grid(), 0.25f), 1440f - 120f, 988f - 132f);
    }

    [Test]
    public void ADrop_CoveringMoreThanTheShareOfAnotherCell_MovesToTheNearestFreeSpot()
    {
        IReadOnlyList<IconPlace> others = DesktopLayout.Arrange(Eight, Grid()).Where(p => p.Id != "settings").ToList();
        // Right on top of Portals' cell (20, 160): the next column's (152, 160) is the nearest free spot (the first column is full).
        AssertAt(DesktopLayout.Drop("settings", 20f, 160f, others, Grid(), 0.25f), 152f, 160f);
    }

    [Test]
    public void ADrop_CoveringLessThanTheShare_StaysPut()
    {
        IReadOnlyList<IconPlace> others = DesktopLayout.Arrange(Eight, Grid()).Where(p => p.Id != "settings").ToList();
        // 100 to the right of Portals: 20 of its 120 wide cell overlap (1/6 < 25 %).
        AssertAt(DesktopLayout.Drop("settings", 120f, 160f, others, Grid(), 0.25f), 120f, 160f);
    }

    [Test]
    public void OverlapShare_IsTheCoveredPartOfTheOtherCell()
    {
        Assert.AreEqual(1f, DesktopLayout.OverlapShare(10f, 10f, 10f, 10f, Grid()), 1e-6f);
        Assert.AreEqual(0.5f, DesktopLayout.OverlapShare(0f, 0f, 60f, 0f, Grid()), 1e-6f);
        Assert.AreEqual(0f, DesktopLayout.OverlapShare(0f, 0f, 120f, 0f, Grid()), 1e-6f);
        Assert.AreEqual(0.25f, DesktopLayout.OverlapShare(0f, 0f, 60f, 66f, Grid()), 1e-6f);
    }

    [Test]
    public void Restore_OfNothing_IsTheArrangement()
    {
        IReadOnlyList<IconPlace> arranged = DesktopLayout.Arrange(Eight, Grid());
        CollectionAssert.AreEqual(arranged, DesktopLayout.Restore(null, Eight, Grid()));
        CollectionAssert.AreEqual(arranged, DesktopLayout.Restore("", Eight, Grid()));
    }

    [Test]
    public void Restore_KeepsSavedPlaces_DropsUnknownIds_PlacesNewOnes_AndClamps()
    {
        string saved = "mail:700,300;lexicon:5,5;investigation:-40,2000;junk;notes:1,x";
        IReadOnlyList<IconPlace> places = DesktopLayout.Restore(saved, Eight, Grid());

        CollectionAssert.AreEqual(Eight, places.Select(p => p.Id).ToArray(), "every known id once, in the order");
        AssertAt(At(places, "mail"), 700f, 300f);
        AssertAt(At(places, "investigation"), 0f, 988f - 132f);
        // The six without a (valid) saved place take the first free arrange spots: (20, 20) is free, (20, 160) is free...
        AssertAt(At(places, "notes"), 20f, 20f);
        AssertAt(At(places, "portals"), 20f, 160f);
        AssertAt(At(places, "internet"), 20f, 300f);
        AssertAt(At(places, "citizen_account"), 20f, 440f);
        AssertAt(At(places, "orders"), 20f, 580f);
        AssertAt(At(places, "settings"), 20f, 720f);
    }

    [Test]
    public void Restore_ASixIconLayout_GainsPortalsAndOrdersInTheFirstFreeSpots()
    {
        // A layout saved before the Portals and Orders icons: the six in their column; the new two in the default order.
        string saved = "investigation:20,20;internet:20,160;mail:20,300;citizen_account:20,440;notes:20,580;settings:20,720";
        IReadOnlyList<IconPlace> places = DesktopLayout.Restore(saved, Eight, Grid());

        AssertAt(At(places, "settings"), 20f, 720f);
        AssertAt(At(places, "portals"), 152f, 20f);
        AssertAt(At(places, "orders"), 152f, 160f);
    }

    [Test]
    public void Restore_ASevenIconLayout_GainsPortalsInTheFirstFreeSpot()
    {
        // A layout saved with Orders, before the Portals icon (the portals spec v3 PA1).
        string saved = "investigation:20,20;internet:20,160;mail:20,300;citizen_account:20,440;orders:20,580;notes:20,720;settings:152,20";
        IReadOnlyList<IconPlace> places = DesktopLayout.Restore(saved, Eight, Grid());

        AssertAt(At(places, "settings"), 152f, 20f);
        AssertAt(At(places, "portals"), 152f, 160f);
    }

    [Test]
    public void Restore_ANewId_SkipsSpotsTakenBySavedIcons()
    {
        IReadOnlyList<IconPlace> places = DesktopLayout.Restore("investigation:20,20;internet:25,165", Eight, Grid());
        AssertAt(At(places, "mail"), 20f, 300f);
        AssertAt(At(places, "notes"), 20f, 440f);
        AssertAt(At(places, "portals"), 20f, 580f);
    }

    /// <summary>The PC's icon band (DesktopConfigSO: 188 x 156 cells from (20, 20), 200 and 164 steps, two columns) in its 1440 x 1020 icon area.</summary>
    private static IconGrid Band() => IconGrid.InColumns(2, 1440f, 1020f, 188f, 156f, 20f, 20f, 200f, 164f);

    [Test]
    public void TheBand_IsTwoColumnsWide_WithTheMarginOnBothSides()
    {
        IconGrid band = Band();
        Assert.AreEqual(20f + 200f + 188f + 20f, band.AreaWidth, 1e-3f, "the waiting strip starts right of it");
        Assert.AreEqual(2, band.Columns);
        Assert.AreEqual(6, band.Rows);
        Assert.AreEqual(300f, IconGrid.InColumns(9, 300f, 1020f, 188f, 156f, 20f, 20f, 200f, 164f).AreaWidth, "never wider than the area");
    }

    [Test]
    public void Arrange_OfTheShownIcons_LeavesNoGaps()
    {
        // Day 3 (the playtest 2026-10-07): Investigation and Citizen Account are not introduced yet; the shown six pack the band's spots in order.
        string[] shown = Eight.Where(id => id != "investigation" && id != "citizen_account").ToArray();
        IReadOnlyList<IconPlace> places = DesktopLayout.Arrange(shown, Band());
        CollectionAssert.AreEqual(shown, places.Select(p => p.Id).ToArray());
        for (int i = 0; i < shown.Length; i++)
            AssertAt(places[i], 20f, 20f + 164f * i);
    }

    [Test]
    public void Restore_PullsAnIconBackIntoTheBand_AndOffAnotherIcon()
    {
        // A place saved before the band (x 900, under the waiting strip) comes back to the band's edge; one that lands on an icon goes to a free spot.
        string saved = "mail:900,20;notes:20,20;portals:1300,40";
        IReadOnlyList<IconPlace> places = DesktopLayout.Restore(saved, new[] { "mail", "notes", "portals" }, Band());
        AssertAt(At(places, "mail"), 428f - 188f, 20f);
        AssertAt(At(places, "notes"), 20f, 20f);
        IconPlace portals = At(places, "portals");
        foreach (IconPlace other in places.Where(p => p.Id != "portals"))
            Assert.AreEqual(0f, DesktopLayout.OverlapShare(portals.X, portals.Y, other.X, other.Y, Band()), "portals covers " + other.Id);
        foreach (IconPlace p in places)
            Assert.IsTrue(p.X >= 0f && p.X + 188f <= Band().AreaWidth && p.Y >= 0f && p.Y + 156f <= 1020f, p + " inside the band");
    }

    [Test]
    public void SaveThenRestore_RoundTrips_InInvariantNumbers()
    {
        var places = new List<IconPlace>
        {
            new IconPlace("investigation", 12.5f, 40.25f), new IconPlace("portals", 900f, 40f), new IconPlace("internet", 700f, 300.75f), new IconPlace("mail", 0f, 600f),
            new IconPlace("citizen_account", 1320f, 856f), new IconPlace("orders", 250f, 250f), new IconPlace("notes", 400.5f, 600f),
            new IconPlace("settings", 99f, 300f)
        };
        string saved = DesktopLayout.Save(places);
        StringAssert.DoesNotContain(" ", saved);
        Assert.AreEqual("investigation:12.5,40.25;portals:900,40;internet:700,300.75;mail:0,600;citizen_account:1320,856;orders:250,250;notes:400.5,600;settings:99,300", saved);
        CollectionAssert.AreEqual(Eight.Select(id => places.Single(p => p.Id == id)).ToList(), DesktopLayout.Restore(saved, Eight, Grid()), "in the default order, none covering another");
    }

    [Test]
    public void Nearest_FindsTheClosestIconInEachDirection()
    {
        var places = new List<IconPlace>
        {
            new IconPlace("centre", 500f, 400f),
            new IconPlace("up", 510f, 200f), new IconPlace("farUp", 500f, 20f),
            new IconPlace("down", 480f, 600f),
            new IconPlace("left", 300f, 420f),
            new IconPlace("right", 800f, 380f), new IconPlace("diagonal", 700f, 700f)
        };
        Assert.AreEqual("up", DesktopLayout.Nearest("centre", 0, -1, places));
        Assert.AreEqual("down", DesktopLayout.Nearest("centre", 0, 1, places));
        Assert.AreEqual("left", DesktopLayout.Nearest("centre", -1, 0, places));
        Assert.AreEqual("right", DesktopLayout.Nearest("centre", 1, 0, places));
    }

    [Test]
    public void Nearest_IsNull_WithNothingThatWay_OrAnUnknownStart()
    {
        IReadOnlyList<IconPlace> column = DesktopLayout.Arrange(Eight.Take(6).ToArray(), Grid());
        Assert.IsNull(DesktopLayout.Nearest("investigation", 0, -1, column));
        Assert.IsNull(DesktopLayout.Nearest("investigation", 1, 0, column));
        Assert.AreEqual("mail", DesktopLayout.Nearest("investigation", 0, 1, column));
        Assert.IsNull(DesktopLayout.Nearest("lexicon", 0, 1, column));
    }

    [TestCase(0, null)]
    [TestCase(-5, null)]
    [TestCase(IconBadge.Dot, "")]
    [TestCase(1, "1")]
    [TestCase(99, "99")]
    [TestCase(100, "99+")]
    public void ABadge_ShowsItsCount_ADot_OrNothing(int count, string label)
    {
        Assert.AreEqual(label, IconBadge.Label(count));
    }
}
