using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The desk machines spec §1-3 (Saleh 2026-10-17: the chip reader "reads the
/// chip", the UV torch shows "the hidden watermarks and symbols"): what each
/// paper shows under UV and holds in its chip, the hidden lies' plans, and
/// the content's checks.
/// </summary>
public class HiddenLayersTests
{
    private static DocumentField F(ClueCategory c, string value, string erased = "", string issuer = "") =>
        new DocumentField { category = c, value = value, label = c.ToString(), erased = erased, issuer = issuer };

    private static HiddenContent Content(float ghost = 1f) => new HiddenContent { chipForms = new List<string> { "TC-101", "TC-240" }, ghostChance = ghost };

    /// <summary>Counts the draws made on it (a fixed value, a fixed range pick).</summary>
    private sealed class Counting : IRandomSource
    {
        public int Draws;
        private readonly float _value;
        public Counting(float value = 0f) => _value = value;
        public int Range(int minInclusive, int maxExclusive)
        {
            Draws++;
            return minInclusive;
        }
        public float Value()
        {
            Draws++;
            return _value;
        }
    }

    private static List<DocumentField> Passport(string idErased = "", string sealErased = "") => new List<DocumentField>
    {
        F(ClueCategory.Name, "Pell Quimby"), F(ClueCategory.CitizenId, "NHA-512", idErased), F(ClueCategory.BirthDate, "4 Jul 2124"),
        F(ClueCategory.Seal, "Blue hexagon · VO", sealErased, "visa_office")
    };

    private static List<DocumentField> Ticket() => new List<DocumentField> { F(ClueCategory.CitizenId, "NHA-512"), F(ClueCategory.Seal, "Red circle · TO", "", "ticket_office") };

    [Test]
    public void AnHonestCase_AnIntactWatermarkAndMicroprint_AChipOnlyOnAChipForm_ThatHoldsThePrint_NoDraw()
    {
        var rng = new Counting();
        CaseHidden hidden = HiddenLayers.Build(new[] { new HiddenPaper("TC-101", Passport()), new HiddenPaper("TC-230", Ticket()) }, null, Content(), rng);
        Assert.AreEqual(2, hidden.Count);
        Assert.AreEqual(0, rng.Draws, "nothing forged, nothing drawn");
        IReadOnlyList<UvMark> marks = hidden.Marks(0);
        Assert.AreEqual(UvMarkKind.Watermark, marks[0].Kind);
        Assert.AreEqual(HiddenLayers.Watermark, marks[0].Text);
        UvMark micro = marks.Single(m => m.Kind == UvMarkKind.Microprint);
        Assert.AreEqual(3, micro.Field, "under the seal's box");
        Assert.AreEqual("VISA_OFFICE", micro.Text, "the issuing office's microprint");
        Assert.IsFalse(marks.Any(m => m.Fault));
        Assert.IsTrue(hidden.HasChip(0));
        Assert.IsFalse(hidden.HasChip(1), "the Entry Ticket carries no chip");
        Assert.IsEmpty(ScanLayers.ChipDifferences(Passport(), hidden.ChipOf(0)), "an honest chip never differs");
        Assert.IsEmpty(hidden.Faults(0));
    }

    [Test]
    public void AForgedBox_TheChipHoldsTheIssuedValue_AndAGhostShowsAtTheChance_OneDrawPerForgedBox()
    {
        var rng = new Counting(0.2f);
        CaseHidden ghosted = HiddenLayers.Build(new[] { new HiddenPaper("TC-101", Passport("NHA-777")) }, null, Content(0.5f), rng);
        Assert.AreEqual(1, rng.Draws, "one Value draw for the one forged box");
        UvMark ghost = ghosted.Faults(0).Single();
        Assert.AreEqual(UvMarkKind.Ghost, ghost.Kind);
        Assert.AreEqual(1, ghost.Field);
        Assert.AreEqual("NHA-777", ghost.Text, "the ghost of the value the office issued");
        CollectionAssert.AreEqual(new[] { 1 }, ScanLayers.ChipDifferences(Passport("NHA-777"), ghosted.ChipOf(0)), "the chip still holds the issued Citizen ID");

        CaseHidden plain = HiddenLayers.Build(new[] { new HiddenPaper("TC-101", Passport("NHA-777")) }, null, Content(0.1f), new Counting(0.2f));
        Assert.IsEmpty(plain.Faults(0), "a draw over the chance leaves no ghost");
        Assert.IsNotEmpty(ScanLayers.ChipDifferences(Passport("NHA-777"), plain.ChipOf(0)), "the chip shows it whatever the draw");
    }

    [Test]
    public void AForgedSeal_ShowsNoMicroprint_AFault_AndDrawsNothing()
    {
        var rng = new Counting();
        CaseHidden hidden = HiddenLayers.Build(new[] { new HiddenPaper("TC-230", new List<DocumentField> { F(ClueCategory.Seal, "Red square · XX", "Red circle · TO", "ticket_office") }) },
                                               null, Content(), rng);
        UvMark micro = hidden.Faults(0).Single();
        Assert.AreEqual(UvMarkKind.Microprint, micro.Kind);
        Assert.AreEqual(string.Empty, micro.Text, "no microprint at all");
        Assert.AreEqual(0, rng.Draws);
    }

    [Test]
    public void AHiddenLiesTells_AreWrittenIn_AGhostUnderUv_AChipValue_AnUnknownPaperIgnored()
    {
        var tells = new[]
        {
            new HiddenTell(0, ClueCategory.Name, ScanLayer.Uv, "Orla Venn"),
            new HiddenTell(1, ClueCategory.CitizenId, ScanLayer.Chip, "QRT-301"),
            new HiddenTell(7, ClueCategory.Name, ScanLayer.Uv, "nobody")
        };
        CaseHidden hidden = HiddenLayers.Build(new[] { new HiddenPaper("TC-310", new List<DocumentField> { F(ClueCategory.Name, "Pell Quimby") }), new HiddenPaper("TC-240", Ticket()) },
                                               tells, Content(), new Counting());
        UvMark ghost = hidden.Faults(0).Single();
        Assert.AreEqual(("Orla Venn", 0), (ghost.Text, ghost.Field));
        Assert.IsFalse(hidden.HasChip(0), "a waiver carries no chip");
        CollectionAssert.AreEqual(new[] { 0 }, ScanLayers.ChipDifferences(Ticket(), hidden.ChipOf(1)));
        Assert.AreEqual("QRT-301", hidden.ChipOf(1)[0].value);
        Assert.IsEmpty(hidden.Marks(7));
        Assert.IsNull(hidden.ChipOf(7));
    }

    private static readonly List<FaultEntry> Canon = new List<FaultEntry>
    {
        new FaultEntry { id = "clonedPassportChip", form = "TC-101", field = "CitizenId", lie = "ClonedChip", variant = "cloned", against = "chip" },
        new FaultEntry { id = "clonedCardChip", form = "TC-240", field = "CitizenId", lie = "ClonedChip", variant = "cloned", against = "chip" },
        new FaultEntry { id = "washedWaiver", form = "TC-310", field = "Name", lie = "WashedPaper", variant = "washed", against = "uv" },
    };

    [Test]
    public void PlanChip_AChippedPapersCitizenId_AFreshNumberNobodyHolds_OneRangeDrawAmongTwo()
    {
        var forms = new List<RecordForm> { new RecordForm("TC-101", Passport()), new RecordForm("TC-230", Ticket()), new RecordForm("TC-240", Ticket()) };
        var taken = new HashSet<string> { "NHA-512" };
        List<HiddenTell> tells = HiddenLies.PlanChip(Canon, forms, Content(), taken, new SeededRandom(5));
        HiddenTell tell = tells.Single();
        Assert.AreEqual(ScanLayer.Chip, tell.Layer);
        Assert.AreEqual(ClueCategory.CitizenId, tell.Category);
        CollectionAssert.Contains(new[] { 0, 2 }, tell.Document, "a chipped paper the canon names, never the ticket");
        Assert.AreNotEqual("NHA-512", tell.Value);
        Assert.IsTrue(taken.Contains(tell.Value), "the clone's number is taken today");
        Assert.AreEqual(tell.Value, HiddenLies.PlanChip(Canon, forms, Content(), new HashSet<string> { "NHA-512" }, new SeededRandom(5)).Single().Value, "seeded");

        var counting = new Counting();
        CollectionAssert.IsEmpty(HiddenLies.PlanChip(Canon, new List<RecordForm> { new RecordForm("TC-230", Ticket()) }, Content(), taken, counting));
        Assert.AreEqual(0, counting.Draws, "nothing can show: no draw");
    }

    [Test]
    public void PlanWash_TheGhostOfAnotherName_NeverThePrintedOne()
    {
        var forms = new List<RecordForm> { new RecordForm("TC-101", Passport()), new RecordForm("TC-310", new List<DocumentField> { F(ClueCategory.Name, "Pell Quimby") }) };
        HiddenTell tell = HiddenLies.PlanWash(Canon, forms, new[] { "Pell Quimby", "Orla Venn" }, new SeededRandom(3)).Single();
        Assert.AreEqual((1, ClueCategory.Name, ScanLayer.Uv, "Orla Venn"), (tell.Document, tell.Category, tell.Layer, tell.Value));
        CollectionAssert.IsEmpty(HiddenLies.PlanWash(Canon, forms, new[] { "Pell Quimby" }, new SeededRandom(3)), "no other name: honest");
    }

    [Test]
    public void Problems_ANonIdentityRow_AClonedChipOffAChipForm_AHiddenLieBeforeItsMachine_AndTheContentsRanges()
    {
        var canon = new List<FaultEntry>(Canon)
        {
            new FaultEntry { id = "washedWage", form = "TC-520", field = "Wage", lie = "WashedPaper", variant = "washed", against = "uv" },
            new FaultEntry { id = "clonedTicket", form = "TC-230", field = "CitizenId", lie = "ClonedChip", variant = "cloned", against = "chip" },
        };
        int FirstDay(string key) => key == Feature.UvTorch ? 7 : 0;
        List<string> problems = HiddenLies.Problems(canon, Content(), new[] { (7, (IEnumerable<LieKind>)new[] { LieKind.WashedPaper }), (12, new[] { LieKind.ClonedChip, LieKind.ForgedSeal }) }, FirstDay);
        Assert.AreEqual(3, problems.Count, string.Join("\n", problems));
        StringAssert.Contains("'washedWage'", problems[0]);
        StringAssert.Contains("TC-230 carries no chip", problems[1]);
        StringAssert.Contains("Day 12 lists ClonedChip", problems[2]);
        CollectionAssert.IsEmpty(HiddenLies.Problems(Canon, Content(), new[] { (8, (IEnumerable<LieKind>)new[] { LieKind.WashedPaper }) }, FirstDay));

        var bad = new HiddenContent { chipForms = new List<string> { "TC-101", "", "TC-101" }, ghostChance = 1.5f };
        Assert.AreEqual(3, bad.Problems().Count);
        CollectionAssert.IsEmpty(Content(0.5f).Problems());
    }
}
