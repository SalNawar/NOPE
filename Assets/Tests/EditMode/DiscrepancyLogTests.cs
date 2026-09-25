using NUnit.Framework;

/// <summary>
/// Decision table for DiscrepancyLog.Prove and Add — the core verification rule.
/// Claim under test: Norvik / Medieval. Truth for Technology: "Longship".
/// A liar's papers print "Aqueduct" (which really belongs to Latia / Rome).
/// </summary>
public class DiscrepancyLogTests
{
    private const string ClaimNation = "norvik";
    private const string ClaimEra = "medieval";

    private static CompareEvidence TellDocField(string value = "Aqueduct") => new CompareEvidence
    {
        kind = EvidenceKind.DocumentField,
        category = ClueCategory.Technology,
        value = value,
        isAnachronism = true
    };

    private static CompareEvidence HonestDocField(string value = "Longship") => new CompareEvidence
    {
        kind = EvidenceKind.DocumentField,
        category = ClueCategory.Technology,
        value = value,
        isAnachronism = false
    };

    private static CompareEvidence Entry(string nationId, string eraId, string value, ClueCategory category = ClueCategory.Technology) =>
        CompareEvidence.ForReferenceEntry(category, value, nationId, eraId, $"{nationId} — {eraId}");

    private static CompareEvidence TellIdentityField(ClueCategory category, string value) => new CompareEvidence
    {
        kind = EvidenceKind.DocumentField,
        category = category,
        value = value,
        isAnachronism = true
    };

    /// <summary>A spoken Technology answer: a liar's Answer tell by default.</summary>
    private static CompareEvidence SaidDevice(string value = "Aqueduct", bool isTell = true) =>
        CompareEvidence.ForAnswer(ClueCategory.Technology, value, isTell);

    /// <summary>Proves the pair and documents the proof; the proof when the log accepts it, else null.</summary>
    private static Discrepancy Register(DiscrepancyLog log, CompareEvidence a, CompareEvidence b, string claimedNationId, string claimedEraId)
    {
        Discrepancy proof = DiscrepancyLog.Prove(a, b, claimedNationId, claimedEraId);
        return log.Add(proof) ? proof : null;
    }

    // -----------------------------
    // Record proof (identity)
    // -----------------------------

