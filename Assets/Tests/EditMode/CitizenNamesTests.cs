using System.Linq;
using NUnit.Framework;

/// <summary>
/// Who a 2150 citizen is (traveller types K4): a name from the Future
/// places' lists together, gender from the list it came from, and the
/// country whose list gave it (their looks, C1; their lineage).
/// </summary>
public class CitizenNamesTests
{
    private static CitizenNames Names() => new CitizenNames(new[]
    {
        new NameList("egypt_future", new[] { "Omar", "Karim" }, new[] { "Nour" }),
        new NameList("japan_future", new[] { "Haruto" }, new[] { "Yui", "Aoi" })
    });

    [Test]
    public void All_IsEveryListsMen_ThenEveryListsWomen()
    {
        CollectionAssert.AreEqual(new[] { "Omar", "Karim", "Haruto", "Nour", "Yui", "Aoi" }, Names().All.ToArray());
        CollectionAssert.AreEqual(new[] { "Omar", "Karim", "Haruto" }, Names().Male.ToArray());
        CollectionAssert.AreEqual(new[] { "Nour", "Yui", "Aoi" }, Names().Female.ToArray());
    }

    [Test]
    public void GenderOf_FollowsTheMergedLists_SuffixesIncluded()
    {
        Assert.AreEqual(TravellerGender.Female, Names().GenderOf("Yui"));
        Assert.AreEqual(TravellerGender.Male, Names().GenderOf("Haruto II"));
        Assert.AreEqual(TravellerGender.Unknown, Names().GenderOf("Zed"));
    }

    [Test]
    public void SourceOf_IsTheListThatGaveTheName()
    {
        Assert.AreEqual(1, Names().SourceOf("Haruto"));
        Assert.AreEqual(0, Names().SourceOf("Nour II"), "the pool name behind a suffix");
        Assert.AreEqual(1, Names().SourceOf(" aoi "), "the scanner comparison");
        Assert.AreEqual(-1, Names().SourceOf("Zed"));
        Assert.AreEqual(-1, Names().SourceOf(null));
        Assert.AreEqual("japan_future", Names().Lists[1].Id);
    }

    [Test]
    public void Problems_ANameOnBothMergedLists_OrNoNames()
    {
        CollectionAssert.IsEmpty(Names().Problems());

        var clash = new CitizenNames(new[]
        {
            new NameList("egypt_future", new[] { "Nour" }, new[] { "Salma" }),
            new NameList("iraq_future", new[] { "Ali" }, new[] { "nour" })
        });
        Assert.IsTrue(clash.Problems().Any(p => p.Contains("Nour") && p.Contains("egypt_future") && p.Contains("iraq_future")), string.Join("\n", clash.Problems()));

        Assert.IsTrue(new CitizenNames(new NameList[0]).Problems().Any(p => p.Contains("no names")));
        Assert.IsTrue(new CitizenNames(null).Problems().Any(p => p.Contains("no names")));
    }
}
