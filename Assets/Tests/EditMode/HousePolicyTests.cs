using NUnit.Framework;

/// <summary>
/// The balance simulation's buyer at Home (the Home upgrades spec HU10, §9):
/// care first for a member at the threshold, then the cheapest house upgrade
/// it may buy while the wallet keeps its reserve.
/// </summary>
public class HousePolicyTests
{
    private static HouseOffer Offer(string id, int price, bool owned = false, bool unlocked = true, params string[] requires) => new HouseOffer(id, price, owned, unlocked, requires);

    /// <summary>A small tree: seals (30) -> insulation (150) -> flat (220); filter (90) -> purifier (150); clinic (200) alone.</summary>
    private static HouseOffer[] Tree(bool sealsOwned = false) => new[]
    {
        Offer("seals", 30, owned: sealsOwned),
        Offer("insulation", 150, unlocked: sealsOwned, requires: "seals"),
        Offer("flat", 220, unlocked: false, requires: "insulation"),
        Offer("filter", 90),
        Offer("purifier", 150, unlocked: false, requires: "filter"),
        Offer("clinic", 200),
    };

    [Test]
    public void Purchase_TheCheapestBuyable_KeepingTheReserve()
    {
        var offers = new[] { Offer("bed", 110), Offer("plant", 15), Offer("radio", 45) };
        Assert.AreEqual("plant", HousePolicy.Purchase(offers, 100, 60));
    }

    [Test]
    public void Purchase_SkipsOwnedAndLocked()
    {
        var offers = new[] { Offer("plant", 15, owned: true), Offer("photo", 25, unlocked: false), Offer("radio", 45) };
        Assert.AreEqual("radio", HousePolicy.Purchase(offers, 200, 60));
    }

    [Test]
    public void Purchase_TiesByPriceGoToTheFirstIdInOrdinalOrder()
    {
        var offers = new[] { Offer("b", 30), Offer("a", 30) };
        Assert.AreEqual("a", HousePolicy.Purchase(offers, 200, 0));
    }

    [TestCase(74, null, Description = "15 would leave 59, under the reserve")]
    [TestCase(75, "plant", Description = "exactly the reserve left")]
    public void Purchase_NothingWhenItWouldBreakTheReserve(int money, string expected)
    {
        var offers = new[] { Offer("plant", 15) };
        Assert.AreEqual(expected, HousePolicy.Purchase(offers, money, 60));
    }

    [Test]
    public void Purchase_NothingWhenNothingIsBuyable()
    {
        Assert.IsNull(HousePolicy.Purchase(new[] { Offer("plant", 15, owned: true), Offer("photo", 25, unlocked: false) }, 1000, 0));
        Assert.IsNull(HousePolicy.Purchase(new HouseOffer[0], 1000, 0));
        Assert.IsNull(HousePolicy.Purchase(null, 1000, 0));
    }

    [Test]
    public void Care_TheSickestAtTheThreshold_WhenTheWalletKeepsTheReserve()
    {
        Assert.AreEqual(1, HousePolicy.Care(new[] { 3, 5, 4 }, 100, 8, 60, 3));
        Assert.AreEqual(0, HousePolicy.Care(new[] { 4, 4 }, 100, 8, 60, 3), "a tie goes to the first");
    }

    [Test]
    public void Care_NoneBelowTheThresholdOrOverTheReserve()
    {
        Assert.AreEqual(-1, HousePolicy.Care(new[] { 2, 1 }, 100, 8, 60, 3), "nobody at the threshold");
        Assert.AreEqual(-1, HousePolicy.Care(new[] { 5 }, 67, 8, 60, 3), "67 - 8 is under the reserve");
        Assert.AreEqual(0, HousePolicy.Care(new[] { 5 }, 68, 8, 60, 3), "exactly the reserve left");
        Assert.AreEqual(-1, HousePolicy.Care(null, 100, 8, 60, 3));
    }

    [Test]
    public void Climb_SavesForTheCheapestTopTierPath_AndBuysItsFirstStep()
    {
        // Top tier from 150: insulation's path is 180 (seals + itself), purifier's 240, clinic 200, flat 400.
        Assert.IsNull(HousePolicy.Climb(Tree(), 239, 60, 150), "180 + the reserve 60 is 240");
        Assert.AreEqual("seals", HousePolicy.Climb(Tree(), 240, 60, 150), "the whole path is covered: its cheapest unlocked step first");
        Assert.AreEqual("insulation", HousePolicy.Climb(Tree(sealsOwned: true), 210, 60, 150), "the seals owned, the path is insulation alone");
    }

    [Test]
    public void Climb_BuysNothingBelowTheTopTier_AndStopsWhenItIsOwned()
    {
        Assert.IsNull(HousePolicy.Climb(new[] { Offer("plant", 15), Offer("radio", 45) }, 1000, 0, 150), "no top-tier offer: nothing, however rich");
        Assert.IsNull(HousePolicy.Climb(new[] { Offer("clinic", 200, owned: true), Offer("plant", 15) }, 1000, 0, 150));
        Assert.IsNull(HousePolicy.Climb(null, 1000, 0, 150));
    }

    [Test]
    public void Climb_ATieByPathCostGoesToTheFirstTargetIdInOrdinalOrder()
    {
        var offers = new[] { Offer("b", 200), Offer("a", 200) };
        Assert.AreEqual("a", HousePolicy.Climb(offers, 260, 60, 150));
    }

    [Test]
    public void Climb_CountsASharedPrerequisiteOnce_AndSkipsAPrerequisiteNotOffered()
    {
        var offers = new[] { Offer("base", 50), Offer("top", 160, unlocked: false, requires: new[] { "base", "base", "elsewhere" }) };
        Assert.AreEqual("base", HousePolicy.Climb(offers, 210, 0, 150), "the path is 210: base once, the unknown prerequisite skipped");
        Assert.IsNull(HousePolicy.Climb(offers, 209, 0, 150));
    }
}
