using System.Linq;
using NUnit.Framework;

/// <summary>Recent items (the PC redesign PR2): newest first, no duplicates, the cap, and the pins' scopes.</summary>
public class RecentListTests
{
    private static EntryRef R(string key)
    {
        Assert.IsTrue(EntryKeys.TryRef(key, out EntryRef r), key);
        return r;
    }

    private static string[] Keys(RecentList recent) => recent.Items.Select(i => i.Ref.Key).ToArray();

    [Test]
    public void Touch_NewestFirst()
    {
        var recent = new RecentList(10);
        recent.Touch(R(EntryKeys.Document(0)), "Visa");
        recent.Touch(R(EntryKeys.Book(ClueCategory.Currency)), "Currency Ledger");
        CollectionAssert.AreEqual(new[] { "bookof:Currency", "doc:0" }, Keys(recent));
        Assert.AreEqual("Currency Ledger", recent.Items[0].Label);
    }

    [Test]
    public void TouchingAgain_MovesItToTheFront_NoDuplicate()
    {
        var recent = new RecentList(10);
        recent.Touch(R(EntryKeys.Document(0)), "Visa");
        recent.Touch(R(EntryKeys.Document(1)), "Manifest");
        recent.Touch(R(EntryKeys.Document(0)), "Visa (again)");
        CollectionAssert.AreEqual(new[] { "doc:0", "doc:1" }, Keys(recent));
        Assert.AreEqual("Visa (again)", recent.Items[0].Label);
    }

    [Test]
    public void TheCap_DropsTheOldest()
    {
        var recent = new RecentList(10);
        for (int i = 0; i < 12; i++)
            recent.Touch(R(PickKeys.Line(i)), "line " + i);
        Assert.AreEqual(10, recent.Items.Count);
        Assert.AreEqual("line:11", recent.Items[0].Ref.Key);
        Assert.AreEqual("line:2", recent.Items[9].Ref.Key);
    }

    [Test]
    public void EndCase_DropsCaseItems_KeepsDayItems()
    {
        var recent = new RecentList(10);
        recent.Touch(R(EntryKeys.Document(0)), "Visa");
        recent.Touch(R(EntryKeys.RecordCard("552-1804-33")), "Records: Aster Vale");
        recent.Touch(R(PickKeys.Line(3)), "line");
        recent.EndCase();
        CollectionAssert.AreEqual(new[] { "rec:552-1804-33" }, Keys(recent));
    }

    [Test]
    public void EndDay_DropsEverything()
    {
        var recent = new RecentList(10);
        recent.Touch(R(EntryKeys.RecordCard("552-1804-33")), "Records");
        recent.EndDay();
        Assert.AreEqual(0, recent.Items.Count);
    }
}
