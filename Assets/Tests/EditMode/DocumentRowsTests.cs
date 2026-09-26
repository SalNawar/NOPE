using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// The one row source of a document (piece 10): the scanned page's rows and
/// the desk paper's rows come from DocumentRows. The order is page by page,
/// authored order within a page; each row keeps its index in the field list
/// and its place among the rows in the tongue (piece 9's row stagger), across
/// the document for the paper and within the page for the scanned page.
/// </summary>
public class DocumentRowsTests
{
    private static DocumentField F(ClueCategory category, string label, int page = 0) =>
        new DocumentField { category = category, label = label, value = label + " value", page = page };

    /// <summary>The passport: name, birth date, coin, tongue, all on page 0.</summary>
    private static List<DocumentField> Passport() => new List<DocumentField>
    {
        F(ClueCategory.Name, "Full Name"),
        F(ClueCategory.BirthDate, "Date of Birth"),
        F(ClueCategory.Currency, "Coin of Issue"),
        F(ClueCategory.Language, "Native Tongue")
    };

    /// <summary>The permit: Declared Device on page 0, Bond Currency on page 1.</summary>
    private static List<DocumentField> Permit() => new List<DocumentField>
    {
        F(ClueCategory.Technology, "Declared Device", 0),
        F(ClueCategory.Currency, "Bond Currency", 1)
    };

    private static List<string> Labels(IReadOnlyList<DocumentRow> rows)
    {
        var labels = new List<string>();
        foreach (DocumentRow r in rows)
            labels.Add(r.Field.label);
        return labels;
    }

    [Test]
    public void Ordered_IsPageMajorThenAuthored()
    {
        var fields = new List<DocumentField>
        {
            F(ClueCategory.Currency, "b1", 1),
            F(ClueCategory.Name, "a1", 0),
            F(ClueCategory.Language, "b2", 1),
            F(ClueCategory.BirthDate, "a2", 0),
            F(ClueCategory.Technology, "c1", 2)
        };
        CollectionAssert.AreEqual(new[] { "a1", "a2", "b1", "b2", "c1" }, Labels(DocumentRows.Ordered(fields)));
    }

    [Test]
    public void Ordered_SkipsNulls_IndexIsFieldIndex()
    {
        var fields = new List<DocumentField> { null, F(ClueCategory.Currency, "coin", 1), null, F(ClueCategory.Name, "name", 0) };
        IReadOnlyList<DocumentRow> rows = DocumentRows.Ordered(fields);
        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("name", rows[0].Field.label);
        Assert.AreEqual(3, rows[0].Index);
        Assert.AreEqual("coin", rows[1].Field.label);
        Assert.AreEqual(1, rows[1].Index);
        CollectionAssert.IsEmpty(DocumentRows.Ordered(null));
    }

    [Test]
    public void Ordered_TongueRowCountsAcrossDocument()
    {
        IReadOnlyList<DocumentRow> passport = DocumentRows.Ordered(Passport());
        CollectionAssert.AreEqual(new[] { 0, 0, 0, 1 }, new[] { passport[0].TongueRow, passport[1].TongueRow, passport[2].TongueRow, passport[3].TongueRow },
            "the name and the date are not in the tongue and take no place; the coin is the first row in the tongue, the language the second");

        IReadOnlyList<DocumentRow> permit = DocumentRows.Ordered(Permit());
        Assert.AreEqual("Declared Device", permit[0].Field.label);
        Assert.AreEqual(0, permit[0].TongueRow);
        Assert.AreEqual("Bond Currency", permit[1].Field.label);
        Assert.AreEqual(1, permit[1].TongueRow, "across pages on the paper");
    }

    [Test]
    public void OnPage_TongueRowCountsWithinPage()
    {
        IReadOnlyList<DocumentRow> page0 = DocumentRows.OnPage(Permit(), 0);
        IReadOnlyList<DocumentRow> page1 = DocumentRows.OnPage(Permit(), 1);
        Assert.AreEqual(1, page0.Count);
        Assert.AreEqual("Declared Device", page0[0].Field.label);
        Assert.AreEqual(0, page0[0].TongueRow);
        Assert.AreEqual(1, page1.Count);
        Assert.AreEqual("Bond Currency", page1[0].Field.label);
        Assert.AreEqual(0, page1[0].TongueRow, "within its page on the scanned copy (piece 9's rule)");
        Assert.AreEqual(1, page1[0].Index);
    }

    [Test]
    public void OnPage_OtherPagesExcluded()
    {
        var fields = new List<DocumentField> { F(ClueCategory.Name, "a", 0), null, F(ClueCategory.Currency, "b", 1), F(ClueCategory.Language, "c", 0) };
        CollectionAssert.AreEqual(new[] { "a", "c" }, Labels(DocumentRows.OnPage(fields, 0)));
        CollectionAssert.AreEqual(new[] { "b" }, Labels(DocumentRows.OnPage(fields, 1)));
        CollectionAssert.IsEmpty(DocumentRows.OnPage(fields, 2));
        CollectionAssert.IsEmpty(DocumentRows.OnPage(null, 0));
    }

    [Test]
    public void PageCount_EmptyIsOne()
    {
        Assert.AreEqual(1, DocumentRows.PageCount(new List<DocumentField>()));
        Assert.AreEqual(1, DocumentRows.PageCount(null));
        Assert.AreEqual(1, DocumentRows.PageCount(new List<DocumentField> { null }));
    }

    [Test]
    public void PageCount_HighestPagePlusOne()
    {
        Assert.AreEqual(1, DocumentRows.PageCount(Passport()));
        Assert.AreEqual(2, DocumentRows.PageCount(Permit()));
        Assert.AreEqual(4, DocumentRows.PageCount(new List<DocumentField> { F(ClueCategory.Name, "x", 3), F(ClueCategory.Name, "y", 0) }));
    }

    [Test]
    public void HasTongue_Passport_True() => Assert.IsTrue(DocumentRows.HasTongue(Passport()));

    [Test]
    public void HasTongue_NameAndBirthDateOnly_False() =>
        Assert.IsFalse(DocumentRows.HasTongue(new List<DocumentField> { F(ClueCategory.Name, "n"), null, F(ClueCategory.BirthDate, "b") }));

    [Test]
    public void HasTongue_Empty_False()
    {
        Assert.IsFalse(DocumentRows.HasTongue(new List<DocumentField>()));
        Assert.IsFalse(DocumentRows.HasTongue(null));
    }
}
