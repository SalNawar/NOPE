using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The desk machines spec §1: what the reader and the torch have read
/// (DeskReadings), the scan tabs as the machines reveal them
/// (MachineLayers), the reader's cycle and readout (ChipReaderCycle), the
/// torch's cone (UvCone), and a machine's reading as evidence
/// (DiscrepancyLog's hidden proof).
/// </summary>
public class DeskMachinesTests
{
    private static DocumentField F(ClueCategory c, string value, string erased = "") => new DocumentField { category = c, value = value, label = c.ToString(), erased = erased };

    private static List<DocumentField> Passport(string idErased = "") => new List<DocumentField>
    {
        F(ClueCategory.Name, "Pell Quimby"), F(ClueCategory.CitizenId, "NHA-512", idErased), F(ClueCategory.Photo, "a1-b2"), F(ClueCategory.Seal, "Blue hexagon · VO")
    };

    private static CaseHidden Hidden(string idErased = "") =>
        HiddenLayers.Build(new[] { new HiddenPaper("TC-101", Passport(idErased)), new HiddenPaper("TC-230", new List<DocumentField> { F(ClueCategory.CitizenId, "NHA-512") }) },
                           null, new HiddenContent { chipForms = new List<string> { "TC-101" }, ghostChance = 1f }, new SeededRandom(1));

    [Test]
    public void Readings_EachOnce_RaisingChanged_AndForgottenAtANewCase()
    {
        var readings = new DeskReadings();
        var changed = new List<int>();
        readings.Changed += changed.Add;
        Assert.IsTrue(readings.ReadChip(0));
        Assert.IsFalse(readings.ReadChip(0));
        Assert.IsTrue(readings.See(1, 2));
        Assert.IsFalse(readings.See(1, 2));
        Assert.IsTrue(readings.See(1, 0));
        CollectionAssert.AreEqual(new[] { 0, 1, 1 }, changed);
        CollectionAssert.AreEqual(new[] { 0, 2 }, readings.SeenMarks(1));
        readings.Clear();
        Assert.IsFalse(readings.ChipRead(0));
        CollectionAssert.IsEmpty(readings.SeenMarks(1));
    }

    [Test]
    public void MachineLayers_HiddenBeforeTheirDay_ThenOnlyWhatTheMachinesRead()
    {
        CaseHidden hidden = Hidden("NHA-777");
        var readings = new DeskReadings();
        var before = new MachineLayers(hidden, readings, false, false);
        Assert.AreEqual(LayerState.Unavailable, before.UvState(0));
        Assert.AreEqual(LayerState.Unavailable, before.ChipState(0));
        CollectionAssert.IsEmpty(before.Uv(0));
        Assert.IsNull(before.Chip(0));

        var after = new MachineLayers(hidden, readings, true, true);
        Assert.AreEqual(LayerState.NotYet, after.UvState(0));
        Assert.AreEqual(LayerState.NotYet, after.ChipState(0));
        Assert.AreEqual(LayerState.None, after.ChipState(1), "the ticket carries no chip");
        Assert.IsNull(after.Chip(0), "not read yet");

        readings.ReadChip(0);
        readings.See(0, 0);
        Assert.AreEqual(LayerState.Shown, after.ChipState(0));
        Assert.AreEqual("NHA-777", after.Chip(0)[1].value, "the chip holds the issued Citizen ID");
        Assert.AreEqual(LayerState.Shown, after.UvState(0));
        Assert.AreEqual(UvMarkKind.Watermark, after.Uv(0).Single().Kind, "only the marks seen");
        CollectionAssert.AreEqual(new[] { 0 }, after.SeenMarks(0));
    }

    [Test]
    public void Cycle_InsertReadResultEject_ThenIdle_OneReadAtATime()
    {
        var reader = new ChipReaderCycle(0.25f, 1f, 1.5f, 0.3f);
        Assert.IsFalse(reader.Busy);
        CaseHidden hidden = Hidden("NHA-777");
        Assert.IsTrue(reader.Begin(0, Passport("NHA-777"), hidden.ChipOf(0)));
        Assert.IsFalse(reader.Begin(1, Passport(), null), "busy");
        Assert.AreEqual(ChipReaderStep.Insert, reader.Step);
        Assert.AreEqual(ChipOutcome.Mismatch, reader.Outcome);
        ChipLine line = reader.Lines.Single();
        Assert.AreEqual((1, ClueCategory.CitizenId, "NHA-777"), (line.Field, line.Category, line.Chip));

        Assert.IsNull(reader.Tick(0.1f));
        Assert.AreEqual(0.4f, reader.Progress, 1e-4f);
        Assert.AreEqual(ChipReaderStep.Reading, reader.Tick(0.2f));
        Assert.AreEqual(ChipReaderStep.Result, reader.Tick(1f));
        Assert.AreEqual(ChipReaderStep.Idle, reader.Tick(5f), "a long frame passes Eject too");
        Assert.IsFalse(reader.Busy);
        Assert.AreEqual(-1, reader.Document);

        Assert.IsTrue(reader.Begin(1, Passport(), null));
        Assert.AreEqual(ChipOutcome.NoChip, reader.Outcome);
        reader.Cancel();
        Assert.IsFalse(reader.Busy);
        Assert.IsTrue(reader.Begin(0, Passport(), Hidden().ChipOf(0)));
        Assert.AreEqual(ChipOutcome.Match, reader.Outcome, "an honest chip, the photo and seal never read");
    }

