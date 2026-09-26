using System;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The Investigation app's tabs (the PC redesign AP1, AP5, AP8): the default
/// order, one tab per source, and which tabs are the case's (their views show
/// the no-case state between travellers) and which the day's (they still work).
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
}
