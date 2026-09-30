using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The Investigation app's keyboard regions (the PC redesign KB4; the PC workbench spec section 7): Tab and Shift+Tab walk them in order, skipping the ones that are not there.</summary>
public class AppFocusTests
{
    /// <summary>The regions Tab visits from <paramref name="start"/>, once round.</summary>
    private static List<AppRegion> Walk(AppFocusState state, int direction, AppRegion start = AppRegion.Steps)
    {
        var seen = new List<AppRegion>();
        AppRegion at = start;
        do
        {
            seen.Add(at);
            at = AppFocus.Next(at, state, direction);
        }
        while (at != start && seen.Count < 20);
        return seen;
    }

    private static readonly AppFocusState Everything = new AppFocusState(split: true, findings: true, caseOn: true, holding: true);

    [Test]
    public void Everything_InTheSpecsOrder() =>
        CollectionAssert.AreEqual(new[] { AppRegion.Steps, AppRegion.Shelf, AppRegion.PaneContent, AppRegion.OtherPane, AppRegion.Findings, AppRegion.Holding },
                                  Walk(Everything, 1));

    [Test]
    public void TheDecision_ReplacesThePanes()
    {
        var deciding = new AppFocusState(split: true, findings: true, caseOn: true, deciding: true);
        CollectionAssert.AreEqual(new[] { AppRegion.Steps, AppRegion.Shelf, AppRegion.Findings, AppRegion.Decision }, Walk(deciding, 1));
        Assert.AreEqual(AppRegion.Steps, AppFocus.Home(deciding));
        Assert.IsFalse(AppFocus.Available(AppRegion.Decision, new AppFocusState(caseOn: false, deciding: true)), "no traveller: nothing to decide");
    }

    [Test]
    public void TheDrawerOpen_OnlyItsFieldAndHits()
    {
        var drawer = new AppFocusState(drawer: true, results: true, split: true, caseOn: true);
        CollectionAssert.AreEqual(new[] { AppRegion.Search, AppRegion.Results }, Walk(drawer, 1, AppRegion.Search));
        Assert.AreEqual(AppRegion.Search, AppFocus.Home(drawer));
        CollectionAssert.AreEqual(new[] { AppRegion.Search }, Walk(new AppFocusState(drawer: true), 1, AppRegion.Search), "no hits: the field alone");
        Assert.IsFalse(AppFocus.Available(AppRegion.Search, new AppFocusState()), "the drawer closed: no search region");
    }

    [Test]
    public void OnePane_NothingHeld_FindingsHidden_AreSkipped() =>
        CollectionAssert.AreEqual(new[] { AppRegion.Steps, AppRegion.Shelf, AppRegion.PaneContent },
                                  Walk(new AppFocusState(findings: false, caseOn: true), 1));

    [Test]
    public void ShiftTab_WalksBack() =>
        CollectionAssert.AreEqual(new[] { AppRegion.Steps, AppRegion.Holding, AppRegion.Findings, AppRegion.OtherPane, AppRegion.PaneContent, AppRegion.Shelf },
                                  Walk(Everything, -1));

    [Test]
    public void FromARegionNotThere_GoesToTheNextOneThere()
    {
        // The value was let go while the ring was on its Cancel: Tab goes on to the region after it that is there.
        var released = new AppFocusState(split: false, findings: true, caseOn: true);
        Assert.AreEqual(AppRegion.Steps, AppFocus.Next(AppRegion.Holding, released, 1));
        Assert.AreEqual(AppRegion.Findings, AppFocus.Next(AppRegion.Holding, released, -1));
        Assert.AreEqual(AppRegion.PaneContent, AppFocus.Home(released));
    }
}
