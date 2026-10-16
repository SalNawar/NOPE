using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The desk rulebook's RULES page split into sheets (leftover 2026-10-08: day 12's six rules were unreadably small at 720p).</summary>
public class RuleSheetsTests
{
    [Test]
    public void Starts_OneSheetWhenEverythingFits()
    {
        CollectionAssert.AreEqual(new List<int> { 0 }, RuleSheets.Starts(3, 6, (s, c) => true));
        CollectionAssert.AreEqual(new List<int> { 0 }, RuleSheets.Starts(0, 6, (s, c) => true));
    }

    [Test]
    public void Starts_TheRulesThatDoNotFitSpillToTheNextSheet()
    {
        // At most four lines' worth a sheet, each rule two lines: two rules a sheet.
        CollectionAssert.AreEqual(new List<int> { 0, 2, 4 }, RuleSheets.Starts(6, 6, (s, c) => c * 2 <= 4));
        // Never more than the rows a sheet has.
        CollectionAssert.AreEqual(new List<int> { 0, 6 }, RuleSheets.Starts(8, 6, (s, c) => true));
    }

    [Test]
    public void Starts_ARuleTooLongForASheetStillGetsOneOfItsOwn()
    {
        CollectionAssert.AreEqual(new List<int> { 0, 1, 2 }, RuleSheets.Starts(3, 6, (s, c) => false));
    }
}
