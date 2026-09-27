using System.Collections.Generic;
using NUnit.Framework;

/// <summary>The Investigation app's keyboard regions (the PC redesign KB4): Tab and Shift+Tab walk them in order, skipping the ones that are not there.</summary>
public class AppFocusTests
{
    /// <summary>The regions Tab visits from Search, once round.</summary>
    private static List<AppRegion> Walk(bool split, bool sidebar, bool caseOn, int direction)
    {
        var seen = new List<AppRegion>();
        AppRegion at = AppRegion.Search;
        do
        {
            seen.Add(at);
            at = AppFocus.Next(at, split, sidebar, caseOn, direction);
        }
        while (at != AppRegion.Search && seen.Count < 20);
        return seen;
    }

    [Test]
    public void Everything_InTheSpecsOrder() =>
        CollectionAssert.AreEqual(new[] { AppRegion.Search, AppRegion.TabStrip, AppRegion.PaneHeader, AppRegion.PaneContent, AppRegion.OtherPane,
                                          AppRegion.Sidebar, AppRegion.Dock, AppRegion.Decision },
                                  Walk(true, true, true, 1));

    [Test]
    public void OnePane_SkipsTheOtherPane() =>
        CollectionAssert.DoesNotContain(Walk(false, true, true, 1), AppRegion.OtherPane);

    [Test]
    public void TheSidebarHidden_IsSkipped() =>
        CollectionAssert.AreEqual(new[] { AppRegion.Search, AppRegion.TabStrip, AppRegion.PaneHeader, AppRegion.PaneContent, AppRegion.Dock, AppRegion.Decision },
                                  Walk(false, false, true, 1));

    [Test]
    public void NoCase_SkipsTheDockAndTheDecision() =>
        CollectionAssert.AreEqual(new[] { AppRegion.Search, AppRegion.TabStrip, AppRegion.PaneHeader, AppRegion.PaneContent, AppRegion.Sidebar },
                                  Walk(false, true, false, 1));

    [Test]
    public void ShiftTab_WalksBack()
    {
        CollectionAssert.AreEqual(new[] { AppRegion.Search, AppRegion.Decision, AppRegion.Dock, AppRegion.Sidebar, AppRegion.PaneContent, AppRegion.PaneHeader, AppRegion.TabStrip },
                                  Walk(false, true, true, -1));
    }

    [Test]
    public void TheLast_WrapsToTheFirst()
    {
        Assert.AreEqual(AppRegion.Search, AppFocus.Next(AppRegion.Decision, false, true, true, 1));
        Assert.AreEqual(AppRegion.Decision, AppFocus.Next(AppRegion.Search, false, true, true, -1));
    }

    [Test]
    public void FromARegionNotThere_GoesToTheNextOneThere()
    {
        // The case ended while the ring was on the dock: Tab goes on to the region after it that is there.
        Assert.AreEqual(AppRegion.Search, AppFocus.Next(AppRegion.Dock, false, true, false, 1));
        Assert.AreEqual(AppRegion.Sidebar, AppFocus.Next(AppRegion.Dock, false, true, false, -1));
    }

    [Test]
    public void Available_SaysWhichRegionsAreThere()
    {
        Assert.IsTrue(AppFocus.Available(AppRegion.Search, false, false, false));
        Assert.IsFalse(AppFocus.Available(AppRegion.OtherPane, false, true, true));
        Assert.IsTrue(AppFocus.Available(AppRegion.OtherPane, true, true, true));
        Assert.IsFalse(AppFocus.Available(AppRegion.Sidebar, true, false, true));
        Assert.IsFalse(AppFocus.Available(AppRegion.Decision, true, true, false));
    }
}
