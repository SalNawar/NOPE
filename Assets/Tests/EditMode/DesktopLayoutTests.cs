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

    private static readonly string[] Six = DesktopAppIds.DefaultOrder.ToArray();

    private static IconPlace At(IReadOnlyList<IconPlace> places, string id) => places.Single(p => p.Id == id);

    private static void AssertAt(IconPlace place, float x, float y)
    {
        Assert.AreEqual(x, place.X, 1e-3f, place.Id + " x");
        Assert.AreEqual(y, place.Y, 1e-3f, place.Id + " y");
    }

    [Test]
    public void TheDefaultOrder_IsTheSixApps()
    {
        CollectionAssert.AreEqual(new[] { "investigation", "internet", "mail", "citizen_account", "notes", "settings" }, Six);
    }

    [Test]
    public void Arrange_PutsTheSixInOneColumn_FromTheOrigin_InOrder()
    {
        IReadOnlyList<IconPlace> places = DesktopLayout.Arrange(Six, Grid());
        CollectionAssert.AreEqual(Six, places.Select(p => p.Id).ToArray());
        for (int i = 0; i < Six.Length; i++)
            AssertAt(places[i], 20f, 20f + 140f * i);
    }

    [Test]
    public void Arrange_WrapsIntoTheNextColumn_WhenTheAreaIsShort()
    {
        // 500 tall: rows at 20, 160 and 300 fit (300 + 132 <= 500); 440 does not.
        IReadOnlyList<IconPlace> places = DesktopLayout.Arrange(Six, Grid(500f));
        AssertAt(places[2], 20f, 300f);
        AssertAt(places[3], 152f, 20f);
        AssertAt(places[5], 152f, 300f);
    }

    [Test]
    public void ADrop_OnEmptyDesktop_StaysExactlyWhereItWasDropped()
    {
        IReadOnlyList<IconPlace> others = DesktopLayout.Arrange(Six, Grid()).Where(p => p.Id != "mail").ToList();
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
        IReadOnlyList<IconPlace> others = DesktopLayout.Arrange(Six, Grid()).Where(p => p.Id != "settings").ToList();
        // Right on top of Internet's cell (20, 160): the next column's (152, 160) is nearer than the column's empty sixth spot (20, 720).
        AssertAt(DesktopLayout.Drop("settings", 20f, 160f, others, Grid(), 0.25f), 152f, 160f);
    }

    [Test]
    public void ADrop_CoveringLessThanTheShare_StaysPut()
    {
        IReadOnlyList<IconPlace> others = DesktopLayout.Arrange(Six, Grid()).Where(p => p.Id != "settings").ToList();
        // 100 to the right of Internet: 20 of its 120 wide cell overlap (1/6 < 25 %).
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
        IReadOnlyList<IconPlace> arranged = DesktopLayout.Arrange(Six, Grid());
        CollectionAssert.AreEqual(arranged, DesktopLayout.Restore(null, Six, Grid()));
        CollectionAssert.AreEqual(arranged, DesktopLayout.Restore("", Six, Grid()));
    }

    [Test]
    public void Restore_KeepsSavedPlaces_DropsUnknownIds_PlacesNewOnes_AndClamps()
    {
        string saved = "mail:700,300;lexicon:5,5;investigation:-40,2000;junk;notes:1,x";
        IReadOnlyList<IconPlace> places = DesktopLayout.Restore(saved, Six, Grid());

        CollectionAssert.AreEqual(Six, places.Select(p => p.Id).ToArray(), "every known id once, in the order");
        AssertAt(At(places, "mail"), 700f, 300f);
        AssertAt(At(places, "investigation"), 0f, 988f - 132f);
        // The four without a (valid) saved place take the first free arrange spots: (20, 20) is free, (20, 160) is free...
        AssertAt(At(places, "internet"), 20f, 20f);
        AssertAt(At(places, "citizen_account"), 20f, 160f);
        AssertAt(At(places, "notes"), 20f, 300f);
        AssertAt(At(places, "settings"), 20f, 440f);
    }

    [Test]
    public void Restore_ANewId_SkipsSpotsTakenBySavedIcons()
    {
        IReadOnlyList<IconPlace> places = DesktopLayout.Restore("investigation:20,20;internet:25,165", Six, Grid());
        AssertAt(At(places, "mail"), 20f, 300f);
    }

    [Test]
    public void SaveThenRestore_RoundTrips_InInvariantNumbers()
    {
        var places = new List<IconPlace>
        {
            new IconPlace("investigation", 12.5f, 40.25f), new IconPlace("internet", 700f, 300.75f), new IconPlace("mail", 0f, 0f),
            new IconPlace("citizen_account", 1320f, 856f), new IconPlace("notes", 400.5f, 600f), new IconPlace("settings", 99f, 101f)
        };
        string saved = DesktopLayout.Save(places);
        StringAssert.DoesNotContain(" ", saved);
        Assert.AreEqual("investigation:12.5,40.25;internet:700,300.75;mail:0,0;citizen_account:1320,856;notes:400.5,600;settings:99,101", saved);
        CollectionAssert.AreEqual(places, DesktopLayout.Restore(saved, Six, Grid()));
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
        IReadOnlyList<IconPlace> column = DesktopLayout.Arrange(Six, Grid());
        Assert.IsNull(DesktopLayout.Nearest("investigation", 0, -1, column));
        Assert.IsNull(DesktopLayout.Nearest("investigation", 1, 0, column));
        Assert.AreEqual("internet", DesktopLayout.Nearest("investigation", 0, 1, column));
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
