using System.Linq;
using NUnit.Framework;

/// <summary>Pins (the PC redesign PR1, PR2): pin and unpin, the cap, no duplicates, and the scopes (case items go when the case ends, everything when the day ends).</summary>
public class PinBoardTests
{
    private static EntryRef R(string key)
    {
        Assert.IsTrue(EntryKeys.TryRef(key, out EntryRef r), key);
        return r;
    }

    private static readonly string Field = PickKeys.Field(0, 4);
    private static readonly string Line = PickKeys.Line(2);
    private static readonly string BookRow = PickKeys.BookRow(ClueCategory.Currency, "greece", "ancient");
    private static readonly string Record = EntryKeys.RecordCard("552-1804-33");

    [Test]
    public void Toggle_PinsThenUnpins()
    {
        var pins = new PinBoard(20);
        Assert.AreEqual(PinChange.Pinned, pins.Toggle(R(Field), "Visa · Visa Class"));
        Assert.IsTrue(pins.IsPinned(Field));
        Assert.AreEqual("Visa · Visa Class", pins.Items[0].Label);
        Assert.AreEqual(PinChange.Unpinned, pins.Toggle(R(Field), "Visa · Visa Class"));
        Assert.IsFalse(pins.IsPinned(Field));
        Assert.AreEqual(0, pins.Items.Count);
    }

    [Test]
    public void Pins_KeepTheirOrder_NewestLast()
    {
        var pins = new PinBoard(20);
        pins.Toggle(R(Field), "a");
        pins.Toggle(R(BookRow), "b");
        pins.Toggle(R(Record), "c");
        CollectionAssert.AreEqual(new[] { Field, BookRow, Record }, pins.Items.Select(i => i.Ref.Key).ToArray());
    }

    [Test]
    public void Full_RefusesANewPin_ButStillUnpins()
    {
        var pins = new PinBoard(2);
        pins.Toggle(R(Field), "a");
        pins.Toggle(R(BookRow), "b");
        Assert.AreEqual(PinChange.Full, pins.Toggle(R(Record), "c"));
        Assert.IsFalse(pins.IsPinned(Record));
        Assert.AreEqual(2, pins.Items.Count);
        Assert.AreEqual(PinChange.Unpinned, pins.Toggle(R(Field), "a"));
        Assert.AreEqual(PinChange.Pinned, pins.Toggle(R(Record), "c"));
    }

    [Test]
    public void TheDefaultCap_IsTwenty()
    {
        var pins = new PinBoard(20);
        for (int i = 0; i < 20; i++)
            Assert.AreEqual(PinChange.Pinned, pins.Toggle(R(PickKeys.Line(i)), "line"));
        Assert.AreEqual(PinChange.Full, pins.Toggle(R(PickKeys.Line(20)), "line"));
    }

    [Test]
    public void Unpin_ByKey()
    {
        var pins = new PinBoard(20);
        pins.Toggle(R(Field), "a");
        Assert.IsTrue(pins.Unpin(Field));
        Assert.IsFalse(pins.Unpin(Field));
        Assert.AreEqual(0, pins.Items.Count);
    }

    [Test]
    public void EndCase_DropsCaseItems_KeepsDayItems()
    {
        var pins = new PinBoard(20);
        pins.Toggle(R(Field), "field");
        pins.Toggle(R(BookRow), "book row");
        pins.Toggle(R(Line), "line");
        pins.Toggle(R(Record), "record");
        pins.EndCase();
        CollectionAssert.AreEqual(new[] { BookRow, Record }, pins.Items.Select(i => i.Ref.Key).ToArray());
    }

    [Test]
    public void EndDay_DropsEverything()
    {
        var pins = new PinBoard(20);
        pins.Toggle(R(Field), "field");
        pins.Toggle(R(BookRow), "book row");
        pins.EndDay();
        Assert.AreEqual(0, pins.Items.Count);
    }

    [Test]
    public void ACapBelowOne_CountsAsOne()
    {
        var pins = new PinBoard(0);
        Assert.AreEqual(PinChange.Pinned, pins.Toggle(R(Field), "a"));
        Assert.AreEqual(PinChange.Full, pins.Toggle(R(Line), "b"));
    }
}
