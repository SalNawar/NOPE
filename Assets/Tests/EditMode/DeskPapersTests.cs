using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// One case's papers on the desk and the day-1 desk notes. A paper's state is
/// private, so each test observes it through CanDrag, ScannerBusy,
/// OnDeskCount, HandOver, Drop, Tick and ZoneOf (the counter and the desk);
/// how a left-click on a paper routes (PaperClicks). Case under test: a
/// passport handed over on arrival, a permit on request and a letter on
/// arrival, scans of 1.5 s.
/// </summary>
public class DeskPapersTests
{
    private const float Scan = 1.5f;

    private static CaseDocument Doc(string name, DocumentHandOver handOver) =>
        new CaseDocument { name = name, handOver = handOver };

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
        Assert.AreEqual(DeskZone.Counter, p.ZoneOf(0));
    }

    // -----------------------------
    // The counter and the desk (Papers, Please's zones, Saleh 2026-10-06)
    // -----------------------------

    [Test]
    public void APaper_ArrivesOnTheCounter_AndTakesTheZoneItIsDroppedIn()
    {
        DeskPapers p = AllOnDesk();
        for (int i = 0; i < p.Count; i++)
            Assert.AreEqual(DeskZone.Counter, p.ZoneOf(i), "papers arrive on the counter, small");
        Assert.AreEqual(DropOutcome.Stays, p.Drop(1, false, DeskZone.Counter), "a paper still on the counter may move along it");
        Assert.AreEqual(DeskZone.Counter, p.ZoneOf(1));
        Assert.AreEqual(DropOutcome.Stays, p.Drop(0, false, DeskZone.Desk));
        Assert.AreEqual(DeskZone.Desk, p.ZoneOf(0), "on the desk it is full size");
        Assert.AreEqual(DeskZone.Counter, p.ZoneOf(-1), "out of range");
    }

    [Test]
    public void ADraggedPaper_GrowsOverTheDesk_AndNeverShrinksBeforeItIsHandedBack()
    {
        Assert.AreEqual(DeskZone.Desk, DeskPapers.ShownWhileDragged(DeskZone.Counter, DeskZone.Desk), "off the counter it grows at once");
        Assert.AreEqual(DeskZone.Counter, DeskPapers.ShownWhileDragged(DeskZone.Counter, DeskZone.Counter), "still over the counter: small");
        Assert.AreEqual(DeskZone.Desk, DeskPapers.ShownWhileDragged(DeskZone.Desk, DeskZone.Counter), "over the counter it keeps its size (Saleh: no shrinking as I stamp)");
    }

    [Test]
    public void APaperFromTheDesk_DroppedOnTheCounter_BeforeTheVerdict_BouncesBack()
    {
        DeskPapers p = AllOnDesk();
        p.Drop(0, false, DeskZone.Desk);
        Assert.AreEqual(DropOutcome.NotStamped, p.Drop(0, false, DeskZone.Counter), "documents can only be returned after stamping");
        Assert.AreEqual(DeskZone.Desk, p.ZoneOf(0), "it stays a desk paper, full size");
        Assert.IsTrue(p.CanDrag(0), "it is still on the desk");
    }

    [Test]
    public void OnceThePassportCarriesItsVerdict_AnyPaperDroppedOnTheCounter_HandsThePapersBack()
    {
        DeskPapers p = AllOnDesk();
        p.Drop(0, false, DeskZone.Desk);
        Assert.AreEqual(DropOutcome.Stays, p.Drop(0, false, DeskZone.Desk, verdict: true), "the stamped passport on the desk stays");
        Assert.AreEqual(DropOutcome.HandsBack, p.Drop(0, false, DeskZone.Counter, verdict: true));
        Assert.AreEqual(DropOutcome.HandsBack, p.Drop(1, false, DeskZone.Counter, verdict: true), "another paper hands them back too");
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(2, true, DeskZone.Counter, verdict: true), "the scanner wins over the counter");
    }

    [Test]
    public void AScan_KeepsThePapersZone()
    {
        DeskPapers p = AllOnDesk();
        p.Drop(1, false, DeskZone.Desk);
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true, DeskZone.Counter));
        Assert.AreEqual(1, p.Tick(Scan));
        Assert.AreEqual(DeskZone.Desk, p.ZoneOf(1), "back from the scan where it lay");
    }

    [Test]
    public void ReturnAll_PutsEveryPaperBackOnTheCounter()
    {
        DeskPapers p = AllOnDesk();
        p.Drop(0, false, DeskZone.Desk);
        p.ReturnAll();
        Assert.AreEqual(DeskZone.Counter, p.ZoneOf(0));
        Assert.IsFalse(p.CanDrag(0));
    }

    // Only left-clicks act; outside inspect mode clicks never compare (Saleh 2026-10-06).
    [TestCase(false, false, DeskZone.Counter, PaperClickAction.Read)]
    [TestCase(false, true, DeskZone.Counter, PaperClickAction.Read)]
    [TestCase(false, false, DeskZone.Desk, PaperClickAction.Front)]
    [TestCase(false, true, DeskZone.Desk, PaperClickAction.Front)]
    [TestCase(true, true, DeskZone.Desk, PaperClickAction.Pick)]
    [TestCase(true, true, DeskZone.Counter, PaperClickAction.Pick)]
    [TestCase(true, false, DeskZone.Desk, PaperClickAction.None)]
    [TestCase(true, false, DeskZone.Counter, PaperClickAction.None)]
    public void PaperClicks_DecisionTable(bool inspecting, bool onRow, DeskZone zone, PaperClickAction expected) =>
        Assert.AreEqual(expected, PaperClicks.Decide(inspecting, onRow, zone));

    // -----------------------------
    // DeskHints
    // -----------------------------

    private const string ScanHint = "Drag a paper onto the scanner to open it on the PC.";
    private const string WheelHint = "Click the traveller to talk and ask for papers.";

    [Test]
    public void TheScanHint_ShowsOnDayOne_WhileAPaperIsOnTheDesk_UntilTheFirstReadOrScan()
    {
        Assert.IsTrue(DeskHints.ScanHintVisible(ScanHint, 1, 1, usesToday: 0, paperOnDesk: true));
        Assert.IsFalse(DeskHints.ScanHintVisible(" ", 1, 1, 0, true), "a blank hint");
        Assert.IsFalse(DeskHints.ScanHintVisible(null, 1, 1, 0, true), "no hint");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 2, 1, 0, true), "past the last day");
        Assert.IsFalse(DeskHints.ScanHintVisible(ScanHint, 1, 1, usesToday: 1, paperOnDesk: true), "a paper read or scanned today");
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

    // -----------------------------
    // The Auto-Feed queue (the PC redesign SC3)
    // -----------------------------

    [Test]
    public void Enqueue_OnlyAHandedOverPaper_Once()
    {
        DeskPapers p = Papers();
        Assert.IsFalse(p.Enqueue(0), "still with the traveller");
        Assert.IsFalse(p.Enqueue(9), "out of range");
        Assert.IsTrue(p.HandOver(0));
        Assert.IsTrue(p.Enqueue(0));
        Assert.IsFalse(p.Enqueue(0), "already queued");
        Assert.AreEqual(1, p.QueuedCount);
        Assert.AreEqual(DropOutcome.Stays, p.Drop(0, false, DeskZone.Desk), "a queued paper still moves");
        Assert.IsTrue(p.HandOver(2));
        Assert.IsTrue(p.Enqueue(2));
        Assert.AreEqual(2, p.QueuedCount);
        Assert.IsFalse(p.ScannerBusy, "queueing scans nothing by itself");
    }

    [Test]
    public void FeedNext_ScansTheQueuedPapers_InHandOverOrder_OneAtATime()
    {
        DeskPapers p = Papers();
        Assert.IsTrue(p.HandOver(2));
        Assert.IsTrue(p.Enqueue(2));
        Assert.IsTrue(p.HandOver(0));
        Assert.IsTrue(p.Enqueue(0));

        Assert.AreEqual(2, p.FeedNext(null), "the paper handed over first goes first");
        Assert.IsTrue(p.ScannerBusy);
        Assert.AreEqual(ScanPass.Plain, p.Pass, "the scanner fed itself");
        Assert.IsFalse(p.CanDrag(2));
        Assert.AreEqual(1, p.QueuedCount);
        Assert.AreEqual(-1, p.FeedNext(null), "one at a time: a busy scanner feeds nothing");
        Assert.AreEqual(2, p.Tick(Scan));
        Assert.AreEqual(0, p.FeedNext(null));
        Assert.AreEqual(0, p.Tick(Scan));
        Assert.AreEqual(-1, p.FeedNext(null), "the queue is empty");
        Assert.AreEqual(0, p.QueuedCount);
        Assert.IsTrue(p.CanDrag(0) && p.CanDrag(2), "both are back on the desk");
    }

    [Test]
    public void FeedNext_WaitsForAPaperTheControllerHoldsBack()
    {
        DeskPapers p = AllOnDesk();
        Assert.IsTrue(p.Enqueue(0));
        Assert.IsTrue(p.Enqueue(1));
        bool landed = false;
        Assert.AreEqual(1, p.FeedNext(i => i != 0 || landed), "a paper still sliding (or dragged) is skipped");
        Assert.AreEqual(1, p.Tick(Scan));
        Assert.AreEqual(-1, p.FeedNext(i => i != 0 || landed));
        landed = true;
        Assert.AreEqual(0, p.FeedNext(i => i != 0 || landed));
    }

    /// <summary>Two papers on the desk, the Analysis Scanner owned or not (its pass takes 3 s, a plain scan Scan).</summary>
    private static DeskPapers Analysing(bool analysis = true)
    {
        var p = new DeskPapers(new[] { Doc("Travel Passport", DocumentHandOver.OnArrival), Doc("Letter", DocumentHandOver.OnArrival) },
                               Scan, 3f, new ScannerDay(false, analysis));
        Assert.IsTrue(p.HandOver(0) && p.HandOver(1));
        return p;
    }

    [Test]
    public void ADropOnTheScanner_ScansByHand_AndTakesThePaperOutOfTheQueue()
    {
        DeskPapers p = Analysing();
        Assert.IsTrue(p.Enqueue(0));
        Assert.IsTrue(p.Enqueue(1));
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true));
        Assert.AreEqual(ScanPass.Analysis, p.Pass, "a scan by hand");
        Assert.AreEqual(1, p.QueuedCount, "the dropped paper is not fed again");
        Assert.AreEqual(-1, p.FeedNext(null), "busy with the hand scan");
        Assert.AreEqual(1, p.Tick(3f));
        Assert.AreEqual(ScanPass.Plain, p.Pass, "idle again");
        Assert.AreEqual(0, p.FeedNext(null));
        Assert.AreEqual(ScanPass.Plain, p.Pass, "the scanner fed itself");
        Assert.AreEqual(DropOutcome.Refused, p.Drop(1, true), "a drop on the busy scanner slides back, as ever");
    }

    [Test]
    public void AHandScan_TakesItsOwnDuration_AFedScanThePlainOne()
    {
        DeskPapers p = Analysing();
        Assert.IsTrue(p.Enqueue(1));
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(-1, p.Tick(Scan), "the analysis pass is longer than the plain scan");
        Assert.AreEqual(0, p.Tick(3f - Scan));
        Assert.AreEqual(1, p.FeedNext(null));
        Assert.AreEqual(1, p.Tick(Scan), "the scanner's own feed keeps the plain duration");

        DeskPapers same = Analysing(false);
        Assert.AreEqual(DropOutcome.Scanning, same.Drop(0, true));
        Assert.AreEqual(ScanPass.Plain, same.Pass);
        Assert.AreEqual(0, same.Tick(Scan), "without the Analysis Scanner a scan by hand is the plain scan");
        Assert.IsFalse(same.WasAnalysed(0));
    }

    [Test]
    public void TheAnalysis_WorksOncePerDocument_AReScanIsPlain()
    {
        DeskPapers p = Analysing();
        Assert.IsFalse(p.WasAnalysed(0));
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(ScanPass.Analysis, p.Pass);
        Assert.IsFalse(p.WasAnalysed(0), "not until the pass ends");
        Assert.AreEqual(0, p.Tick(3f));
        Assert.IsTrue(p.WasAnalysed(0));

        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true), "a scanned paper may be scanned again");
        Assert.AreEqual(ScanPass.AlreadyAnalysed, p.Pass, "Saleh 2026-09-29: the Analysis Scanner only works once per document");
        Assert.AreEqual(0, p.Tick(Scan), "a re-scan takes the plain scan's time");
        Assert.IsTrue(p.WasAnalysed(0), "its one pass stays");

        Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true));
        Assert.AreEqual(ScanPass.Analysis, p.Pass, "another document still takes its pass");
        Assert.IsFalse(p.WasAnalysed(1));
        Assert.AreEqual(1, p.Tick(3f));
        Assert.IsTrue(p.WasAnalysed(1));
        Assert.IsFalse(p.WasAnalysed(-1), "out of range");
        Assert.IsFalse(p.WasAnalysed(2));
    }

    [Test]
    public void AFedScan_NeverAnalyses_SoAHandScanLaterStillCan()
    {
        var p = new DeskPapers(new[] { Doc("Travel Passport", DocumentHandOver.OnArrival) }, Scan, 3f, new ScannerDay(true, true));
        Assert.IsTrue(p.HandOver(0) && p.Enqueue(0));
        Assert.AreEqual(0, p.FeedNext(null));
        Assert.AreEqual(ScanPass.Plain, p.Pass);
        Assert.AreEqual(0, p.Tick(Scan));
        Assert.IsFalse(p.WasAnalysed(0));
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(0, true));
        Assert.AreEqual(ScanPass.Analysis, p.Pass);
    }

    [Test]
    public void ReturnAll_ClearsTheQueue_AndTheRunningPass()
    {
        DeskPapers p = Analysing();
        Assert.IsTrue(p.Enqueue(0));
        Assert.AreEqual(DropOutcome.Scanning, p.Drop(1, true));
        p.ReturnAll();
        Assert.AreEqual(0, p.QueuedCount);
        Assert.AreEqual(ScanPass.Plain, p.Pass);
        Assert.AreEqual(-1, p.FeedNext(null));
    }
}
