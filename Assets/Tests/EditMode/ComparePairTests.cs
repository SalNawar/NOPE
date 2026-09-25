using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The compare pair (piece 10): the one rule for every surface (the PC's
/// rows, the desk paper's rows, the bubble's answer, a garment). A first pick
/// waits, a second pairs, a pick after a pair starts a new comparison, and a
/// value picked again (the same key, on either surface) clears it. MATCH is
/// decided on each side's CompareEvidence.MatchValue. PickKeys names every
/// pickable value once.
/// </summary>
public class ComparePairTests
{
    private static ComparePick Doc(int doc, int field, string value, ClueCategory c = ClueCategory.Currency) =>
        new ComparePick(PickKeys.Field(doc, field), "Travel Passport · Coin of Issue", value,
                        CompareEvidence.FromDocumentField(new DocumentField { category = c, value = value }));

    private static ComparePick Book(string value, string nation = "greece", string era = "ancient") =>
        new ComparePick(PickKeys.BookRow(ClueCategory.Currency, nation, era), "Currency Ledger · Periclean Athens", value,
                        CompareEvidence.ForReferenceEntry(ClueCategory.Currency, value, nation, era, "Periclean Athens"));

    [Test]
    public void FirstPick_Pending()
    {
        var pair = new ComparePair();
        Assert.IsFalse(pair.HasA);
        Assert.AreEqual(CompareStep.Pending, pair.Select(Doc(0, 2, "Silver drachma (owl)")));
        Assert.IsTrue(pair.HasA);
        Assert.IsFalse(pair.IsPaired);
        Assert.AreEqual(PickKeys.Field(0, 2), pair.A.Key);
    }

    [Test]
    public void SecondPick_Paired()
    {
        var pair = new ComparePair();
        pair.Select(Doc(0, 2, "Silver drachma (owl)"));
        Assert.AreEqual(CompareStep.Paired, pair.Select(Book("Silver drachma (owl)")));
        Assert.IsTrue(pair.IsPaired);
        Assert.AreEqual(PickKeys.Field(0, 2), pair.A.Key);
        Assert.AreEqual(PickKeys.BookRow(ClueCategory.Currency, "greece", "ancient"), pair.B.Key);
        Assert.AreEqual("Silver drachma (owl)", pair.B.Shown);
        Assert.AreEqual(EvidenceKind.ReferenceEntry, pair.B.Evidence.kind);
    }

    [Test]
    public void SameKeyAsA_Clears()
    {
        var pair = new ComparePair();
        pair.Select(Doc(0, 2, "Silver drachma (owl)"));
        Assert.AreEqual(CompareStep.Cleared, pair.Select(Doc(0, 2, "Silver drachma (owl)")), "the same field picked again, on either surface");
        Assert.IsFalse(pair.HasA);
        Assert.IsFalse(pair.IsPaired);
    }

    [Test]
    public void SameKeyAsB_Clears()
    {
        var pair = new ComparePair();
        pair.Select(Doc(0, 2, "a"));
        pair.Select(Book("b"));
        Assert.AreEqual(CompareStep.Cleared, pair.Select(Book("b")));
        Assert.IsFalse(pair.HasA);
        Assert.IsFalse(pair.IsPaired);
    }

    [Test]
    public void ThirdPick_StartsNewWithItAsA()
    {
        var pair = new ComparePair();
        pair.Select(Doc(0, 2, "a"));
        pair.Select(Book("b"));
        Assert.AreEqual(CompareStep.Pending, pair.Select(Doc(1, 1, "c")));
        Assert.IsTrue(pair.HasA);
        Assert.IsFalse(pair.IsPaired);
        Assert.AreEqual(PickKeys.Field(1, 1), pair.A.Key);
    }

    [Test]
    public void BlankKeys_NeverMatchEachOther()
    {
        var pair = new ComparePair();
        var noKey = new ComparePick(null, "x", "a", default);
        var blank = new ComparePick("  ", "y", "a", default);
        pair.Select(noKey);
        Assert.AreEqual(CompareStep.Paired, pair.Select(blank), "two keyless picks pair instead of clearing");
        Assert.AreEqual(CompareStep.Pending, pair.Select(new ComparePick("", "z", "a", default)));
    }

    [Test]
    public void Matches_UsesMatchValue_Garment()
    {
        var pair = new ComparePair();
        pair.Select(new ComparePick(PickKeys.Garment(0), "Traveller · OUTFIT", "Ionic chiton",
                                    CompareEvidence.ForAppearance(Looks.EvidenceCategory, "Chiton and himation", false)));
        pair.Select(new ComparePick(PickKeys.BookRow(Looks.EvidenceCategory, "greece", "ancient"), "Costume Guide · Periclean Athens", "Chiton and himation",
                                    CompareEvidence.ForReferenceEntry(Looks.EvidenceCategory, "Chiton and himation", "greece", "ancient", "Periclean Athens")));
        Assert.IsTrue(pair.Matches, "a garment shows its item but matches on its place's Culture value");
    }

    [Test]
    public void Matches_CaseInsensitiveTrimmed()
    {
        var pair = new ComparePair();
        pair.Select(Doc(0, 2, "  Silver Drachma (OWL) "));
        pair.Select(Book("silver drachma (owl)"));
        Assert.IsTrue(pair.Matches);
    }

    [Test]
    public void Mismatch_Differs()
    {
        var pair = new ComparePair();
        pair.Select(Doc(0, 2, "Silver denarius"));
        pair.Select(Book("Silver drachma (owl)"));
        Assert.IsFalse(pair.Matches);
        var single = new ComparePair();
        single.Select(Doc(0, 2, "x"));
        Assert.IsFalse(single.Matches, "no match before a pair");
    }

    [Test]
    public void Clear_EmptiesBoth()
    {
        var pair = new ComparePair();
        pair.Select(Doc(0, 2, "a"));
        pair.Select(Book("b"));
        pair.Clear();
        Assert.IsFalse(pair.HasA);
        Assert.IsFalse(pair.IsPaired);
        Assert.AreEqual(CompareStep.Pending, pair.Select(Book("b")), "a cleared pair starts again");
    }

    [Test]
    public void PickKeys_Formats()
    {
        Assert.AreEqual("field:0:2", PickKeys.Field(0, 2));
        Assert.AreEqual("line:5", PickKeys.Line(5));
        Assert.AreEqual("garment:1", PickKeys.Garment(1));
        Assert.AreEqual("book:Currency:greece:ancient", PickKeys.BookRow(ClueCategory.Currency, "greece", "ancient"));
        Assert.AreEqual("record:BirthDate", PickKeys.Record(ClueCategory.BirthDate));
    }

    [Test]
    public void PickKeys_SameFieldSameKey()
    {
        Assert.AreEqual(PickKeys.Field(1, 0), PickKeys.Field(1, 0), "the paper's row and the scanned copy's row");
        Assert.AreNotEqual(PickKeys.Field(1, 0), PickKeys.Field(0, 1));
        Assert.AreNotEqual(PickKeys.Field(1, 12), PickKeys.Field(11, 2));
    }

    [Test]
    public void PickKeys_KindsNeverCollide()
    {
        var keys = new HashSet<string>
        {
            PickKeys.Field(1, 1), PickKeys.Line(1), PickKeys.Garment(1),
            PickKeys.BookRow(ClueCategory.Currency, "1", "1"), PickKeys.Record(ClueCategory.Currency),
            PickKeys.BookRow(ClueCategory.Currency, null, null)
        };
        Assert.AreEqual(6, keys.Count);
    }
}
