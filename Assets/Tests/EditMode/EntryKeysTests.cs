using NUnit.Framework;

/// <summary>The navigable items' keys (the PC redesign section 4.2): the item keys, reading back PickKeys' row keys, each key's source and its scope.</summary>
public class EntryKeysTests
{
    [Test]
    public void TheItemKeys()
    {
        Assert.AreEqual("doc:2", EntryKeys.Document(2));
        Assert.AreEqual("bookof:Currency", EntryKeys.Book(ClueCategory.Currency));
        Assert.AreEqual("rec:552-1804-33", EntryKeys.RecordCard("552-1804-33"));
    }

    [Test]
    public void ItemKeys_ReadBack()
    {
        Assert.IsTrue(EntryKeys.TryDocument("doc:2", out int doc));
        Assert.AreEqual(2, doc);
        Assert.IsTrue(EntryKeys.TryBook("bookof:Culture", out ClueCategory book));
        Assert.AreEqual(ClueCategory.Culture, book);
        Assert.IsTrue(EntryKeys.TryRecordCard("rec:Aster Vale", out string card));
        Assert.AreEqual("Aster Vale", card);
    }

    [Test]
    public void PickKeysRows_ReadBack()
    {
        Assert.IsTrue(EntryKeys.TryField(PickKeys.Field(1, 7), out int d, out int f));
        Assert.AreEqual((1, 7), (d, f));
        Assert.IsTrue(EntryKeys.TryLine(PickKeys.Line(12), out int line));
        Assert.AreEqual(12, line);
        Assert.IsTrue(EntryKeys.TryBookRow(PickKeys.BookRow(ClueCategory.Currency, "greece", "ancient"), out ClueCategory c, out string nation, out string era));
        Assert.AreEqual((ClueCategory.Currency, "greece", "ancient"), (c, nation, era));
        Assert.IsTrue(EntryKeys.TryRecordRow(PickKeys.Record(ClueCategory.BirthDate, "552-1804-33"), out string id, out ClueCategory row));
        Assert.AreEqual(("552-1804-33", ClueCategory.BirthDate), (id, row));
    }

    [Test]
    public void ARecordNamedWithAColon_StillReadsBack()
    {
        Assert.IsTrue(EntryKeys.TryRecordRow(PickKeys.Record(ClueCategory.Name, "Ra: Son"), out string id, out ClueCategory row));
        Assert.AreEqual(("Ra: Son", ClueCategory.Name), (id, row));
    }

    [TestCase("")]
    [TestCase(null)]
    [TestCase("doc:")]
    [TestCase("doc:x")]
    [TestCase("field:1")]
    [TestCase("field:a:b")]
    [TestCase("line:-1")]
    [TestCase("bookof:NotACategory")]
    [TestCase("book:Currency:greece")]
    [TestCase("record::Name")]
    [TestCase("rec:")]
    [TestCase("garment:1")]
    public void Malformed_ReadAsNothing(string key)
    {
        Assert.IsFalse(EntryKeys.TryDocument(key, out _));
        Assert.IsFalse(EntryKeys.TryField(key, out _, out _));
        Assert.IsFalse(EntryKeys.TryLine(key, out _));
        Assert.IsFalse(EntryKeys.TryBook(key, out _));
        Assert.IsFalse(EntryKeys.TryBookRow(key, out _, out _, out _));
        Assert.IsFalse(EntryKeys.TryRecordCard(key, out _));
        Assert.IsFalse(EntryKeys.TryRecordRow(key, out _, out _));
        Assert.IsFalse(EntryKeys.TryRef(key, out _));
    }

    [Test]
    public void EachKey_ItsSourceAndScope()
    {
        AssertRef(EntryKeys.Document(0), AppTab.Documents, EntryScope.Case);
        AssertRef(PickKeys.Field(0, 3), AppTab.Documents, EntryScope.Case);
        AssertRef(PickKeys.Line(4), AppTab.Transcript, EntryScope.Case);
        AssertRef(EntryKeys.Book(ClueCategory.Culture), AppTab.Reference, EntryScope.Day);
        AssertRef(PickKeys.BookRow(ClueCategory.Culture, "egypt", "ancient"), AppTab.Reference, EntryScope.Day);
        AssertRef(EntryKeys.RecordCard("552-1804-33"), AppTab.Records, EntryScope.Day);
        AssertRef(PickKeys.Record(ClueCategory.BirthDate, "552-1804-33"), AppTab.Records, EntryScope.Day);
    }

    [Test]
    public void AGarment_IsNotAnAppItem() => Assert.IsFalse(EntryKeys.TryRef(PickKeys.Garment(0), out _));

    private static void AssertRef(string key, AppTab source, EntryScope scope)
    {
        Assert.IsTrue(EntryKeys.TryRef(key, out EntryRef r), key);
        Assert.AreEqual(key, r.Key);
        Assert.AreEqual(source, r.Source, key);
        Assert.AreEqual(scope, r.Scope, key);
    }
}
