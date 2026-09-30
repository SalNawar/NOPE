using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The document design spec (2026-09-30): the seals (D4: the offices, a seal's
/// value, a forgery that differs in one part, the seal proof), someone else's
/// photo (D8: Looks.Stranger, the identity key, the photo proof) and the
/// published fault canon (D9: its variants, its reading, its problems, and
/// the real table's soundness).
/// </summary>
public class DocumentDesignTests
{
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    private static AgencyOffice Visa() => new AgencyOffice
    {
        id = "visa", name = "Visa Office", shape = "Hexagon", ink = "Blue", legend = "VO", forgedLegend = "VD", forms = new List<string> { "TC-101" }
    };

    private static AgencyOffice Labour() => new AgencyOffice
    {
        id = "labour", name = "Labour Placement Bureau", shape = "Square", ink = "Black", legend = "LB", forgedLegend = "LP", forms = new List<string> { "TC-520" }
    };

    // ---------------- Seals (D4) ----------------

    [Test]
    public void ASealsValue_IsItsInkOutlineAndLegend_AndReadsBack()
    {
        Assert.IsTrue(Visa().TryGetSeal(out Seal seal));
        Assert.AreEqual("Blue hexagon · VO", Seals.Describe(seal));
        Assert.IsTrue(Seals.TryParse("  blue HEXAGON · vo ", out Seal back), "any case, trimmed");
        Assert.AreEqual(seal, back);
        Assert.IsFalse(Seals.TryParse("Blue hexagon VO", out _), "no separator");
        Assert.IsFalse(Seals.TryParse("Teal hexagon · VO", out _), "no such ink");
        Assert.IsFalse(Seals.TryParse("Blue blob · VO", out _), "no such outline");
        Assert.IsFalse(Seals.TryParse(null, out _));
    }

    [Test]
    public void AForgedSeal_DiffersInExactlyOnePart()
    {
        AgencyOffice visa = Visa();
        visa.TryGetSeal(out Seal truth);

        var shapeDraw = new ScriptedRandom(R(0));
        Seal shape = Seals.Forge(visa, SealForgery.Shape, shapeDraw);
        Assert.IsTrue(shapeDraw.Done, "one draw over the five other outlines");
        Assert.AreEqual(SealShape.Circle, shape.Shape, "the first other outline, in enum order");
        Assert.AreEqual(truth.Ink, shape.Ink);
        Assert.AreEqual(truth.Legend, shape.Legend);

        var inkDraw = new ScriptedRandom(R(1));
        Seal ink = Seals.Forge(visa, SealForgery.Ink, inkDraw);
        Assert.IsTrue(inkDraw.Done);
        Assert.AreEqual(SealInk.Green, ink.Ink, "the second other ink: Red, Green, ...");
        Assert.AreEqual(truth.Shape, ink.Shape);

        var none = new ScriptedRandom();
        Seal legend = Seals.Forge(visa, SealForgery.Legend, none);
        Assert.AreEqual("VD", legend.Legend, "the office's authored wrong legend, no draw");
        Assert.AreEqual(truth.Shape, legend.Shape);
        Assert.AreEqual(truth.Ink, legend.Ink);
        foreach (Seal forged in new[] { shape, ink, legend })
            Assert.AreNotEqual(Seals.Describe(truth), Seals.Describe(forged));
    }

    [Test]
    public void EverySealOutline_IsDrawn_ARingWithRoomForItsLegend()
    {
        foreach (SealShape shape in (SealShape[])Enum.GetValues(typeof(SealShape)))
        {
            string name = shape.ToString();
            Assert.IsTrue(SealOutlines.Has(name), name);
            Assert.IsFalse(SealOutlines.Inked(name, 0f, 0f), $"{name}: the middle is left for the legend");
            Assert.IsFalse(SealOutlines.Inked(name, 0.3f, 0.2f), $"{name}: the legend's corner is clear");
            int inked = 0;
            for (int i = 0; i < 64; i++)
            {
                double a = i * Math.PI / 32.0;
                for (float r = 0.5f; r <= 1f; r += 0.02f)
                    if (SealOutlines.Inked(name, (float)(r * Math.Cos(a)), (float)(r * Math.Sin(a))))
                    {
                        inked++;
                        break;
                    }
            }
            Assert.AreEqual(64, inked, $"{name}: the ring closes all round");
        }
        Assert.IsFalse(SealOutlines.Has("Blob"));
        Assert.IsFalse(SealOutlines.Inked("Blob", 0.95f, 0f));
    }

    [Test]
    public void TheOffices_EachIssueTheirForms_AndTheirSealsTellEveryOfficeApart()
    {
        List<AgencyOffice> offices = ContentFixture.Offices();
        string[] forms = { "TC-101", "TC-230", "TC-310", "TC-415", "TC-416", "TC-417", "TC-520", "TC-610", "TC-620", "TC-630" };
        CollectionAssert.IsEmpty(Seals.Problems(offices, forms), "the published offices are sound");
        foreach (string form in forms)
            Assert.IsNotNull(Seals.OfficeOf(offices, form), form);
        Assert.IsNull(Seals.OfficeOf(offices, "TC-999"));
    }

    [Test]
    public void OfficeProblems_NameEachBrokenRule()
    {
        AgencyOffice twin = Labour();
        twin.shape = "Hexagon";
        twin.ink = "Blue";
        twin.forgedLegend = "VO";
        List<string> problems = Seals.Problems(new[] { Visa(), twin }, new[] { "TC-101", "TC-520", "TC-230" });
        Assert.IsTrue(problems.Any(p => p.Contains("outline and ink")), "two offices' seals look alike");
        Assert.IsTrue(problems.Any(p => p.Contains("forged legend")), "a forger's legend that is an office's");
        Assert.IsTrue(problems.Any(p => p.Contains("no office issues form TC-230")));
    }

    [Test]
    public void ASeal_ProvesOnlyAgainstTheRegister()
    {
        string truth = "Blue hexagon · VO", forged = "Blue circle · VO", labours = "Black square · LB";
        var forgedField = new DocumentField { category = ClueCategory.Seal, value = forged, isAnachronism = true, issuer = "visa" };
        var honestField = new DocumentField { category = ClueCategory.Seal, value = truth, issuer = "visa" };
        CompareEvidence visaRow = CompareEvidence.ForSealRow(truth, "visa", "Visa Office");
        CompareEvidence labourRow = CompareEvidence.ForSealRow(labours, "labour", "Labour Placement Bureau");

        Discrepancy mismatch = DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(forgedField, 0), visaRow, "egypt", "ancient", "Mara");
        Assert.IsNotNull(mismatch, "the office's own row disproves a forged seal");
        Assert.AreEqual(ClueCategory.Seal, mismatch.category);
        Assert.AreEqual(DiscrepancyProof.ClaimMismatch, mismatch.provedBy);
        Assert.AreEqual(truth, mismatch.expectedValue);
        Assert.IsNotNull(DiscrepancyLog.Prove(visaRow, CompareEvidence.FromDocumentField(forgedField, 0), null, null, null), "either order");

        Assert.IsNull(DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(honestField, 0), visaRow, "egypt", "ancient", "Mara"), "an honest seal proves nothing");
        Assert.IsNull(DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(forgedField, 0), labourRow, "egypt", "ancient", "Mara"), "another office's different seal proves nothing");

        var borrowed = new DocumentField { category = ClueCategory.Seal, value = labours, isAnachronism = true, issuer = "visa" };
        Discrepancy foreign = DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(borrowed, 0), labourRow, "egypt", "ancient", "Mara");
        Assert.IsNotNull(foreign, "a visa carrying the Labour Bureau's seal: the seal belongs there");
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, foreign.provedBy);
        Assert.AreEqual("Labour Placement Bureau", foreign.actualOrigin);

        var otherPaper = new DocumentField { category = ClueCategory.Seal, value = labours, issuer = "labour" };
        Assert.IsNull(DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(forgedField, 0), CompareEvidence.FromDocumentField(otherPaper, 1), null, null, null),
                      "two papers' seals never cross-prove: each is its own office's");
        Assert.IsFalse(PaperChecks.IsCompared(ClueCategory.Seal));
    }

    // ---------------- Photos (D8) ----------------

    private static TravellerLook Person()
    {
        var wardrobe = new PlaceWardrobe
        {
            male = new GenderLook
            {
                signature = LookSlot.Headwear,
                outfit = new LookItem { label = "chiton" },
                hair = new LookItem { label = "short curls" },
                headwear = new LookItem { label = "petasos" }
            },
            female = new GenderLook { outfit = new LookItem { label = "peplos" }, hair = new LookItem { label = "bun" } }
        };
        var claim = new LookSource { NationId = "greece", EraId = "ancient", PlaceId = "greece_ancient", Wardrobe = wardrobe, CultureValue = "Greek dress" };
        var weights = new LookWeights { skin = new[] { 0f, 0f, 1f, 0f, 0f }, hair = new List<HairColourWeight> { new HairColourWeight { colour = "black", weight = 1f } } };
        var rules = new LookRules { faceBands = new List<FaceBand> { new FaceBand { minAge = 0, faces = new List<string> { "a" } } } };
        return Looks.Compose(claim, null, TravellerGender.Male, "1 Jan 480 BCE", -450, weights, rules,
                             new ScriptedRandom(ScriptStep.Value(0f), R(0), ScriptStep.Value(0f)));
    }

    [Test]
    public void AStranger_WearsTheSameClothes_TwoSkinTonesAway_WithAnotherHairColour()
    {
        TravellerLook me = Person();
        Assert.AreEqual(3, me.SkinTone);
        var draws = new ScriptedRandom(R(1), R(0));
        TravellerLook other = Looks.Stranger(me, draws);
        Assert.IsTrue(draws.Done, "the tone, then the colour");
        Assert.AreEqual(5, other.SkinTone, "tones 1 and 5 are two away from 3; the second");
        Assert.AreEqual("brown", other.HairColour, "the first hair colour that is not theirs (black)");
        Assert.AreEqual(me.Garments.Count, other.Garments.Count);
        Assert.AreEqual(me.Parts.Select(p => p.Layer), other.Parts.Select(p => p.Layer), "the same layers");
        Assert.AreEqual("body_m_skin5", other.PartOn(LookLayer.Body).Value.Key.Name);
        Assert.AreEqual("head_m_skin5_facea", other.PartOn(LookLayer.Head).Value.Key.Name);
        StringAssert.EndsWith("_brown", other.PartOn(LookLayer.Hair).Value.Key.Name, "the hair takes the new colour");
        Assert.AreEqual(me.PartOn(LookLayer.Headwear).Value.Key.Name, other.PartOn(LookLayer.Headwear).Value.Key.Name, "a hat is a hat");
        Assert.AreNotEqual(Looks.IdentityKey(me), Looks.IdentityKey(other));
        Assert.AreEqual("m/skin3/face-a/black", Looks.IdentityKey(me));
    }

    [Test]
    public void APremade_HasNoStranger_AndNoDraw()
    {
        TravellerLook whole = Looks.Whole("socrates", null, new LookRules());
        Assert.IsNull(Looks.Stranger(whole, new ScriptedRandom()));
        Assert.AreEqual("premade:socrates", Looks.IdentityKey(whole));
        Assert.AreEqual(string.Empty, Looks.IdentityKey(null));
    }

    [Test]
    public void APhoto_ProvesOnlyAgainstThePersonAtTheDesk()
    {
        TravellerLook me = Person();
        string person = Looks.IdentityKey(me);
        string stranger = Looks.IdentityKey(Looks.Stranger(me, new ScriptedRandom(R(0), R(0))));
        var swapped = new DocumentField { category = ClueCategory.Photo, value = stranger, isAnachronism = true };
        var honest = new DocumentField { category = ClueCategory.Photo, value = person };

        Discrepancy proof = DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(swapped, 0), CompareEvidence.ForPerson(person), null, null, null);
        Assert.IsNotNull(proof);
        Assert.AreEqual(DiscrepancyProof.PersonMismatch, proof.provedBy);
        Assert.AreEqual("deviation.personMismatch.papers", proof.ReportKey);
        Assert.IsNull(DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(honest, 0), CompareEvidence.ForPerson(person), null, null, null), "their own photo");
        Assert.IsNull(DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(swapped, 0), CompareEvidence.FromDocumentField(honest, 1), null, null, null), "two photos never cross-prove");
        Assert.IsFalse(PaperChecks.IsCompared(ClueCategory.Photo));
        var pair = new ComparePair();
        pair.Select(new ComparePick("a", "", "(photo)", CompareEvidence.FromDocumentField(honest, 0)));
        pair.Select(new ComparePick("face", "", "(face)", CompareEvidence.ForPerson(person)));
        Assert.IsTrue(pair.Matches, "an honest photo matches the person on their identity, whatever each side shows");
    }

    // ---------------- The visual lies' plans ----------------

    private static RecordForm Paper(string form, params ClueCategory[] fields) =>
        new RecordForm(form, fields.Select(c => new DocumentField { category = c, value = "v" }).ToList());

    [Test]
    public void AForgedSeal_DrawsTheVariantThePaperThenTheForgery_FromTheCanon()
    {
        List<FaultEntry> canon = ContentFixture.Faults();
        List<AgencyOffice> offices = ContentFixture.Offices();
        var forms = new[] { Paper("TC-101", ClueCategory.Name, ClueCategory.Seal), Paper("TC-230", ClueCategory.CitizenId, ClueCategory.Seal) };
        var draws = new ScriptedRandom(R(0), R(1), R(0));
        List<RecordTell> tells = VisualLies.PlanSeal(canon, forms, offices, draws);
        Assert.IsTrue(draws.Done, "the variant (shape, ink, legend), the paper, the other outline");
        Assert.AreEqual(1, tells.Count);
        Assert.AreEqual(1, tells[0].Document, "the second paper: the manifest");
        Assert.AreEqual(ClueCategory.Seal, tells[0].Category);
        Assert.AreEqual("Green hexagon · PH", tells[0].Value, "Portal Hall Dispatch's green circle, its outline the first other one");

        var one = new ScriptedRandom(R(2));
        List<RecordTell> legend = VisualLies.PlanSeal(canon, new[] { Paper("TC-610", ClueCategory.Seal) }, offices, one);
        Assert.IsTrue(one.Done, "one paper: no paper draw; the legend draws nothing");
        Assert.AreEqual("Violet circle · DB", legend[0].Value);

        Assert.IsEmpty(VisualLies.PlanSeal(canon, new[] { Paper("TC-999", ClueCategory.Seal) }, offices, new ScriptedRandom()), "no office: nothing to forge, no draw");
        Assert.IsEmpty(VisualLies.PlanSeal(new List<FaultEntry>(), forms, offices, new ScriptedRandom()), "no canon row: nothing");
    }

    [Test]
    public void SomeoneElsesPhoto_IsAStrangerOnTheCanonsPhotoForms()
    {
        List<FaultEntry> canon = ContentFixture.Faults();
        var forms = new[] { Paper("TC-101", ClueCategory.Name, ClueCategory.Photo), Paper("TC-230", ClueCategory.CitizenId) };
        var draws = new ScriptedRandom(R(0), R(0));
        List<RecordTell> tells = VisualLies.PlanPhoto(canon, forms, Person(), draws, out TravellerLook stranger);
        Assert.IsTrue(draws.Done);
        Assert.IsNotNull(stranger);
        Assert.AreEqual(1, tells.Count);
        Assert.AreEqual(0, tells[0].Document);
        Assert.AreEqual(Looks.IdentityKey(stranger), tells[0].Value);
        Assert.IsEmpty(VisualLies.PlanPhoto(canon, new[] { Paper("TC-230", ClueCategory.CitizenId) }, Person(), new ScriptedRandom(), out stranger), "no photo carried: no draw");
        Assert.IsNull(stranger);
        Assert.IsEmpty(VisualLies.PlanPhoto(canon, forms, Looks.Whole("socrates", null, new LookRules()), new ScriptedRandom(), out _), "a premade's picture is not swapped");
    }

    [Test]
    public void TheLookMenu_OffersTheFace_WhenAPaperShowsAPhoto()
    {
        var lines = new InterviewLines { lookLabel = "Look >", backLabel = "< Back", faceLabel = "Their face" };
        DialogGraph withFace = InterviewScript.Build(lines, null, null, new InterviewCase { face = true });
        DialogChoice face = withFace.Node(InterviewScript.LookNodeId).Choices.Single(c => c.Id == "look:face");
        Assert.AreEqual(DialogAction.InspectFace, face.Action);
        Assert.AreEqual("Their face", face.Label);
        Assert.AreEqual(DialogChoiceKind.Look, face.Kind);
        Assert.IsTrue(withFace.Node(InterviewScript.HubNodeId).Choices.Any(c => c.Id == "look"), "the face alone opens the look menu");
        DialogGraph without = InterviewScript.Build(lines, null, null, new InterviewCase());
        Assert.IsFalse(without.Node(InterviewScript.HubNodeId).Choices.Any(c => c.Id == "look"), "no photo, no garment: no look menu");
    }

    // ---------------- The fault canon (D9) ----------------

    private static FaultEntry Row(string id, string lie, string variant, string form, string field, bool optional = false) =>
        new FaultEntry { id = id, lie = lie, variant = variant, form = form, field = field, optional = optional, against = "x" };

    [Test]
    public void TheCanonsVariants_AreItsRowsGroupedInOrder()
    {
        var canon = new[]
        {
            Row("a", "FakeWaiver", "number", "TC-310", "WaiverNo"),
            Row("b", "ForgedProof", "credit", "TC-415", "Credit"),
            Row("c", "FakeWaiver", "transponder", "TC-310", "TransponderId"),
            Row("d", "FakeWaiver", "number", "TC-310", "Debt")
        };
        List<FaultVariant> variants = FaultCanon.Variants(canon, LieKind.FakeWaiver);
        Assert.AreEqual(new[] { "number", "transponder" }, variants.Select(v => v.Name));
        Assert.AreEqual(new[] { "a", "d" }, variants[0].Rows.Select(r => r.id));
        Assert.IsTrue(FaultCanon.Allows(canon, FaultCanon.Of(LieKind.FakeWaiver), "TC-310", ClueCategory.Debt));
        Assert.IsFalse(FaultCanon.Allows(canon, FaultCanon.Of(LieKind.FakeWaiver), "TC-415", ClueCategory.Credit));
        Assert.IsTrue(FaultCanon.Allows(new[] { Row("s", "ForgedSeal", "shape", FaultCanon.Any, "Seal") }, FaultCanon.Of(LieKind.ForgedSeal), "TC-620", ClueCategory.Seal),
                      "a seal row names every paper");
    }

    [Test]
    public void CanonProblems_NameEachBrokenRule()
    {
        var forms = new Dictionary<string, IReadOnlyList<ClueCategory>> { ["TC-310"] = new[] { ClueCategory.WaiverNo, ClueCategory.Seal } };
        var canon = new[]
        {
            Row("a", "FakeWaiver", "number", "TC-310", "WaiverNo"),
            Row("a", "FakeWaiver", "number", "TC-310", "Wage"),
            Row("b", "NoSuchLie", "x", "TC-310", "WaiverNo"),
            new FaultEntry { id = "c", lie = "FakeWaiver", directive = "ExpiredPaper", variant = "v", form = "TC-310", field = "WaiverNo", against = "x" },
            Row("d", "FakeWaiver", "number", FaultCanon.Any, "WaiverNo"),
            Row("e", "FakeWaiver", "opt", "TC-310", "WaiverNo", optional: true),
            Row("f", "FakeWaiver", "", "TC-999", "WaiverNo")
        };
        List<string> problems = FaultCanon.Problems(canon, forms);
        Assert.IsTrue(problems.Any(p => p.Contains("'a'") && p.Contains("twice")));
        Assert.IsTrue(problems.Any(p => p.Contains("prints no Wage")));
        Assert.IsTrue(problems.Any(p => p.Contains("'NoSuchLie' is not a lie")));
        Assert.IsTrue(problems.Any(p => p.Contains("'c'") && p.Contains("both")));
        Assert.IsTrue(problems.Any(p => p.Contains("only a forged seal")));
        Assert.IsTrue(problems.Any(p => p.Contains("only optional rows")));
        Assert.IsTrue(problems.Any(p => p.Contains("'TC-999' is not a form")));
        Assert.IsTrue(problems.Any(p => p.Contains("'f'") && p.Contains("variant")));
    }

    [Test]
    public void ThePublishedCanon_NamesRealLiesAndDirectives_AndCoversEveryLie()
    {
        List<FaultEntry> canon = ContentFixture.Faults();
        foreach (FaultEntry e in canon)
        {
            Assert.IsTrue(e.TryLie(out _) ^ e.TryDirective(out _), e.id);
            Assert.IsFalse(string.IsNullOrWhiteSpace(e.against), e.id);
        }
        foreach (LieKind lie in (LieKind[])Enum.GetValues(typeof(LieKind)))
            Assert.IsNotEmpty(FaultCanon.Variants(canon, lie), $"{lie} draws from the canon");
        Assert.AreEqual(new[] { "shape", "ink", "legend" }, FaultCanon.Variants(canon, LieKind.ForgedSeal).Select(v => v.Name),
                        "the seal's variants are SealForgery's, in its order");
        foreach (FaultVariant v in FaultCanon.Variants(canon, LieKind.ForgedSeal))
            Assert.IsTrue(Enum.TryParse(v.Name, true, out SealForgery _), v.Name);
        Assert.AreEqual(new[] { "TC-101", "TC-520", "TC-610" }, FaultCanon.Variants(canon, LieKind.SwappedPhoto).Single().Rows.Select(r => r.form),
                        "the photo forms: each kind's primary paper");
    }
}
