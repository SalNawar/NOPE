using NUnit.Framework;

/// <summary>
/// The investigation desk's reachability (audit R4-022; the PC redesign RF1): which of the rich desk, the evidence
/// system, the interview, the look at the garments and the desk's papers are reachable from what the builder wired.
/// GameManager generates the day's spoken and dress tells, and gates denials on evidence, from these.
/// </summary>
public class InvestigationWiringTests
{
    /// <summary>Everything wired (the built office), with single parts switched off by name.</summary>
    private static InvestigationWiring Wired(bool documentTemplate = true, bool windowLayer = true, bool accept = true, bool deny = true,
                                             bool compare = true, bool ring = true, bool transcript = true, bool transcriptChrome = true,
                                             bool desk = true, bool records = true) =>
        new InvestigationWiring(documentTemplate, windowLayer, accept, deny, compare, ring, transcript, transcriptChrome, desk, records);

    [Test]
    public void TheBuiltOffice_ReachesEverything_AndWarnsOfNothing()
    {
        InvestigationWiring w = Wired();
        Assert.IsTrue(w.RichMode);
        Assert.IsTrue(w.EvidenceSystemActive);
        Assert.IsTrue(w.InterviewReachable);
        Assert.IsTrue(w.AppearanceReachable);
        Assert.IsTrue(w.DeskReachable);
        Assert.IsFalse(w.RecordsMissing);
        Assert.IsFalse(w.InterviewMissing);
        Assert.IsFalse(w.AppearanceMissing);
        Assert.IsFalse(w.DeskMissing);
    }

    [TestCase(false, true, true, true)]
    [TestCase(true, false, true, true)]
    [TestCase(true, true, false, true)]
    [TestCase(true, true, true, false)]
    public void WithoutAnyRichPart_TheTextFallback_ReadsAnswersAndDress_ButHasNoEvidenceNorDesk(bool documentTemplate, bool windowLayer, bool accept, bool deny)
    {
        InvestigationWiring w = Wired(documentTemplate, windowLayer, accept, deny, ring: false, transcript: false, compare: false);
        Assert.IsFalse(w.RichMode);
        Assert.IsFalse(w.EvidenceSystemActive);
        Assert.IsTrue(w.InterviewReachable, "the fallback prints the answers");
        Assert.IsTrue(w.AppearanceReachable, "the fallback prints the dress");
        Assert.IsFalse(w.DeskReachable);
        Assert.IsFalse(w.InterviewMissing || w.AppearanceMissing || w.DeskMissing || w.RecordsMissing, "the fallback warns of nothing");
    }

    [TestCase(false, true, true)]
    [TestCase(true, false, true)]
    [TestCase(true, true, false)]
    public void TheRichDesk_WithoutTheRing_TheTranscriptOrItsChrome_SpeaksNoTell(bool ring, bool transcript, bool chrome)
    {
        InvestigationWiring w = Wired(ring: ring, transcript: transcript, transcriptChrome: chrome);
        Assert.IsFalse(w.InterviewReachable);
        Assert.IsTrue(w.InterviewMissing);
    }

    [Test]
    public void TheRichDesk_WithoutTheRingOrTheCompare_LeaksNoDress()
    {
        Assert.IsFalse(Wired(ring: false).AppearanceReachable);
        Assert.IsTrue(Wired(ring: false).AppearanceMissing);
        Assert.IsFalse(Wired(compare: false).AppearanceReachable);
        Assert.IsTrue(Wired(compare: false).AppearanceMissing);
        Assert.IsTrue(Wired(transcript: false).AppearanceReachable, "the look needs no transcript");
    }

    [Test]
    public void TheEvidenceSystem_NeedsTheRichDeskAndTheCompare()
    {
        Assert.IsFalse(Wired(compare: false).EvidenceSystemActive);
        Assert.IsTrue(Wired(ring: false, transcript: false, desk: false, records: false).EvidenceSystemActive);
    }

    [Test]
    public void MissingRecords_WarnOnlyWhenTheEvidenceSystemIsActive()
    {
        Assert.IsTrue(Wired(records: false).RecordsMissing);
        Assert.IsFalse(Wired(records: false, compare: false).RecordsMissing);
        Assert.IsTrue(Wired(records: false).EvidenceSystemActive, "records only warn; birth-date tells cannot be proven");
    }

    [Test]
    public void TheDesk_IsReachableOnlyInTheRichDesk()
    {
        Assert.IsFalse(Wired(desk: false).DeskReachable);
        Assert.IsTrue(Wired(desk: false).DeskMissing, "documents then open on the PC at the hand-over");
        Assert.IsFalse(Wired(accept: false).DeskReachable);
        Assert.IsFalse(Wired(accept: false).DeskMissing);
    }
}
