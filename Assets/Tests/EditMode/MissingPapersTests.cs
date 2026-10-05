using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The papers the clerk can flag missing (the desk-first redesign, item 7):
/// the day's papers menu, the same for every traveller, until each paper is
/// handed over; a flag unlocks the request once; a paper asked for and not
/// carried stays NotCarried.
/// </summary>
public class MissingPapersTests
{
    private static readonly AskableForm[] Menu =
    {
        new AskableForm("TC-230", "Entry Ticket", "", true),
        new AskableForm("TC-310", "Stranding Waiver", "", true)
    };

    /// <summary>A passport on arrival, an entry ticket on request, and a card on request the day has not introduced.</summary>
    private static readonly CaseDocument[] Papers =
    {
        new CaseDocument { name = "Passport", handOver = DocumentHandOver.OnArrival, formNumber = "TC-101" },
        new CaseDocument { name = "Entry Ticket", handOver = DocumentHandOver.OnRequest, formNumber = "TC-230" },
        new CaseDocument { name = "Transponder Card", handOver = DocumentHandOver.OnRequest, formNumber = "TC-240" }
    };

    private static MissingPapers For(CaseDocument[] papers) => new MissingPapers(Menu, FormRequests.Build(Menu, papers, null));

    [Test]
    public void TheDaysMenu_WhateverTheTravellerCarries_NeverAPaperNotIntroduced()
    {
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310" }, For(Papers).Requests.Select(r => r.Id).ToArray());
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310" }, For(Papers.Take(1).ToArray()).Requests.Select(r => r.Id).ToArray(), "the same list for a traveller carrying neither");
        CollectionAssert.IsEmpty(MissingPapers.None.Requests);
    }

    [Test]
    public void Open_LeavesOutAPaperHandedOver()
    {
        MissingPapers missing = For(Papers);
        var papers = new CasePapers(3);
        papers.HandOver(0);
        CollectionAssert.AreEqual(new[] { "TC-230", "TC-310" }, missing.Open(papers).Select(r => r.Id).ToArray());
        papers.HandOver(1);
        CollectionAssert.AreEqual(new[] { "TC-310" }, missing.Open(papers).Select(r => r.Id).ToArray(), "the ticket is on the desk");
    }

    [Test]
    public void Flag_Once_ThenNotCarried_Once()
    {
        MissingPapers missing = For(Papers);
        Assert.AreEqual(MissingPaperState.None, missing.State("TC-310"));
        Assert.IsTrue(missing.Flag("TC-310"));
        Assert.IsFalse(missing.Flag("TC-310"), "flagged already");
        Assert.AreEqual(MissingPaperState.Flagged, missing.State("TC-310"));
        Assert.IsTrue(missing.NotCarried("TC-310"));
        Assert.IsFalse(missing.NotCarried("TC-310"));
        Assert.AreEqual(MissingPaperState.NotCarried, missing.State("TC-310"));
        Assert.IsFalse(missing.Flag("TC-240"), "not on the day's menu");
        Assert.IsFalse(missing.Flag(null));
    }
}