    [Test]
    public void BirthDateTell_VsRecord_Registers_AsRecordMismatch()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, 
            TellIdentityField(ClueCategory.BirthDate, "3 May 1101"),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131"),
            ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.RecordMismatch, d.provedBy);
        Assert.AreEqual("3 May 1131", d.expectedValue);
        Assert.AreEqual("deviation.recordMismatch.papers", d.ReportKey);
        Assert.AreEqual("3 May 1131", d.ReportOther);
    }

    [Test]
    public void HonestBirthDate_MatchingRecord_DoesNotRegister()
    {
        var log = new DiscrepancyLog();
        var honest = new CompareEvidence
        {
            kind = EvidenceKind.DocumentField,
            category = ClueCategory.BirthDate,
            value = "3 May 1131",
            isAnachronism = false
        };

        Assert.IsNull(Register(log, honest, CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131"), ClaimNation, ClaimEra));
    }

    [Test]
    public void RecordField_VsDifferentCategoryDoc_DoesNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, 
            TellDocField(), // Technology
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131"),
            ClaimNation, ClaimEra));
    }

    [Test]
    public void TwoRecordFields_DoNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, 
            CompareEvidence.ForRecordField(ClueCategory.Name, "Bjorn"),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131"),
            ClaimNation, ClaimEra));
    }

    [Test]
    public void RecordProof_WorksEvenWithoutClaimEra()
    {
        // Identity has nothing to do with the travel claim.
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, 
            TellIdentityField(ClueCategory.BirthDate, "3 May 1101"),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131"),
            null, null);

        Assert.NotNull(d);
    }

    // -----------------------------
    // Proof modalities
    // -----------------------------

    [Test]
    public void MismatchAgainstClaimedEraEntry_Registers()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual("Longship", d.expectedValue);
        Assert.IsNull(d.actualOrigin);
        Assert.AreEqual(1, log.Count);
    }

    [Test]
    public void MatchAgainstForeignEraEntry_Registers_AsOriginProof()
    {
        // The screenshot bug: papers say Aqueduct, player matches it against
        // the Latia/Rome entry — that MATCH proves the device is Roman.
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, TellDocField("Aqueduct"), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.IsNull(d.expectedValue);
        Assert.AreEqual("latia — rome", d.actualOrigin);
    }

    [Test]
    public void EraOnlyEntry_AppliesToAnyNationOfThatEra()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, TellDocField(), Entry(null, "medieval", "Longship"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
    }

    [Test]
    public void ArgumentOrder_DoesNotMatter()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, Entry("norvik", "medieval", "Longship"), TellDocField(), ClaimNation, ClaimEra);

        Assert.NotNull(d);
    }

    // -----------------------------
    // Rejections (junk comparisons must not register)
    // -----------------------------

    [Test]
    public void MismatchAgainstForeignEraEntry_DoesNotRegister()
    {
        // Aqueduct vs The Future's "Solar Sail": mismatch, but the entry says
        // nothing about the claim — proves nothing.
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, TellDocField(), Entry("helios", "future", "Solar Sail"), ClaimNation, ClaimEra));
        Assert.AreEqual(0, log.Count);
    }

    [Test]
    public void HonestField_NeverRegisters_EitherWay()
    {
        var log = new DiscrepancyLog();
        // Mismatch path: honest Longship vs a wrong-value entry.
        Assert.IsNull(Register(log, HonestDocField(), Entry("norvik", "medieval", "Waterwheel"), ClaimNation, ClaimEra));
        // Match path: honest Longship happens to match a foreign entry.
        Assert.IsNull(Register(log, HonestDocField(), Entry("latia", "rome", "Longship"), ClaimNation, ClaimEra));
    }

    [Test]
    public void DifferentCategories_DoNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, TellDocField(), Entry("norvik", "medieval", "Silver Mark", ClueCategory.Currency), ClaimNation, ClaimEra));
    }

    [Test]
    public void TwoDocumentFields_DoNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, TellDocField(), TellDocField("Something"), ClaimNation, ClaimEra));
    }

    [Test]
    public void TwoReferenceEntries_DoNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, Entry("norvik", "medieval", "Longship"), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra));
    }

    [Test]
    public void OtherNationSameEraEntry_MismatchDoesNotRegister()
    {
        // Albion's Waterwheel is not Norvik's truth — a mismatch against it
        // proves nothing about the claim.
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, TellDocField(), Entry("albion", "medieval", "Waterwheel"), ClaimNation, ClaimEra));
    }

    [Test]
    public void OtherNationSameEraEntry_MatchRegisters_AsOriginProof()
    {
        // The tell equals Albion/Medieval's truth while claiming Norvik:
        // the value provably belongs to Albion.
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, TellDocField("Waterwheel"), Entry("albion", "medieval", "Waterwheel"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual("albion — medieval", d.actualOrigin);
    }

    [Test]
    public void NullClaimEra_DoesNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, null));
    }

    [Test]
    public void ValueComparison_IgnoresCaseAndWhitespace()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, TellDocField("  aqueduct "), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
    }

    // -----------------------------
    // Log behaviour
    // -----------------------------

    [Test]
    public void SameCategory_RegistersOnlyOnce()
    {
        var log = new DiscrepancyLog();
        Assert.NotNull(Register(log, TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra));
        Assert.IsNull(Register(log, TellDocField(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra));
        Assert.AreEqual(1, log.Count);
    }

    [Test]
    public void Clear_EmptiesTheLog()
    {
        var log = new DiscrepancyLog();
        Register(log, TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);
        log.Clear();

        Assert.AreEqual(0, log.Count);
    }

    // -----------------------------
    // Spoken answers (the interview)
    // -----------------------------

    [Test]
    public void AnswerTell_VsTheClaimsRow_ProvesAMismatch_SaidByTheTraveller()
    {
        Discrepancy d = DiscrepancyLog.Prove(SaidDevice(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d.provedBy);
        Assert.AreEqual(EvidenceKind.Answer, d.source);
        Assert.AreEqual("deviation.claimMismatch.said", d.ReportKey);
        Assert.AreEqual("Aqueduct", d.documentValue);
        Assert.AreEqual("Longship", d.ReportOther);
    }

    [Test]
    public void AnswerTell_VsAForeignRow_ProvesTheOrigin_SaidByTheTraveller()
    {
        Discrepancy d = DiscrepancyLog.Prove(Entry("latia", "rome", "Aqueduct"), SaidDevice(), ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d.provedBy);
        Assert.AreEqual("latia — rome", d.actualOrigin);
        Assert.AreEqual("deviation.foreignOrigin.said", d.ReportKey);
        Assert.AreEqual("latia — rome", d.ReportOther);
    }

    [Test]
    public void AnswerTell_VsTheRecord_ProvesARecordMismatch()
    {
        Discrepancy d = DiscrepancyLog.Prove(
            CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 Jun 1801 BCE", true),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 Jun 1510 BCE"),
            ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.RecordMismatch, d.provedBy);
        Assert.AreEqual("deviation.recordMismatch.said", d.ReportKey);
        Assert.AreEqual("3 Jun 1801 BCE", d.documentValue);
        Assert.AreEqual("3 Jun 1510 BCE", d.ReportOther);
    }

    [Test]
    public void HonestAnswer_NeverRegisters_EitherWayOrOrder()
    {
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice("Longship", false), Entry("norvik", "medieval", "Waterwheel"), ClaimNation, ClaimEra));
        Assert.IsNull(DiscrepancyLog.Prove(Entry("latia", "rome", "Longship"), SaidDevice("Longship", false), ClaimNation, ClaimEra));
        Assert.IsNull(DiscrepancyLog.Prove(
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131"),
            CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 May 1101", false),
            ClaimNation, ClaimEra));
    }

    [Test]
    public void AnswerVsPapers_AndAnswerVsAnswer_ProveNothing()
    {
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice(), HonestDocField(), ClaimNation, ClaimEra));
        Assert.IsNull(DiscrepancyLog.Prove(TellDocField(), SaidDevice("Longship", false), ClaimNation, ClaimEra));
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice(), SaidDevice("Longship"), ClaimNation, ClaimEra));
    }

    [Test]
    public void PaperReports_UseThePapersKeys_AndTheCategoryWord()
    {
        Discrepancy mismatch = DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);
        Assert.AreEqual(EvidenceKind.DocumentField, mismatch.source);
        Assert.AreEqual("deviation.claimMismatch.papers", mismatch.ReportKey);
        Assert.AreEqual("Longship", mismatch.ReportOther);
        Assert.AreEqual("category.Technology", ClueLabels.Key(mismatch.category));

        Discrepancy origin = DiscrepancyLog.Prove(TellDocField(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);
        Assert.AreEqual("deviation.foreignOrigin.papers", origin.ReportKey);
        Assert.AreEqual("latia — rome", origin.ReportOther);
    }

    [Test]
    public void Add_RefusesASecondProofOfADocumentedCategory_FromEitherSource_AndNull()
    {
        var log = new DiscrepancyLog();
        Assert.IsTrue(log.Add(DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra)));
        Assert.IsFalse(log.Add(DiscrepancyLog.Prove(SaidDevice(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra)), "same category, spoken");
        Assert.IsFalse(log.Add(null));
        Assert.AreEqual(1, log.Count);
        Assert.AreEqual(EvidenceKind.DocumentField, log.Items[0].source);
    }

    [Test]
    public void Prove_IsPure_ADocumentedCategoryStillProves_TheSameWayTwice()
    {
        var log = new DiscrepancyLog();
        Assert.IsTrue(log.Add(DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra)));

        // Technology is documented now; Prove never reads the log, so the spoken pair still proves.
        Discrepancy x = DiscrepancyLog.Prove(SaidDevice(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);
        Discrepancy y = DiscrepancyLog.Prove(SaidDevice(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);

        Assert.NotNull(x, "an already documented category still proves; only Add refuses it");
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, x.provedBy);
        Assert.AreNotSame(x, y);
        Assert.AreEqual(x.ReportKey, y.ReportKey);
        Assert.AreEqual(x.ReportOther, y.ReportOther);
        Assert.AreEqual(x.provedBy, y.provedBy);
        Assert.AreEqual(x.source, y.source);
        Assert.AreEqual(1, log.Count, "proving documents nothing");
        Assert.AreEqual(EvidenceKind.DocumentField, log.Items[0].source);
    }

    [TestCase(DiscrepancyProof.ClaimMismatch, EvidenceKind.DocumentField, "deviation.claimMismatch.papers")]
    [TestCase(DiscrepancyProof.ForeignOrigin, EvidenceKind.Answer, "deviation.foreignOrigin.said")]
    [TestCase(DiscrepancyProof.RecordMismatch, EvidenceKind.DocumentField, "deviation.recordMismatch.papers")]
    [TestCase(DiscrepancyProof.RecordMismatch, EvidenceKind.Answer, "deviation.recordMismatch.said")]
    [TestCase(DiscrepancyProof.ClaimMismatch, EvidenceKind.None, "deviation.claimMismatch.papers")]
    public void ReportKeyFor_NamesTheProofAndWhoStatedIt(DiscrepancyProof proof, EvidenceKind statement, string expected)
    {
        Assert.AreEqual(expected, Discrepancy.ReportKeyFor(proof, statement));
    }

    [Test]
    public void ReportKey_AndReportOther_OfRealProofs()
    {
        Discrepancy mismatch = DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);
        Assert.AreEqual("deviation.claimMismatch.papers", mismatch.ReportKey);
        Assert.AreEqual("Longship", mismatch.ReportOther, "the expected value");

        Discrepancy origin = DiscrepancyLog.Prove(Entry("latia", "rome", "Aqueduct"), SaidDevice(), ClaimNation, ClaimEra);
        Assert.AreEqual("deviation.foreignOrigin.said", origin.ReportKey);
        Assert.AreEqual("latia — rome", origin.ReportOther, "the place the value belongs to");

        Discrepancy record = DiscrepancyLog.Prove(
            CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 Jun 1801 BCE", true),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 Jun 1510 BCE"),
            ClaimNation, ClaimEra);
        Assert.AreEqual("deviation.recordMismatch.said", record.ReportKey);
        Assert.AreEqual("3 Jun 1510 BCE", record.ReportOther, "the recorded value");
    }

    [TestCase(ClueCategory.Language, "category.Language")]
    [TestCase(ClueCategory.Material, "category.Material")]
    [TestCase(ClueCategory.Politics, "category.Politics")]
    [TestCase(ClueCategory.Technology, "category.Technology")]
    [TestCase(ClueCategory.Currency, "category.Currency")]
    [TestCase(ClueCategory.Geography, "category.Geography")]
    [TestCase(ClueCategory.Culture, "category.Culture")]
    [TestCase(ClueCategory.Name, "category.Name")]
    [TestCase(ClueCategory.BirthDate, "category.BirthDate")]
    public void ClueLabels_Key_OneKeyPerCategory(ClueCategory category, string expected)
    {
        Assert.AreEqual(expected, ClueLabels.Key(category));
    }

}
