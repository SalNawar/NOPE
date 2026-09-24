using System.Collections.Generic;
using NUnit.Framework;

public class NameRosterTests
{
    private static readonly string[] Pool = { "Marcus", "Lucia", "Gaius" };

    /// <summary>Deterministic "random" index: always the first candidate.</summary>
    private static int First(int count) => 0;

    [Test]
    public void Take_GivesEveryPoolName_BeforeAnyRepeat()
    {
        var roster = new NameRoster();
        var names = new HashSet<string>();
        for (int i = 0; i < Pool.Length; i++)
            names.Add(roster.Take(Pool, First));
        CollectionAssert.AreEquivalent(Pool, names);
    }

    [Test]
    public void Take_WhenPoolExhausted_AddsRomanSuffixes()
    {
        var roster = new NameRoster();
        string[] pool = { "Marcus" };
        Assert.AreEqual("Marcus", roster.Take(pool, First));
        Assert.AreEqual("Marcus II", roster.Take(pool, First));
        Assert.AreEqual("Marcus III", roster.Take(pool, First));
    }

    [Test]
    public void Reserve_BlocksThatName_CaseAndSpaceInsensitive()
    {
        var roster = new NameRoster();
        Assert.IsTrue(roster.Reserve("Marcus"));
        Assert.IsFalse(roster.Reserve("marcus "));
        Assert.AreEqual("Lucia", roster.Take(new[] { "Marcus", "Lucia" }, First));
    }

    [Test]
    public void IsTaken_SeesReservedAndTakenNames_CaseAndSpaceInsensitive()
    {
        var roster = new NameRoster();
        roster.Reserve("Calliope Demarch");
        string taken = roster.Take(Pool, First);
        Assert.IsTrue(roster.IsTaken(" calliope demarch"));
        Assert.IsTrue(roster.IsTaken(taken.ToUpperInvariant()));
        Assert.IsFalse(roster.IsTaken("Gaius"));
        Assert.IsFalse(roster.IsTaken(null));
        Assert.IsFalse(roster.IsTaken("  "));
    }

    [Test]
    public void Take_TrimsNames_SkipsBlanks_AndTreatsCaseVariantsAsOne()
    {
        var roster = new NameRoster();
        string[] pool = { " Marcus ", "", null, "MARCUS" };
        Assert.AreEqual("Marcus", roster.Take(pool, First));
        Assert.AreEqual("Marcus II", roster.Take(pool, First));
    }

    [Test]
    public void Take_WithNoUsableNames_ReturnsNull()
    {
        var roster = new NameRoster();
        Assert.IsNull(roster.Take(null, First));
        Assert.IsNull(roster.Take(new string[0], First));
        Assert.IsNull(roster.Take(new[] { " ", null }, First));
    }

    [Test]
    public void Take_ClampsOutOfRangeRandomIndex()
    {
        var roster = new NameRoster();
        Assert.AreEqual("Gaius", roster.Take(Pool, n => 99));
        Assert.AreEqual("Marcus", roster.Take(Pool, n => -3));
    }

    [TestCase(2, "II")]
    [TestCase(4, "IV")]
    [TestCase(9, "IX")]
    [TestCase(14, "XIV")]
    [TestCase(40, "XL")]
    public void Roman_FormatsNumerals(int number, string expected)
    {
        Assert.AreEqual(expected, NameRoster.Roman(number));
    }
}
