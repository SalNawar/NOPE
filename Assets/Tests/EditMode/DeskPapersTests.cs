using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// One case's papers on the desk and the day-1 desk notes. A paper's state is
/// private, so each test observes it through CanDrag, ScannerBusy,
/// OnDeskCount, HandOver, Drop and Tick. Case under test: a passport handed
/// over on arrival, a permit on request and a letter on arrival, scans of 1.5 s.
/// </summary>
public class DeskPapersTests
{
    private const float Scan = 1.5f;

    private static CaseDocument Doc(string name, DocumentHandOver handOver) =>
        new CaseDocument { name = name, holder = "Nefertari", handOver = handOver };

    private static DeskPapers Papers(float scanSeconds = Scan) => new DeskPapers(new[]
    {
        Doc("Travel Passport", DocumentHandOver.OnArrival),
        Doc("Transit Permit", DocumentHandOver.OnRequest),
        Doc("Letter", DocumentHandOver.OnArrival)
    }, scanSeconds);

    /// <summary>Papers with every paper on the desk.</summary>
    private static DeskPapers AllOnDesk()
    {
        DeskPapers p = Papers();
        for (int i = 0; i < p.Count; i++)
            Assert.IsTrue(p.HandOver(i));
        return p;
    }

    /// <summary>The four states a paper can be in, reached through the public members (public: test-case arguments).</summary>
    public enum State { WithTraveller, OnDesk, Scanning, Returned }