    [Test]
    public void Cone_CoversABoxByShare_AndAMarkIsSeenAfterAQuarterSecondLit()
    {
        Assert.AreEqual(1f, UvCone.Covered(0f, 0f, 0.04f, 0.01f, 0f, 0f, 0.1f));
        Assert.AreEqual(0f, UvCone.Covered(0.5f, 0f, 0.04f, 0.01f, 0f, 0f, 0.1f));
        float half = UvCone.Covered(0.1f, 0f, 0.04f, 0.01f, 0f, 0f, 0.1f);
        Assert.That(half, Is.InRange(0.3f, 0.7f), "a box across the edge is partly lit");

        float lit = 0f;
        lit = UvCone.Lit(lit, 1f, true, 0.2f);
        Assert.IsFalse(UvCone.IsSeen(lit));
        lit = UvCone.Lit(lit, 1f, true, 0.1f);
        Assert.IsTrue(UvCone.IsSeen(lit));
        Assert.AreEqual(0f, UvCone.Lit(lit, 0.5f, true, 0.1f), "out of the cone: it starts over");
        Assert.AreEqual(0f, UvCone.Lit(lit, 1f, false, 0.1f), "the torch off: nothing lit");
    }

    [Test]
    public void Cone_LandsAheadOfTheGrip_AlongTheTorch()
    {
        (float x, float z) = UvCone.Ahead(0.2f, 0.1f, 0f, 3f, 0.1f, 45f);
        Assert.AreEqual(0.2f, x, 1e-5f, "straight ahead: no sideways drift");
        Assert.AreEqual(0.2f, z, 1e-5f, "height / tan(45) = 0.1 ahead of the grip");
        (x, _) = UvCone.Ahead(0f, 0f, 1f, 0f, 0.1f, 30f);
        Assert.AreEqual(0.1f / (float)System.Math.Tan(System.Math.PI / 6), x, 1e-5f, "a shallower tilt reaches further");
        Assert.AreEqual(UvCone.Ahead(0f, 0f, 1f, 0f, 0.1f, UvCone.MaxTilt), UvCone.Ahead(0f, 0f, 1f, 0f, 0.1f, 89.9f), "the tilt is kept under MaxTilt");
        Assert.AreEqual((0.3f, -0.2f), UvCone.Ahead(0.3f, -0.2f, 0f, 0f, 0.1f, 45f), "no away direction: under the grip");
    }

    [Test]
    public void AMachinesFault_ProvesItsOwnPapersBox_NeverAnotherPaperOrAnHonestReading()
    {
        CompareEvidence print = CompareEvidence.FromDocumentField(F(ClueCategory.CitizenId, "NHA-512"), 0);
        Discrepancy proof = DiscrepancyLog.Prove(print, CompareEvidence.ForHidden(ClueCategory.CitizenId, "NHA-777", 0, true), "x", "y", "Pell Quimby");
        Assert.AreEqual(DiscrepancyProof.HiddenMismatch, proof.provedBy);
        Assert.AreEqual(("NHA-512", "NHA-777", 0), (proof.documentValue, proof.expectedValue, proof.statementDocument));
        Assert.AreEqual("deviation.hiddenMismatch.papers", proof.ReportKey);
        Assert.AreEqual(FindingKind.Differs, FindingRules.Classify(print, CompareEvidence.ForHidden(ClueCategory.CitizenId, "NHA-777", 0, true), proof, "x", "y", "Pell Quimby"));

        Assert.IsNull(DiscrepancyLog.Prove(print, CompareEvidence.ForHidden(ClueCategory.CitizenId, "NHA-777", 1, true), "x", "y", "P"), "another paper's chip");
        Assert.IsNull(DiscrepancyLog.Prove(print, CompareEvidence.ForHidden(ClueCategory.CitizenId, "NHA-512", 0, false), "x", "y", "P"), "an honest chip line");
        Assert.IsNull(DiscrepancyLog.Prove(print, CompareEvidence.ForHidden(ClueCategory.Name, "Orla Venn", 0, true), "x", "y", "P"), "another box");
        Assert.IsNull(DiscrepancyLog.Prove(CompareEvidence.ForHidden(ClueCategory.Name, "a", 0, true), CompareEvidence.ForHidden(ClueCategory.Name, "b", 0, true), "x", "y", "P"));

        Discrepancy seal = DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(F(ClueCategory.Seal, "Blue hexagon · VO"), 2),
                                                CompareEvidence.ForHidden(ClueCategory.Seal, string.Empty, 2, true), "x", "y", "P");
        Assert.AreEqual(DiscrepancyProof.HiddenMismatch, seal.provedBy, "a missing microprint proves its seal");
    }
}
