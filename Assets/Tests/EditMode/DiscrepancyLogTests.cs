using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// Decision table for DiscrepancyLog.Prove and Add — the core verification rule.
/// Claim under test: Norvik / Medieval, by the traveller Bjorn. Truth for
/// Technology: "Longship". A liar's papers print "Aqueduct" (which really
/// belongs to Latia / Rome).
/// </summary>
public class DiscrepancyLogTests
{
    private const string ClaimNation = "norvik";
    private const string ClaimEra = "medieval";

    /// <summary>The traveller at the desk, whose Citizen Record may prove a lie.</summary>
    private const string Traveller = "Bjorn";

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
        Discrepancy proof = DiscrepancyLog.Prove(a, b, claimedNationId, claimedEraId, Traveller);
        return log.Add(proof) ? proof : null;
    }

    // -----------------------------
    // Record proof (identity)
    // -----------------------------

    [Test]
    public void BirthDateTell_VsTheRecordsBornRow_Registers()
    {
        // Redesign phase 2: a record is rows; its Born row is the evidence the Records app picks.
        var record = new CitizenRecord(Traveller, null, new[]
        {
            new RecordGroup("REGISTRY ENTRY", new[]
            {
                new RecordRow("Name", Traveller, ClueCategory.Name),
                new RecordRow("Born", "3 May 1131", ClueCategory.BirthDate),
                new RecordRow("Origin", "Norvik (Medieval)")
            })
        });
        RecordRow born = record.Groups[0].Rows.Single(r => r.IsEvidence && r.Category == ClueCategory.BirthDate);

        Discrepancy d = DiscrepancyLog.Prove(TellIdentityField(ClueCategory.BirthDate, "3 May 1101"),
            CompareEvidence.ForRecordField(born.Category, born.Value, record.FullName), ClaimNation, ClaimEra, Traveller);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.RecordMismatch, d.provedBy);
        Assert.AreEqual("3 May 1131", d.expectedValue);
    }

    [Test]
    public void BirthDateTell_VsRecord_Registers_AsRecordMismatch()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, 
            TellIdentityField(ClueCategory.BirthDate, "3 May 1101"),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131", Traveller),
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

        Assert.IsNull(Register(log, honest, CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131", Traveller), ClaimNation, ClaimEra));
    }

    [Test]
    public void RecordField_VsDifferentCategoryDoc_DoesNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, 
            TellDocField(), // Technology
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131", Traveller),
            ClaimNation, ClaimEra));
    }

    [Test]
    public void TwoRecordFields_DoNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, 
            CompareEvidence.ForRecordField(ClueCategory.Name, "Bjorn", Traveller),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131", Traveller),
            ClaimNation, ClaimEra));
    }

    /// <summary>
    /// Phase 0 of the audit: any record row proved a birth-date tell, so
    /// another traveller's record (whose date always differs) documented a
    /// "contradiction". Only the traveller's own record proves one.
    /// </summary>
    [Test]
    public void RecordProof_AnotherTravellersRecord_ProvesNothing()
    {
        Assert.IsNull(DiscrepancyLog.Prove(
            TellIdentityField(ClueCategory.BirthDate, "3 May 1101"),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "14 Sep 2401", "Zara-7"),
            ClaimNation, ClaimEra, Traveller));
        Assert.IsNull(DiscrepancyLog.Prove(
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "14 Sep 2401", "Zara-7"),
            TellIdentityField(ClueCategory.BirthDate, "3 May 1101"),
            ClaimNation, ClaimEra, Traveller), "either side first");
    }

    /// <summary>Phase 0 of the audit: an empty record value proved any tell (it differs from every date).</summary>
    [Test]
    public void RecordProof_AnEmptyRecordValue_ProvesNothing()
    {
        foreach (string empty in new[] { null, "", "   " })
            Assert.IsNull(DiscrepancyLog.Prove(
                TellIdentityField(ClueCategory.BirthDate, "3 May 1101"),
                CompareEvidence.ForRecordField(ClueCategory.BirthDate, empty, Traveller),
                ClaimNation, ClaimEra, Traveller), $"'{empty}'");
    }

    /// <summary>Without a traveller, or a record without an owner, nothing is proved; the owner is matched as every value is (trimmed, any case).</summary>
    [Test]
    public void RecordProof_NeedsTheRecordToNameTheTraveller()
    {
        CompareEvidence tell = TellIdentityField(ClueCategory.BirthDate, "3 May 1101");
        Assert.IsNull(DiscrepancyLog.Prove(tell, CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131", Traveller), ClaimNation, ClaimEra, null));
        Assert.IsNull(DiscrepancyLog.Prove(tell, CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131", null), ClaimNation, ClaimEra, Traveller));
        Assert.NotNull(DiscrepancyLog.Prove(tell, CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131", " bjorn "), ClaimNation, ClaimEra, Traveller));
    }

    [Test]
    public void RecordProof_WorksEvenWithoutClaimEra()
    {
        // Identity has nothing to do with the travel claim.
        var log = new DiscrepancyLog();
        Discrepancy d = Register(log, 
            TellIdentityField(ClueCategory.BirthDate, "3 May 1101"),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131", Traveller),
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

    /// <summary>Audit R1-010: the origin proof names the row's label as it is; FactTable guarantees a label (R1-017), so Domain carries no English fallback.</summary>
    [Test]
    public void OriginProof_NamesTheRowsLabelVerbatim()
    {
        Discrepancy d = DiscrepancyLog.Prove(TellDocField("Aqueduct"), CompareEvidence.ForReferenceEntry(ClueCategory.Technology, "Aqueduct", "latia", "rome", " Latia — Republican Rome "), ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual(" Latia — Republican Rome ", d.actualOrigin);
        Assert.AreEqual(d.actualOrigin, d.ReportOther);
        Assert.Throws<System.ArgumentException>(() => new FactTable().Add("latia", "rome", " ", ClueCategory.Technology, "Aqueduct"), "a book row always has a label");
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
    public void TwoDocumentFields_OfOnePaper_DoNotRegister()
    {
        // Both on the first paper (document 0, the default): the cross proof needs two papers (Paper below).
        var log = new DiscrepancyLog();
        Assert.IsNull(Register(log, TellDocField(), TellDocField("Something"), ClaimNation, ClaimEra));
    }

    // -----------------------------
    // Two papers that disagree (redesign phase 7, traveller types L4)
    // -----------------------------

    /// <summary>A field of paper <paramref name="document"/> (EvidencePicks.ForField gives the index).</summary>
    private static CompareEvidence Paper(int document, ClueCategory category, string value, bool isTell) =>
        CompareEvidence.FromDocumentField(new DocumentField { category = category, value = value, isAnachronism = isTell }, document);

    [Test]
    public void TwoPapers_ThatDisagreeOnACategory_OneATell_ProveACrossMismatch_NamingNeither()
    {
        // A borrowed manifest: its Citizen ID (the tell) is not the visa's.
        Discrepancy d = DiscrepancyLog.Prove(
            Paper(1, ClueCategory.CitizenId, "552-1804-33", true),
            Paper(0, ClueCategory.CitizenId, "418-0937-52", false),
            ClaimNation, ClaimEra, Traveller);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.CrossMismatch, d.provedBy);
        Assert.AreEqual(ClueCategory.CitizenId, d.category);
        Assert.AreEqual(EvidenceKind.DocumentField, d.source);
        Assert.AreEqual("deviation.crossMismatch.papers", d.ReportKey);
        Assert.AreEqual("418-0937-52", d.documentValue, "the first paper's value, in paper order, whichever side was picked first");
        Assert.AreEqual("552-1804-33", d.ReportOther, "the other paper's value");
        Assert.IsNull(d.actualOrigin);
    }

    [Test]
    public void TheCrossProof_WorksWithoutAClaimEra_AndWithTheTellOnEitherSide()
    {
        Discrepancy d = DiscrepancyLog.Prove(Paper(0, ClueCategory.AccountStatus, "Premium", true), Paper(1, ClueCategory.AccountStatus, "Standard", false), null, null, null);
        Assert.AreEqual(DiscrepancyProof.CrossMismatch, d?.provedBy);
        Assert.AreEqual("Premium", d.documentValue);
        Assert.AreEqual("Standard", d.ReportOther);
    }

    [Test]
    public void TwoPapers_ThatDisagree_WithNoTell_ProveNothing()
    {
        // Two honest boxes never disagree; a coincidence between two honest values is not a proof.
        Assert.IsNull(DiscrepancyLog.Prove(Paper(0, ClueCategory.CitizenId, "418-0937-52", false), Paper(1, ClueCategory.CitizenId, "552-1804-33", false), ClaimNation, ClaimEra, Traveller));
    }

    [Test]
    public void TheCrossProof_NeedsTwoPapers_OneCategory_AndValuesThatDiffer()
    {
        Assert.IsNull(DiscrepancyLog.Prove(Paper(1, ClueCategory.CitizenId, "552-1804-33", true), Paper(1, ClueCategory.CitizenId, "418-0937-52", false), ClaimNation, ClaimEra, Traveller), "one paper");
        Assert.IsNull(DiscrepancyLog.Prove(Paper(-1, ClueCategory.CitizenId, "552-1804-33", true), Paper(0, ClueCategory.CitizenId, "418-0937-52", false), ClaimNation, ClaimEra, Traveller), "a paper unknown");
        Assert.IsNull(DiscrepancyLog.Prove(Paper(1, ClueCategory.CitizenId, "552-1804-33", true), Paper(0, ClueCategory.TransponderId, "418-0937-52", false), ClaimNation, ClaimEra, Traveller), "two categories");
        Assert.IsNull(DiscrepancyLog.Prove(Paper(1, ClueCategory.CitizenId, " 418-0937-52 ", true), Paper(0, ClueCategory.CitizenId, "418-0937-52", false), ClaimNation, ClaimEra, Traveller), "the same value");
        Assert.IsNull(DiscrepancyLog.Prove(Paper(1, ClueCategory.Expiry, "1 Jan 2150", true), Paper(0, ClueCategory.Expiry, "27 Mar 2150", false), ClaimNation, ClaimEra, Traveller), "directive-only");
        Assert.IsNull(DiscrepancyLog.Prove(Paper(1, ClueCategory.Name, "Mara", true), Paper(0, ClueCategory.Name, "Nebamun", false), ClaimNation, ClaimEra, Traveller), "a name");
    }

    [Test]
    public void AnAnswerAgainstAPaper_StaysAHint_EvenWhenThePaperIsATell()
    {
        // Papers prove, answers hint (Saleh's Q3): only two papers cross-prove.
        Assert.IsNull(DiscrepancyLog.Prove(CompareEvidence.ForAnswer(ClueCategory.Currency, "Deben", false), Paper(1, ClueCategory.Currency, "Denarius", true), ClaimNation, ClaimEra, Traveller));
        Assert.IsNull(DiscrepancyLog.Prove(CompareEvidence.ForAnswer(ClueCategory.Currency, "Denarius", true), Paper(1, ClueCategory.Currency, "Deben", false), ClaimNation, ClaimEra, Traveller));
    }

    [Test]
    public void OneProofPerCategory_TheCrossProofAndTheRecordProofOfOneCategory_DocumentOnce()
    {
        var log = new DiscrepancyLog();
        Assert.IsTrue(log.Add(DiscrepancyLog.Prove(Paper(1, ClueCategory.CitizenId, "552-1804-33", true), Paper(0, ClueCategory.CitizenId, "418-0937-52", false), ClaimNation, ClaimEra, Traveller)));
        Assert.IsFalse(log.Add(DiscrepancyLog.Prove(Paper(1, ClueCategory.CitizenId, "552-1804-33", true), CompareEvidence.ForRecordField(ClueCategory.CitizenId, "418-0937-52", Traveller), ClaimNation, ClaimEra, Traveller)));
        Assert.AreEqual(1, log.Count);
        Assert.AreEqual(DiscrepancyProof.CrossMismatch, log.Items[0].provedBy);
    }

    [Test]
    public void EveryRecordCategory_ProvesAgainstTheTravellersRecord()
    {
        foreach (ClueCategory category in new[] { ClueCategory.CitizenId, ClueCategory.AccountStatus, ClueCategory.TransponderId, ClueCategory.TransponderClass, ClueCategory.Debt, ClueCategory.Destination, ClueCategory.Incident })
        {
            Discrepancy d = DiscrepancyLog.Prove(Paper(0, category, "forged", true), CompareEvidence.ForRecordField(category, "on file", Traveller), ClaimNation, ClaimEra, Traveller);
            Assert.AreEqual(DiscrepancyProof.RecordMismatch, d?.provedBy, category.ToString());
            Assert.AreEqual("deviation.recordMismatch.papers", d.ReportKey, category.ToString());
        }
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
        Discrepancy d = DiscrepancyLog.Prove(SaidDevice(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra, Traveller);

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
        Discrepancy d = DiscrepancyLog.Prove(Entry("latia", "rome", "Aqueduct"), SaidDevice(), ClaimNation, ClaimEra, Traveller);

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
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 Jun 1510 BCE", Traveller),
            ClaimNation, ClaimEra, Traveller);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.RecordMismatch, d.provedBy);
        Assert.AreEqual("deviation.recordMismatch.said", d.ReportKey);
        Assert.AreEqual("3 Jun 1801 BCE", d.documentValue);
        Assert.AreEqual("3 Jun 1510 BCE", d.ReportOther);
    }

    [Test]
    public void HonestAnswer_NeverRegisters_EitherWayOrOrder()
    {
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice("Longship", false), Entry("norvik", "medieval", "Waterwheel"), ClaimNation, ClaimEra, Traveller));
        Assert.IsNull(DiscrepancyLog.Prove(Entry("latia", "rome", "Longship"), SaidDevice("Longship", false), ClaimNation, ClaimEra, Traveller));
        Assert.IsNull(DiscrepancyLog.Prove(
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 May 1131", Traveller),
            CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 May 1101", false),
            ClaimNation, ClaimEra, Traveller));
    }

    // -----------------------------
    // Worn garments (the look menu)
    // -----------------------------

    /// <summary>A garment: a liar's dress tell by default, valued with its place's Culture fact.</summary>
    private static CompareEvidence Wears(string value = "top hat / poke bonnet", bool isTell = true) =>
        CompareEvidence.ForAppearance(ClueCategory.Culture, value, isTell);

    [Test]
    public void DressTell_VsTheClaimsRow_ProvesAMismatch_WornByTheTraveller()
    {
        Discrepancy d = DiscrepancyLog.Prove(Wears(), Entry("norvik", "medieval", "chonmage / shimada", ClueCategory.Culture), ClaimNation, ClaimEra, Traveller);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d.provedBy);
        Assert.AreEqual(EvidenceKind.Appearance, d.source);
        Assert.AreEqual("deviation.claimMismatch.worn", d.ReportKey);
        Assert.AreEqual("chonmage / shimada", d.ReportOther);
    }

    [Test]
    public void DressTell_VsTheTrueHomesRow_ProvesTheOrigin_WornByTheTraveller()
    {
        Discrepancy d = DiscrepancyLog.Prove(Entry("britain", "industrial", "top hat / poke bonnet", ClueCategory.Culture), Wears(), ClaimNation, ClaimEra, Traveller);

        Assert.NotNull(d);
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d.provedBy);
        Assert.AreEqual(EvidenceKind.Appearance, d.source);
        Assert.AreEqual("deviation.foreignOrigin.worn", d.ReportKey);
        Assert.AreEqual("britain — industrial", d.ReportOther);
    }

    /// <summary>
    /// A costume error (traveller types C2, C3): a 2150 garment is valued with
    /// the present's Costume Guide row, so against the claimed row it is DRESS
    /// INCORRECT and against the present's row it names 2150.
    /// </summary>
    [Test]
    public void A2150Garment_VsThePresentsRow_Names2150_AndVsTheClaimsRow_IsAMismatch()
    {
        CompareEvidence present = CompareEvidence.ForReferenceEntry(ClueCategory.Culture, "panelled coat-dress", "neutral", "future", "Temporal Customs Zone (2150)");
        Discrepancy origin = DiscrepancyLog.Prove(Wears("panelled coat-dress"), present, ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual(DiscrepancyProof.ForeignOrigin, origin.provedBy);
        Assert.AreEqual("Temporal Customs Zone (2150)", origin.ReportOther);
        Assert.AreEqual("deviation.foreignOrigin.worn", origin.ReportKey);

        Discrepancy mismatch = DiscrepancyLog.Prove(Wears("panelled coat-dress"), Entry("norvik", "medieval", "chonmage / shimada", ClueCategory.Culture), ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual(DiscrepancyProof.ClaimMismatch, mismatch.provedBy);
        Assert.AreEqual("deviation.claimMismatch.worn", mismatch.ReportKey);
    }

    [Test]
    public void HonestGarment_NeverRegisters_AndGarmentsProveNothingAgainstStatements()
    {
        Assert.IsNull(DiscrepancyLog.Prove(Wears("chonmage / shimada", false), Entry("norvik", "medieval", "wesekh collar", ClueCategory.Culture), ClaimNation, ClaimEra, Traveller));
        Assert.IsNull(DiscrepancyLog.Prove(Entry("latia", "rome", "chonmage / shimada", ClueCategory.Culture), Wears("chonmage / shimada", false), ClaimNation, ClaimEra, Traveller));
        Assert.IsNull(DiscrepancyLog.Prove(Wears(), Entry("latia", "rome", "wesekh collar", ClueCategory.Culture), ClaimNation, ClaimEra, Traveller), "a third place's row");
        Assert.IsNull(DiscrepancyLog.Prove(Wears(), TellDocField(), ClaimNation, ClaimEra, Traveller), "garment vs papers");
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice(), Wears(), ClaimNation, ClaimEra, Traveller), "garment vs answer");
        Assert.IsNull(DiscrepancyLog.Prove(Wears(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra, Traveller), "another category's row");
    }

    [Test]
    public void MatchValue_IsTheEvidenceValue_WhenASideCarriesEvidence_ElseTheShownText()
    {
        Assert.AreEqual("shown", default(CompareEvidence).MatchValue("shown"), "no evidence: the text shown");
        Assert.AreEqual("top hat / poke bonnet", Wears().MatchValue("top hat"), "a garment shows its item and matches on its place's value");
        Assert.AreEqual("Aqueduct", TellDocField().MatchValue("Aqueduct"));
        Assert.AreEqual("Longship", Entry("norvik", "medieval", "Longship").MatchValue("Longship"));
        Assert.IsTrue(Values.Match(Wears().MatchValue("top hat"), Entry("b", "i", " TOP HAT / poke bonnet ", ClueCategory.Culture).MatchValue("x")));
    }

    [Test]
    public void AnswerVsPapers_AndAnswerVsAnswer_ProveNothing()
    {
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice(), HonestDocField(), ClaimNation, ClaimEra, Traveller));
        Assert.IsNull(DiscrepancyLog.Prove(TellDocField(), SaidDevice("Longship", false), ClaimNation, ClaimEra, Traveller));
        Assert.IsNull(DiscrepancyLog.Prove(SaidDevice(), SaidDevice("Longship"), ClaimNation, ClaimEra, Traveller));
    }

    [Test]
    public void PaperReports_UseThePapersKeys_AndTheCategoryWord()
    {
        Discrepancy mismatch = DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual(EvidenceKind.DocumentField, mismatch.source);
        Assert.AreEqual("deviation.claimMismatch.papers", mismatch.ReportKey);
        Assert.AreEqual("Longship", mismatch.ReportOther);
        Assert.AreEqual("category.Technology", ClueLabels.Key(mismatch.category));

        Discrepancy origin = DiscrepancyLog.Prove(TellDocField(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual("deviation.foreignOrigin.papers", origin.ReportKey);
        Assert.AreEqual("latia — rome", origin.ReportOther);
    }

    [Test]
    public void Add_RefusesASecondProofOfADocumentedCategory_FromEitherSource_AndNull()
    {
        var log = new DiscrepancyLog();
        Assert.IsTrue(log.Add(DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra, Traveller)));
        Assert.IsFalse(log.Add(DiscrepancyLog.Prove(SaidDevice(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra, Traveller)), "same category, spoken");
        Assert.IsFalse(log.Add(null));
        Assert.AreEqual(1, log.Count);
        Assert.AreEqual(EvidenceKind.DocumentField, log.Items[0].source);
    }

    [Test]
    public void Prove_IsPure_ADocumentedCategoryStillProves_TheSameWayTwice()
    {
        var log = new DiscrepancyLog();
        Assert.IsTrue(log.Add(DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra, Traveller)));

        // Technology is documented now; Prove never reads the log, so the spoken pair still proves.
        Discrepancy x = DiscrepancyLog.Prove(SaidDevice(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra, Traveller);
        Discrepancy y = DiscrepancyLog.Prove(SaidDevice(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra, Traveller);

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
    [TestCase(DiscrepancyProof.ClaimMismatch, EvidenceKind.Appearance, "deviation.claimMismatch.worn")]
    [TestCase(DiscrepancyProof.ForeignOrigin, EvidenceKind.Appearance, "deviation.foreignOrigin.worn")]
    [TestCase(DiscrepancyProof.CrossMismatch, EvidenceKind.DocumentField, "deviation.crossMismatch.papers")]
    [TestCase(DiscrepancyProof.CrossMismatch, EvidenceKind.Answer, "deviation.crossMismatch.papers")]
    [TestCase(DiscrepancyProof.CrossMismatch, EvidenceKind.Appearance, "deviation.crossMismatch.papers")]
    public void ReportKeyFor_NamesTheProofAndWhoStatedIt(DiscrepancyProof proof, EvidenceKind statement, string expected)
    {
        Assert.AreEqual(expected, Discrepancy.ReportKeyFor(proof, statement));
    }

    [Test]
    public void ReportKey_AndReportOther_OfRealProofs()
    {
        Discrepancy mismatch = DiscrepancyLog.Prove(TellDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual("deviation.claimMismatch.papers", mismatch.ReportKey);
        Assert.AreEqual("Longship", mismatch.ReportOther, "the expected value");

        Discrepancy origin = DiscrepancyLog.Prove(Entry("latia", "rome", "Aqueduct"), SaidDevice(), ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual("deviation.foreignOrigin.said", origin.ReportKey);
        Assert.AreEqual("latia — rome", origin.ReportOther, "the place the value belongs to");

        Discrepancy record = DiscrepancyLog.Prove(
            CompareEvidence.ForAnswer(ClueCategory.BirthDate, "3 Jun 1801 BCE", true),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, "3 Jun 1510 BCE", Traveller),
            ClaimNation, ClaimEra, Traveller);
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
    [TestCase(ClueCategory.CitizenId, "category.CitizenId")]
    [TestCase(ClueCategory.Destination, "category.Destination")]
    [TestCase(ClueCategory.Incident, "category.Incident")]
    [TestCase(ClueCategory.DepartureDate, "category.DepartureDate")]
    [TestCase(ClueCategory.Expiry, "category.Expiry")]
    [TestCase(ClueCategory.AccountStatus, "category.AccountStatus")]
    [TestCase(ClueCategory.TransponderId, "category.TransponderId")]
    [TestCase(ClueCategory.TransponderClass, "category.TransponderClass")]
    [TestCase(ClueCategory.Debt, "category.Debt")]
    [TestCase(ClueCategory.WaiverNo, "category.WaiverNo")]
    [TestCase(ClueCategory.Credit, "category.Credit")]
    [TestCase(ClueCategory.Funds, "category.Funds")]
    [TestCase(ClueCategory.PolicyNo, "category.PolicyNo")]
    [TestCase(ClueCategory.Signature, "category.Signature")]
    public void ClueLabels_Key_OneKeyPerCategory(ClueCategory category, string expected)
    {
        Assert.AreEqual(expected, ClueLabels.Key(category));
    }

    /// <summary>The English UI string of <paramref name="key"/> in world_source.json (null when missing).</summary>
    private static string UiString(string key, [CallerFilePath] string here = "")
    {
        const string source = "Assets/Data/World/world_source.json";
        string path = File.Exists(source) ? source : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", source);
        ContentNode strings = ContentJson.Parse(File.ReadAllText(path)).Get("ui").Get("strings");
        ContentNode entry = strings.Items.FirstOrDefault(s => s.Get("key").Text == key);
        return entry?.Get("text").Text;
    }

    /// <summary>Every report line a real proof can produce has its template in the UI strings ({0} the category word, {1} the stated value, {2} ReportOther).</summary>
    [TestCase(DiscrepancyProof.ClaimMismatch, EvidenceKind.DocumentField)]
    [TestCase(DiscrepancyProof.ClaimMismatch, EvidenceKind.Answer)]
    [TestCase(DiscrepancyProof.ClaimMismatch, EvidenceKind.Appearance)]
    [TestCase(DiscrepancyProof.ForeignOrigin, EvidenceKind.DocumentField)]
    [TestCase(DiscrepancyProof.ForeignOrigin, EvidenceKind.Answer)]
    [TestCase(DiscrepancyProof.ForeignOrigin, EvidenceKind.Appearance)]
    [TestCase(DiscrepancyProof.RecordMismatch, EvidenceKind.DocumentField)]
    [TestCase(DiscrepancyProof.RecordMismatch, EvidenceKind.Answer)]
    [TestCase(DiscrepancyProof.RecordMismatch, EvidenceKind.Appearance)]
    [TestCase(DiscrepancyProof.CrossMismatch, EvidenceKind.DocumentField)]
    public void EveryReportLine_HasItsUiString(DiscrepancyProof proof, EvidenceKind statement)
    {
        string text = UiString(Discrepancy.ReportKeyFor(proof, statement));
        Assert.NotNull(text, Discrepancy.ReportKeyFor(proof, statement));
        StringAssert.Contains("{0}", text);
        StringAssert.Contains("{1}", text);
        StringAssert.Contains("{2}", text);
    }

    [Test]
    public void Categories_AreTheDocumentedOnes_OnePerCategory()
    {
        var log = new DiscrepancyLog();
        CollectionAssert.IsEmpty(log.Categories);

        Discrepancy Currency() => new Discrepancy { category = ClueCategory.Currency, documentValue = "denarius", expectedValue = "drachma", provedBy = DiscrepancyProof.ClaimMismatch, source = EvidenceKind.DocumentField };
        Assert.IsTrue(log.Add(Currency()));
        Assert.IsFalse(log.Add(Currency()), "one discrepancy per category");
        Assert.IsTrue(log.Add(new Discrepancy { category = ClueCategory.Language, documentValue = "Latin", expectedValue = "Attic Greek", provedBy = DiscrepancyProof.ClaimMismatch, source = EvidenceKind.Answer }));
        CollectionAssert.AreEquivalent(new[] { ClueCategory.Currency, ClueCategory.Language }, log.Categories);
        Assert.AreEqual(2, log.Count);
        Assert.IsFalse(log.Add(null));

        log.Clear();
        CollectionAssert.IsEmpty(log.Categories);
        Assert.AreEqual(0, log.Count);
        Assert.IsTrue(log.Add(Currency()), "a new case documents the category again");
    }

    /// <summary>Wave 5, lesson 3: a proof keeps the papers it names (the wheel's question about it names them): the stated paper, and a cross proof's other one; a spoken or worn statement is on no paper.</summary>
    [Test]
    public void Proofs_KeepThePapersTheyName()
    {
        CompareEvidence onPaper = TellDocField();
        onPaper.document = 2;
        Discrepancy byBook = DiscrepancyLog.Prove(onPaper, Entry(ClaimNation, ClaimEra, "Longship"), ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual((2, -1), (byBook.statementDocument, byBook.otherDocument));
        Discrepancy said = DiscrepancyLog.Prove(SaidDevice(), Entry(ClaimNation, ClaimEra, "Longship"), ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual((-1, -1), (said.statementDocument, said.otherDocument));

        var manifest = new CompareEvidence { kind = EvidenceKind.DocumentField, category = ClueCategory.TransponderClass, value = "Premium", isAnachronism = true, document = 3 };
        var visa = new CompareEvidence { kind = EvidenceKind.DocumentField, category = ClueCategory.TransponderClass, value = "Economy", document = 1 };
        Discrepancy cross = DiscrepancyLog.Prove(manifest, visa, ClaimNation, ClaimEra, Traveller);
        Assert.AreEqual(DiscrepancyProof.CrossMismatch, cross.provedBy);
        Assert.AreEqual((1, 3), (cross.statementDocument, cross.otherDocument), "in paper order");
        Assert.AreEqual(("Economy", "Premium"), (cross.documentValue, cross.expectedValue));
    }
}