    /// <summary>Papers whose paper 0 is in <paramref name="state"/> and whose scanner is busy or idle (busy with paper 1 unless paper 0 scans).</summary>
    private static DeskPapers InState(State state, bool busy)
    {
        DeskPapers p = Papers();
        if (state != State.WithTraveller)
            Assert.IsTrue(p.HandOver(0));
        Assert.IsTrue(p.HandOver(1));
        if (state == State.Scanning)
            Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        if (busy && state != State.Scanning)
            Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true));
        if (state == State.Returned)
            p.ReturnAll();
        return p;
    }

    [Test]
    public void EveryPaper_StartsWithTheTraveller()
    {
        DeskPapers p = Papers();
        Assert.AreEqual(3, p.Count);
        Assert.AreEqual(0, p.OnDeskCount);
        Assert.IsFalse(p.ScannerBusy);
        for (int i = 0; i < p.Count; i++)
            Assert.IsFalse(p.CanDrag(i));
    }

    [Test]
    public void ArrivalIndices_AreTheOnArrivalPapers_InPaperOrder()
    {
        CollectionAssert.AreEqual(new[] { 0, 2 }, Papers().ArrivalIndices);
        var withANull = new[] { null, Doc("Travel Passport", DocumentHandOver.OnArrival), Doc("Transit Permit", DocumentHandOver.OnRequest) };
        CollectionAssert.AreEqual(new[] { 1 }, CaseDocuments.ArrivalIndices(withANull), "the rule both paths use: a null entry is skipped");
        CollectionAssert.IsEmpty(CaseDocuments.ArrivalIndices(null));
        Assert.IsTrue(DocumentHandOvers.IsRequested(DocumentHandOver.OnRequest));
        Assert.IsFalse(DocumentHandOvers.IsRequested(DocumentHandOver.OnArrival));
        Assert.IsTrue(Doc("x", DocumentHandOver.OnRequest).Requested);
        Assert.IsFalse(Doc("x", DocumentHandOver.OnArrival).Requested);
        Assert.AreEqual(0, (int)DocumentHandOver.OnRequest, "OnRequest is 0, so existing templates read as OnRequest");
        Assert.AreEqual(1, (int)DocumentHandOver.OnArrival);
    }

    [Test]
    public void HandOver_OnlyFromTheTraveller_Once()
    {
        DeskPapers p = Papers();
        Assert.IsTrue(p.HandOver(1));
        Assert.IsTrue(p.CanDrag(1));
        Assert.AreEqual(1, p.OnDeskCount);
        Assert.IsFalse(p.HandOver(1), "a second hand-over fails");

        Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true));
        Assert.IsFalse(p.HandOver(1), "not while scanning");

        p.ReturnAll();
        Assert.IsFalse(p.HandOver(0), "not after the papers went back");
        Assert.IsFalse(p.HandOver(1));
    }

    [Test]
    public void OutOfRangeIndices_GiveFalse_AndRefused()
    {
        DeskPapers p = AllOnDesk();
        foreach (int i in new[] { -1, 3, 99 })
        {
            Assert.IsFalse(p.HandOver(i));
            Assert.IsFalse(p.CanDrag(i));
            Assert.AreEqual(DropOutcome.Refused, p.Drop(i, false));
            Assert.AreEqual(DropOutcome.Refused, p.Drop(i, true));
        }
        Assert.AreEqual(3, p.OnDeskCount);
    }

    [Test]
    public void CanDrag_OnlyOnTheDesk()
    {
        Assert.IsFalse(InState(State.WithTraveller, false).CanDrag(0));
        Assert.IsTrue(InState(State.OnDesk, false).CanDrag(0));
        Assert.IsFalse(InState(State.Scanning, false).CanDrag(0));
        Assert.IsFalse(InState(State.Returned, false).CanDrag(0));
    }

    [TestCase(State.WithTraveller, false, false, DropOutcome.Refused)]
    [TestCase(State.WithTraveller, false, true, DropOutcome.Refused)]
    [TestCase(State.WithTraveller, true, false, DropOutcome.Refused)]
    [TestCase(State.WithTraveller, true, true, DropOutcome.Refused)]
    [TestCase(State.OnDesk, false, false, DropOutcome.Stays)]
    [TestCase(State.OnDesk, false, true, DropOutcome.Stays)]
    [TestCase(State.OnDesk, true, false, DropOutcome.Scanning)]
    [TestCase(State.OnDesk, true, true, DropOutcome.Refused)]
    [TestCase(State.Scanning, false, true, DropOutcome.Refused)]
    [TestCase(State.Scanning, true, true, DropOutcome.Refused)]
    [TestCase(State.Returned, false, false, DropOutcome.Refused)]
    [TestCase(State.Returned, true, false, DropOutcome.Refused)]
    public void Drop_DecisionTable(State state, bool overScanner, bool busy, DropOutcome expected)
    {
        DeskPapers p = InState(state, busy);
        bool couldDrag = p.CanDrag(0);
        bool wasBusy = p.ScannerBusy;
        int onDesk = p.OnDeskCount;

        Assert.AreEqual(expected, p.Drop(0, overScanner));

        if (expected == DropOutcome.Scanning)
        {
            Assert.IsTrue(p.ScannerBusy);
            Assert.IsFalse(p.CanDrag(0), "a scanning paper cannot be dragged");
            Assert.AreEqual(onDesk, p.OnDeskCount, "a scanning paper still counts as on the desk");
        }
        else
        {
            Assert.AreEqual(couldDrag, p.CanDrag(0), "nothing changes for the dropped paper");
            Assert.AreEqual(wasBusy, p.ScannerBusy, "nothing changes for the scanner");
            Assert.AreEqual(onDesk, p.OnDeskCount);
        }
    }

    [Test]
    public void ABusyScanner_KeepsScanningItsPaper_WhileTheRefusedOneStaysDraggable()
    {
        DeskPapers p = AllOnDesk();
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(DropOutcome.Refused, p.Drop(1, true));
        Assert.IsTrue(p.CanDrag(1));
        Assert.AreEqual(-1, p.Tick(1f));
        Assert.AreEqual(0, p.Tick(0.5f), "the first paper's scan still finishes");
    }

    [Test]
    public void AScannedPaper_CanBeScannedAgain()
    {
        DeskPapers p = AllOnDesk();
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(2, true));
        Assert.AreEqual(2, p.Tick(Scan));
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(2, true));
        Assert.AreEqual(2, p.Tick(Scan));
    }

    [Test]
    public void Tick_FinishesAtTheDuration_AndReturnsThePaper()
    {
        DeskPapers p = AllOnDesk();
        Assert.AreEqual(-1, p.Tick(1f), "no scan running");
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true));

        Assert.AreEqual(-1, p.Tick(1f));
        Assert.AreEqual(-1, p.Tick(0f), "zero is ignored");
        Assert.AreEqual(-1, p.Tick(-5f), "negative is ignored");
        Assert.AreEqual(1, p.Tick(0.5f), "exactly at the duration");
        Assert.IsFalse(p.ScannerBusy);
        Assert.IsTrue(p.CanDrag(1), "draggable again once scanned");
        Assert.AreEqual(-1, p.Tick(10f), "no scan running any more");

        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(0, p.Tick(9f), "an overshoot finishes too");
    }

    [Test]
    public void ReturnAll_MidScan_CancelsTheScanForever()
    {
        DeskPapers p = AllOnDesk();
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        p.ReturnAll();

        Assert.IsFalse(p.ScannerBusy);
        Assert.AreEqual(0, p.OnDeskCount);
        for (int i = 0; i < p.Count; i++)
            Assert.IsFalse(p.CanDrag(i));
        Assert.AreEqual(-1, p.Tick(Scan));
        Assert.AreEqual(-1, p.Tick(100f));
    }

    [Test]
    public void OnDeskCount_CountsScanningPapers()
    {
        DeskPapers p = Papers();
        Assert.IsTrue(p.HandOver(0));
        Assert.IsTrue(p.HandOver(2));
        Assert.AreEqual(2, p.OnDeskCount);
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(2, p.OnDeskCount);
    }

    [Test]
    public void AScanTimeOfZero_IsRaisedToOneHundredthOfASecond()
    {
        DeskPapers p = Papers(0f);
        Assert.IsTrue(p.HandOver(0));
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(-1, p.Tick(0.005f), "half of the raised minimum");
        Assert.AreEqual(0, p.Tick(0.005f), "the raised minimum");
    }

    [Test]
    public void ANullDocumentList_MeansNoPapers()
    {
        var p = new DeskPapers(null, Scan);
        Assert.AreEqual(0, p.Count);
        CollectionAssert.IsEmpty(p.ArrivalIndices);
        Assert.IsFalse(p.HandOver(0));
        Assert.AreEqual(-1, p.Tick(1f));
    }

    // -----------------------------
    // DeskHints
    // -----------------------------

    private const string ScanHint = "Drag papers onto the scanner to read them on the PC.";
    private const string WheelHint = "Click the traveller to talk and ask for papers.";

    [Test]
    public void TheScanHint_ShowsOnDayOne_WhileAPaperIsOnTheDesk_UntilTheFirstScan()
    {
        Assert.IsTrue(DeskHints.ScanHintVisible(ScanHint, 1, 1, 0, true));
        Assert.IsFalse(DeskHints.ScanHintVisible(" ", 1, 1, 0, true), "a blank hint");
        Assert.IsFalse(DeskHints.ScanHintVisible(null, 1, 1, 0, true), "no hint");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 2, 1, 0, true), "past the last day");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 1, 1, 1, true), "a scan today");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 1, 1, 0, false), "no paper on the desk");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 1, 0, 0, true), "last day 0: never");
    }

    [Test]
    public void TheWheelHint_ShowsOnDayOne_WhileATravellerIsAtTheDesk_UntilTheWheelFirstOpens()
    {
        Assert.IsTrue(DeskHints.WheelHintVisible(WheelHint, 1, 1, false, true));
        Assert.IsFalse(DeskHints.WheelHintVisible("", 1, 1, false, true), "a blank hint");
        Assert.IsFalse(DeskHints.WheelHintVisible(WheelHint, 2, 1, false, true), "past the last day");
        Assert.IsFalse(DeskHints.WheelHintVisible(WheelHint, 1, 1, true, true), "the wheel was opened today");
        Assert.IsFalse(DeskHints.WheelHintVisible(WheelHint, 1, 1, false, false), "no traveller at the desk");
        Assert.IsFalse(DeskHints.WheelHintVisible(WheelHint, 1, 0, false, true), "last day 0: never");
    }

    [Test]
    public void TheCaseUnderTest_HandsOverTwoPapersOnArrival()
    {
        var arrived = new List<int>();
        DeskPapers p = Papers();
        foreach (int i in p.ArrivalIndices)
            if (p.HandOver(i))
                arrived.Add(i);
        CollectionAssert.AreEqual(new[] { 0, 2 }, arrived);
        Assert.AreEqual(2, p.OnDeskCount);
        Assert.IsFalse(p.CanDrag(1), "the permit waits for its request");
    }
}
