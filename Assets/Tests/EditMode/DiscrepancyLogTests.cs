using NUnit.Framework;

/// <summary>
/// Decision table for DiscrepancyLog.TryRegister — the core verification rule.
/// Claim under test: Norvik / Medieval. Truth for Technology: "Longship".
/// The forged papers print "Aqueduct" (which really belongs to Latia / Rome).
/// </summary>
public class DiscrepancyLogTests
{
    private const string ClaimNation = "norvik";
    private const string ClaimEra = "medieval";

    private static CompareEvidence ForgedDocField(string value = "Aqueduct") => new CompareEvidence
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

    // -----------------------------
    // Proof modalities
    // -----------------------------

    [Test]
    public void MismatchAgainstClaimedEraEntry_Registers()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = log.TryRegister(ForgedDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);

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
        Discrepancy d = log.TryRegister(ForgedDocField("Aqueduct"), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.IsNull(d.expectedValue);
        Assert.AreEqual("latia — rome", d.actualOrigin);
    }

    [Test]
    public void EraOnlyEntry_AppliesToAnyNationOfThatEra()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = log.TryRegister(ForgedDocField(), Entry(null, "medieval", "Longship"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
    }

    [Test]
    public void ArgumentOrder_DoesNotMatter()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = log.TryRegister(Entry("norvik", "medieval", "Longship"), ForgedDocField(), ClaimNation, ClaimEra);

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
        Assert.IsNull(log.TryRegister(ForgedDocField(), Entry("helios", "future", "Solar Sail"), ClaimNation, ClaimEra));
        Assert.AreEqual(0, log.Count);
    }

    [Test]
    public void HonestField_NeverRegisters_EitherWay()
    {
        var log = new DiscrepancyLog();
        // Mismatch path: honest Longship vs a wrong-value entry.
        Assert.IsNull(log.TryRegister(HonestDocField(), Entry("norvik", "medieval", "Waterwheel"), ClaimNation, ClaimEra));
        // Match path: honest Longship happens to match a foreign entry.
        Assert.IsNull(log.TryRegister(HonestDocField(), Entry("latia", "rome", "Longship"), ClaimNation, ClaimEra));
    }

    [Test]
    public void DifferentCategories_DoNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(log.TryRegister(ForgedDocField(), Entry("norvik", "medieval", "Silver Mark", ClueCategory.Currency), ClaimNation, ClaimEra));
    }

    [Test]
    public void TwoDocumentFields_DoNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(log.TryRegister(ForgedDocField(), ForgedDocField("Something"), ClaimNation, ClaimEra));
    }

    [Test]
    public void TwoReferenceEntries_DoNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(log.TryRegister(Entry("norvik", "medieval", "Longship"), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra));
    }

    [Test]
    public void OtherNationSameEraEntry_MismatchDoesNotRegister()
    {
        // Albion's Waterwheel is not Norvik's truth — a mismatch against it
        // proves nothing about the claim.
        var log = new DiscrepancyLog();
        Assert.IsNull(log.TryRegister(ForgedDocField(), Entry("albion", "medieval", "Waterwheel"), ClaimNation, ClaimEra));
    }

    [Test]
    public void OtherNationSameEraEntry_MatchRegisters_AsOriginProof()
    {
        // Forged value equals Albion/Medieval's truth while claiming Norvik:
        // the value provably belongs to Albion.
        var log = new DiscrepancyLog();
        Discrepancy d = log.TryRegister(ForgedDocField("Waterwheel"), Entry("albion", "medieval", "Waterwheel"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
        Assert.AreEqual("albion — medieval", d.actualOrigin);
    }

    [Test]
    public void NullClaimEra_DoesNotRegister()
    {
        var log = new DiscrepancyLog();
        Assert.IsNull(log.TryRegister(ForgedDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, null));
    }

    [Test]
    public void ValueComparison_IgnoresCaseAndWhitespace()
    {
        var log = new DiscrepancyLog();
        Discrepancy d = log.TryRegister(ForgedDocField("  aqueduct "), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra);

        Assert.NotNull(d);
    }

    // -----------------------------
    // Log behaviour
    // -----------------------------

    [Test]
    public void SameCategory_RegistersOnlyOnce()
    {
        var log = new DiscrepancyLog();
        Assert.NotNull(log.TryRegister(ForgedDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra));
        Assert.IsNull(log.TryRegister(ForgedDocField(), Entry("latia", "rome", "Aqueduct"), ClaimNation, ClaimEra));
        Assert.AreEqual(1, log.Count);
    }

    [Test]
    public void Clear_EmptiesTheLog()
    {
        var log = new DiscrepancyLog();
        log.TryRegister(ForgedDocField(), Entry("norvik", "medieval", "Longship"), ClaimNation, ClaimEra);
        log.Clear();

        Assert.AreEqual(0, log.Count);
    }
}
