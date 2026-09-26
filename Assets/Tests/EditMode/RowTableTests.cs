using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>The row-table layer's CSV: UTF-8, quoting, embedded line breaks, spreadsheet row numbers, blank rows skipped.</summary>
public class RowTableTests
{
    /// <summary>Whether two tables hold the same headers and cells (names and row numbers aside).</summary>
    internal static bool SameCells(RowTable a, RowTable b) =>
        a.Headers.SequenceEqual(b.Headers) && a.Rows.Count == b.Rows.Count && a.Rows.Zip(b.Rows, (x, y) => x.SequenceEqual(y)).All(same => same);

    [Test]
    public void Read_QuotedCellsAndRowNumbers()
    {
        var errors = new List<string>();
        RowTable t = Csv.Read("s", "id,text\r\n1,\"a, \"\"b\"\"\nc\"\r\n,\r\n2,plain\n", errors);
        CollectionAssert.IsEmpty(errors);
        CollectionAssert.AreEqual(new[] { "id", "text" }, t.Headers);
        Assert.AreEqual(2, t.Rows.Count);
        CollectionAssert.AreEqual(new[] { "1", "a, \"b\"\nc" }, t.Rows[0]);
        CollectionAssert.AreEqual(new[] { "2", "plain" }, t.Rows[1]);
        CollectionAssert.AreEqual(new[] { 2, 4 }, t.RowNumbers);
    }

    [Test]
    public void Read_StripsTheBom_AndSniffsSemicolons()
    {
        var errors = new List<string>();
        RowTable t = Csv.Read("s", "﻿id;text\n1;a,b\n", errors);
        CollectionAssert.IsEmpty(errors);
        CollectionAssert.AreEqual(new[] { "id", "text" }, t.Headers);
        CollectionAssert.AreEqual(new[] { "1", "a,b" }, t.Rows[0]);
    }

    [Test]
    public void Read_PadsShortRows_AndWidensForExtraCells()
    {
        var errors = new List<string>();
        RowTable t = Csv.Read("s", "a,b\n1\n2,3,4\n", errors);
        CollectionAssert.IsEmpty(errors);
        CollectionAssert.AreEqual(new[] { "a", "b", "" }, t.Headers);
        CollectionAssert.AreEqual(new[] { "1", "", "" }, t.Rows[0]);
        CollectionAssert.AreEqual(new[] { "2", "3", "4" }, t.Rows[1]);
    }

    [Test]
    public void Read_UnterminatedQuote_IsAnError()
    {
        var errors = new List<string>();
        Csv.Read("people", "a,b\n1,\"open\n", errors);
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains("people", errors[0]);
        StringAssert.Contains("row 2", errors[0]);
    }

    [Test]
    public void Write_QuotesOnlyWhatNeedsIt_AndReadsBack()
    {
        var t = new RowTable("s", new[] { "id", "text" });
        t.Add(new[] { "1", "plain" });
        t.Add(new[] { "2", "comma, \"quote\"\nbreak" });
        t.Add(new[] { "3", " lead" });
        string csv = Csv.Write(t);
        StringAssert.StartsWith("id,text\r\n1,plain\r\n2,\"comma, \"\"quote\"\"\nbreak\"\r\n3, lead\r\n", csv);

        var errors = new List<string>();
        RowTable back = Csv.Read("s", csv, errors);
        CollectionAssert.IsEmpty(errors);
        Assert.IsTrue(SameCells(t, back));
    }
}
