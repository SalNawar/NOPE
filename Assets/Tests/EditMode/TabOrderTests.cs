using System;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Investigation app's tabs (the PC redesign AP1, AP4, AP5, AP8): the
/// default order, one tab per source, which tabs are the case's (their views
/// show the no-case state between travellers) and which the day's (they still
/// work), and the player's order: moved, reset, dragged past a neighbour's
/// middle, saved and read back.
/// </summary>
public class TabOrderTests
{
    [Test]
    public void Default_IsDocumentsRecordsReferenceTranscriptReportRules()
    {
        CollectionAssert.AreEqual(new[] { AppTab.Documents, AppTab.Records, AppTab.Reference, AppTab.Transcript, AppTab.Report, AppTab.Rules },
                                  TabOrder.Default.ToArray());
    }

    [Test]
    public void Default_HoldsEveryTabOnce()
    {
        CollectionAssert.AreEquivalent(Enum.GetValues(typeof(AppTab)).Cast<AppTab>().ToArray(), TabOrder.Default.ToArray());
        CollectionAssert.AllItemsAreUnique(TabOrder.Default.ToArray());
    }

    [Test]
    public void Tabs_KeepTheirValues()
    {
        Assert.AreEqual(0, (int)AppTab.Documents);
        Assert.AreEqual(1, (int)AppTab.Records);
        Assert.AreEqual(2, (int)AppTab.Reference);
        Assert.AreEqual(3, (int)AppTab.Transcript);
        Assert.AreEqual(4, (int)AppTab.Report);
        Assert.AreEqual(5, (int)AppTab.Rules);
        Assert.AreEqual(6, Enum.GetValues(typeof(AppTab)).Length, "a new tab is appended here too");
    }

    [TestCase(AppTab.Documents, true)]
    [TestCase(AppTab.Transcript, true)]
    [TestCase(AppTab.Report, true)]
    [TestCase(AppTab.Records, false)]
    [TestCase(AppTab.Reference, false)]
    [TestCase(AppTab.Rules, false)]
    public void CaseSources_AreTheCasesPapersInterviewAndReport(AppTab tab, bool caseSource)
    {
        Assert.AreEqual(caseSource, TabOrder.IsCaseSource(tab));
    }

    [Test]
    public void NewOrder_IsTheDefault()
    {
        var order = new TabOrder();
        CollectionAssert.AreEqual(TabOrder.Default.ToArray(), order.Tabs.ToArray());
        Assert.AreEqual(2, order.PositionOf(AppTab.Reference));
        Assert.AreEqual(5, order.PositionOf(AppTab.Rules));
    }

    [Test]
    public void Move_TakesTheTabOut_AndTheOthersCloseUp()
    {
        var order = new TabOrder();
        Assert.IsTrue(order.Move(0, 5));
        CollectionAssert.AreEqual(new[] { AppTab.Records, AppTab.Reference, AppTab.Transcript, AppTab.Report, AppTab.Rules, AppTab.Documents },
                                  order.Tabs.ToArray());
        Assert.IsTrue(order.Move(4, 1));
        CollectionAssert.AreEqual(new[] { AppTab.Records, AppTab.Rules, AppTab.Reference, AppTab.Transcript, AppTab.Report, AppTab.Documents },
                                  order.Tabs.ToArray());
    }

    [Test]
    public void Move_OutOfTheStrip_IsClampedOrIgnored()
    {
        var order = new TabOrder();
        Assert.IsFalse(order.Move(0, -1), "Move left on the first tab does nothing");
        Assert.IsFalse(order.Move(5, 9), "Move right on the last tab does nothing");
        Assert.IsFalse(order.Move(-1, 2));
        Assert.IsFalse(order.Move(6, 2));
        CollectionAssert.AreEqual(TabOrder.Default.ToArray(), order.Tabs.ToArray());
        Assert.IsTrue(order.Move(1, 40));
        Assert.AreEqual(5, order.PositionOf(AppTab.Records), "a move past the end goes last");
    }

    [Test]
    public void Positions_FollowTheOrder_NotTheNames()
    {
        var order = new TabOrder();
        order.Move(5, 0);
        Assert.AreEqual(AppTab.Rules, order.Tabs[0], "the first position");
        Assert.AreEqual(AppTab.Documents, order.Tabs[1]);
        Assert.AreEqual(0, order.PositionOf(AppTab.Rules));
        Assert.AreEqual(1, order.PositionOf(AppTab.Documents));
    }

    [Test]
    public void Reset_RestoresTheDefault()
    {
        var order = new TabOrder();
        Assert.IsFalse(order.Reset(), "already the default");
        order.Move(2, 0);
        Assert.IsTrue(order.Reset());
        CollectionAssert.AreEqual(TabOrder.Default.ToArray(), order.Tabs.ToArray());
    }

    [Test]
    public void Save_ThenParse_KeepsTheOrder()
    {
        var order = new TabOrder();
        order.Move(3, 0);
        order.Move(5, 2);
        string saved = order.Save();
        Assert.AreEqual("Transcript,Documents,Rules,Records,Reference,Report", saved);
        CollectionAssert.AreEqual(order.Tabs.ToArray(), TabOrder.Parse(saved).Tabs.ToArray());
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("  ")]
    [TestCase("Nonsense,3,documents")]
    public void Parse_NothingUsable_IsTheDefault(string saved)
    {
        CollectionAssert.AreEqual(TabOrder.Default.ToArray(), TabOrder.Parse(saved).Tabs.ToArray());
    }

    [Test]
    public void Parse_SkipsUnknownAndRepeatedNames_AndAppendsTheMissingInDefaultOrder()
    {
        TabOrder order = TabOrder.Parse(" Rules ,Gone,Report,Rules,Documents");
        CollectionAssert.AreEqual(new[] { AppTab.Rules, AppTab.Report, AppTab.Documents, AppTab.Records, AppTab.Reference, AppTab.Transcript },
                                  order.Tabs.ToArray());
    }

    [Test]
    public void DragTarget_PassingANeighboursMiddle_TakesItsPlace()
    {
        float[] middles = { 50f, 150f, 250f, 350f, 450f, 550f };
        Assert.AreEqual(1, TabOrder.DragTarget(middles, 1, 150f), "not moved");
        Assert.AreEqual(1, TabOrder.DragTarget(middles, 1, 249f), "short of the right neighbour's middle");
        Assert.AreEqual(2, TabOrder.DragTarget(middles, 1, 251f), "past it");
        Assert.AreEqual(5, TabOrder.DragTarget(middles, 1, 900f), "past the end: last");
        Assert.AreEqual(1, TabOrder.DragTarget(middles, 1, 51f), "short of the left neighbour's middle");
        Assert.AreEqual(0, TabOrder.DragTarget(middles, 1, 49f), "past it: first");
        Assert.AreEqual(0, TabOrder.DragTarget(middles, 4, -20f));
    }
}
