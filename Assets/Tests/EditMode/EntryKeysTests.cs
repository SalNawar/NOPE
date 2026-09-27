using NUnit.Framework;

/// <summary>The navigable items' keys (the PC redesign section 4.2): the item keys, each key's source and its scope (PickKeys' row keys read through PickKeys).</summary>
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
    public void ARecordNamedWithAColon_IsStillARecordsItem()
    {
        Assert.IsTrue(EntryKeys.TryRef(PickKeys.Record(ClueCategory.Name, "Ra: Son"), out EntryRef r));
        Assert.AreEqual(AppTab.Records, r.Source);
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
        Assert.IsFalse(EntryKeys.TryBook(key, out _));
        Assert.IsFalse(EntryKeys.TryRecordCard(key, out _));
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
