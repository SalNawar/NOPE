using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The record lies (traveller types L1, L2, §6.2-6.3): each maker, the
/// variants and their draw order, that only the named fields are rewritten,
/// and that every forged field differs from the account and is provable.
/// The fixture is a Standard citizen ("418-0937-52", born 3 Jun 2101, on an
/// Economy Tick-Tock Basic) drawn from the rich entry, with the rich set:
/// TC-101 (Name, Citizen ID, Date of Birth, Destination, Visa Class, Valid
/// Until) and TC-230 (Citizen ID, Transponder, Transponder Class, Currency
/// Carried, Declared Effects, Departure), printed honestly.
/// </summary>
public class RecordLiesTests
{
    private const string Traveller = "Mara";
    private const string Id = "418-0937-52";
    private const string Cover = "3 Jun 2101";
    private const int YearMin = 2080;
    private const int YearMax = 2132;

    private static ScriptStep V(float roll) => ScriptStep.Value(roll);
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    private static CitizenAccount Standard() => new CitizenAccount
    {
        CitizenId = Id,
        Status = CitizenStatus.Standard,
        Debt = 12_000,
        Transponder = "Tick-Tock Basic · TT-40718",
        TransponderClass = TransponderClass.Economy,
        Departure = "14 Mar 2150"
    };

    private static TransponderModel Model(string id, TransponderClass c, string model, string prefix, float weight) =>
        new TransponderModel { id = id, transponderClass = c, model = model, prefix = prefix, weight = weight };

    /// <summary>The agency's models: three Premium (weights 3, 2, 1) and three Economy.</summary>
    private static List<TransponderModel> Transponders() => new List<TransponderModel>
    {
        Model("hopper2", TransponderClass.Premium, "Hopper Mk II", "HP", 3f),
        Model("aurelian9", TransponderClass.Premium, "Aurelian 9", "AU", 2f),
        Model("chronoselite", TransponderClass.Premium, "Chronos Elite", "CE", 1f),
        Model("ticktock", TransponderClass.Economy, "Tick-Tock Basic", "TT", 3f),
        Model("skiplite", TransponderClass.Economy, "Skip Lite", "SL", 2f),
        Model("driftbox3", TransponderClass.Economy, "Driftbox 3", "DB", 1f)
    };

    private static DocumentField F(ClueCategory category, string value) => new DocumentField { category = category, label = category.ToString(), value = value };

    private static List<DocumentField> VisaFields(CitizenAccount a, bool withBirthDate = true)
    {
        var fields = new List<DocumentField> { F(ClueCategory.Name, Traveller), F(ClueCategory.CitizenId, a.CitizenId) };
        if (withBirthDate)
            fields.Add(F(ClueCategory.BirthDate, Cover));
        fields.Add(F(ClueCategory.Destination, "New Kingdom Egypt (Ancient)"));
        fields.Add(F(ClueCategory.AccountStatus, a.Status.ToString()));
        fields.Add(F(ClueCategory.Expiry, "27 Mar 2150"));
        return fields;
    }

    private static List<DocumentField> ManifestFields(CitizenAccount a) => new List<DocumentField>
    {
        F(ClueCategory.CitizenId, a.CitizenId),
        F(ClueCategory.TransponderId, a.Transponder),
        F(ClueCategory.TransponderClass, a.TransponderClass.ToString()),
        F(ClueCategory.Currency, "Deben"),
        F(ClueCategory.Technology, "Papyrus"),
        F(ClueCategory.DepartureDate, a.Departure)
    };

    /// <summary>The rich set, honest, for <paramref name="a"/>.</summary>
    private static List<RecordForm> Forms(CitizenAccount a, bool withBirthDate = true) => new List<RecordForm>
    {
        new RecordForm(RecordLies.Visa, VisaFields(a, withBirthDate)),
        new RecordForm(RecordLies.Manifest, ManifestFields(a))
    };

    private static IReadOnlyList<IReadOnlyList<DocumentField>> Docs(IReadOnlyList<RecordForm> forms) => forms.Select(f => f.Fields).ToList();

    private static LiePlan Plan(LieKind kind, IRandomSource rng, CitizenAccount account = null, IReadOnlyList<RecordForm> forms = null,
                                int yearMin = YearMin, int yearMax = YearMax, ISet<string> taken = null, IReadOnlyList<TransponderModel> transponders = null)
    {
        account = account ?? Standard();
        return RecordLies.Plan(kind, forms ?? Forms(account), account, Cover, yearMin, yearMax, transponders ?? Transponders(), taken ?? Taken(account), rng);
    }

    /// <summary>Today's numbers: the account's own ID and serial (AccountMaker adds them) and a neighbour's.</summary>
    private static HashSet<string> Taken(CitizenAccount a) => new HashSet<string> { a.CitizenId, "TT-40718", "552-1804-33", "HP-00000" };

    // -----------------------------
    // L1 poor posing as rich
    // -----------------------------

    [Test]
    public void PoorPosingAsRich_Forged_DrawsTheVariantThenTheStatus_AndForgesTheTwoClasses()
    {
        var rng = new ScriptedRandom(R(0), R(0));
        LiePlan plan = Plan(LieKind.PoorPosingAsRich, rng);
        Assert.IsTrue(rng.Done, "the variant, then the status's one draw; the class draws nothing");
        Assert.AreEqual(LieOutcome.Forger, plan.Outcome);
        Assert.AreEqual(LieKind.PoorPosingAsRich, plan.Kind);
        Assert.AreEqual(-1, plan.HomeIndex);
        CollectionAssert.IsEmpty(plan.Tells);
        CollectionAssert.AreEqual(
            new[] { (0, ClueCategory.AccountStatus, "Premium"), (1, ClueCategory.TransponderClass, "Premium") },
            plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray());
    }

    [Test]
    public void PoorPosingAsRich_Borrowed_ForgesTheVisasClass_AndTheManifestsIdTransponderAndClass()
    {
        CitizenAccount account = Standard();
        HashSet<string> taken = Taken(account);
        // The variant (1: borrowed), the status, the fresh Citizen ID (three draws), the Premium model (one weighted draw), its serial.
        var rng = new ScriptedRandom(R(1), R(0), R(7), R(70), R(7), V(0.9f), R(12345));
        LiePlan plan = Plan(LieKind.PoorPosingAsRich, rng, account, taken: taken);
        Assert.IsTrue(rng.Done);
        Assert.AreEqual(LieOutcome.Forger, plan.Outcome);

        Assert.AreEqual(4, plan.RecordTells.Count);
        RecordTell status = plan.RecordTells[0], id = plan.RecordTells[1], transponder = plan.RecordTells[2], grade = plan.RecordTells[3];
        Assert.AreEqual((0, ClueCategory.AccountStatus, "Premium"), (status.Document, status.Category, status.Value));
        Assert.AreEqual((1, ClueCategory.CitizenId, "007-0070-07"), (id.Document, id.Category, id.Value), "a rich citizen's number, fresh today");
        Assert.AreEqual((1, ClueCategory.TransponderId, "Chronos Elite · CE-12345"), (transponder.Document, transponder.Category, transponder.Value), "0.9 of weights 3, 2, 1 falls on the third Premium model");
        Assert.AreEqual((1, ClueCategory.TransponderClass, "Premium"), (grade.Document, grade.Category, grade.Value));
        CollectionAssert.Contains(taken, "007-0070-07", "the borrowed number is nobody's today");
        CollectionAssert.Contains(taken, "CE-12345");
    }

    [Test]
    public void ABorrowedManifest_RewritesThatForm_NotTheVisasCitizenId_SoThePapersContradictEachOther()
    {
        CitizenAccount account = Standard();
        List<RecordForm> forms = Forms(account);
        LiePlan plan = Plan(LieKind.PoorPosingAsRich, new ScriptedRandom(R(1), R(0), R(7), R(70), R(7), V(0.9f), R(12345)), account, forms);
        plan.ApplyTo(Docs(forms));

        DocumentField visaId = forms[0].Fields.Single(f => f.category == ClueCategory.CitizenId);
        DocumentField manifestId = forms[1].Fields.Single(f => f.category == ClueCategory.CitizenId);
        Assert.AreEqual(Id, visaId.value);
        Assert.IsFalse(visaId.isAnachronism);
        Assert.AreEqual("007-0070-07", manifestId.value);
        Assert.IsTrue(manifestId.isAnachronism);

        List<PaperContradiction> cross = PaperChecks.Contradictions(Docs(forms));
        Assert.AreEqual(1, cross.Count, "the two IDs disagree; the classes and transponder are checked against the account, not each other");
        Assert.AreEqual(ClueCategory.CitizenId, cross[0].Category);
    }

    [Test]
    public void PoorPosingAsRich_APremiumAccount_HasNothingToForge_IsNoPossibleLie_WithNoDraw()
    {
        CitizenAccount rich = Standard();
        rich.Status = CitizenStatus.Premium;
        rich.TransponderClass = TransponderClass.Premium;
        rich.Transponder = "Hopper Mk II · HP-40718";
        var rng = new ScriptedRandom();
        LiePlan plan = Plan(LieKind.PoorPosingAsRich, rng, rich);
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome);
        Assert.AreEqual(LieKind.PoorPosingAsRich, plan.Kind);
        CollectionAssert.IsEmpty(plan.RecordTells);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void PoorPosingAsRich_WithoutAPremiumModel_TheBorrowedVariantCannotShow_SoTheForgedOneDrawsNoVariant()
    {
        List<TransponderModel> economyOnly = Transponders().Where(t => t.transponderClass == TransponderClass.Economy).ToList();
        var rng = new ScriptedRandom(R(0));
        LiePlan plan = Plan(LieKind.PoorPosingAsRich, rng, transponders: economyOnly);
        Assert.IsTrue(rng.Done, "the status's draw only");
        Assert.AreEqual(2, plan.RecordTells.Count);
    }

    // -----------------------------
    // L2 doctored identity
    // -----------------------------

    [Test]
    public void DoctoredIdentity_TheId_DrawsTheVariantThenAFreshNumber_OnTheVisaOnly()
    {
        CitizenAccount account = Standard();
        List<RecordForm> forms = Forms(account);
        var rng = new ScriptedRandom(R(0), R(552), R(1804), R(33), R(9), R(99), R(9));
        LiePlan plan = Plan(LieKind.DoctoredIdentity, rng, account, forms);
        Assert.IsTrue(rng.Done, "the variant; 552-1804-33 is taken today, so a second number is drawn");
        CollectionAssert.AreEqual(new[] { (0, ClueCategory.CitizenId, "009-0099-09") }, plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray());

        plan.ApplyTo(Docs(forms));
        Assert.AreEqual("009-0099-09", forms[0].Fields.Single(f => f.category == ClueCategory.CitizenId).value);
        Assert.AreEqual(Id, forms[1].Fields.Single(f => f.category == ClueCategory.CitizenId).value, "the manifest keeps the account's number");
        Assert.AreEqual(1, PaperChecks.Contradictions(Docs(forms)).Count, "so the visa and the manifest disagree");
    }

    [Test]
    public void DoctoredIdentity_TheYear_DrawsTheVariantThenTheYear_KeepingDayAndMonth()
    {
        var rng = new ScriptedRandom(R(1), R(0));
        LiePlan plan = Plan(LieKind.DoctoredIdentity, rng);
        Assert.IsTrue(rng.Done);
        RecordTell born = plan.RecordTells.Single();
        Assert.AreEqual((0, ClueCategory.BirthDate, "3 Jun 2080"), (born.Document, born.Category, born.Value), "the earliest year of the present's range");
    }

    [Test]
    public void DoctoredIdentity_AVariantThatCannotShow_IsNeverDrawn()
    {
        var noYears = new ScriptedRandom(R(0), R(0), R(0));
        LiePlan plan = Plan(LieKind.DoctoredIdentity, noYears, yearMin: 0, yearMax: 0);
        Assert.IsTrue(noYears.Done, "no variant draw: only the ID can show, three draws for the number");
        Assert.AreEqual(ClueCategory.CitizenId, plan.RecordTells.Single().Category);

        var noBirthBox = new ScriptedRandom(R(0), R(0), R(0));
        Assert.AreEqual(ClueCategory.CitizenId, Plan(LieKind.DoctoredIdentity, noBirthBox, forms: Forms(Standard(), withBirthDate: false)).RecordTells.Single().Category);
        Assert.IsTrue(noBirthBox.Done);
    }

    [Test]
    public void WithoutTheVisa_NothingCanBeForged_NoPossibleLie_WithNoDraw()
    {
        var forms = new List<RecordForm> { new RecordForm(RecordLies.Manifest, ManifestFields(Standard())) };
        var rng = new ScriptedRandom();
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.DoctoredIdentity, rng, forms: forms).Outcome);
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.PoorPosingAsRich, rng, forms: forms).Outcome);
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.PoorPosingAsRich, rng, forms: new List<RecordForm>()).Outcome);
        Assert.IsTrue(rng.Done);
    }

    // -----------------------------
    // Every forged field differs and is provable
    // -----------------------------

    [TestCase(LieKind.PoorPosingAsRich)]
    [TestCase(LieKind.DoctoredIdentity)]
    public void EveryForgedField_DiffersFromTheAccount_IsProvable_AndProvesAgainstTheRecord(LieKind kind)
    {
        for (int seed = 0; seed < 200; seed++)
        {
            CitizenAccount account = Standard();
            List<RecordForm> forms = Forms(account);
            List<DocumentField> honest = forms.SelectMany(f => f.Fields).Select(f => new DocumentField { category = f.category, value = f.value }).ToList();
            LiePlan plan = Plan(kind, new SeededRandom(seed), account, forms);
            Assert.AreEqual(LieOutcome.Forger, plan.Outcome, $"seed {seed}");
            Assert.IsNotEmpty(plan.RecordTells, $"seed {seed}");

            CitizenRecord record = AccountRecords.Record(Traveller, Cover, "New Kingdom Egypt (Ancient)", account, key => key);
            plan.ApplyTo(Docs(forms));
            List<DocumentField> printed = forms.SelectMany(f => f.Fields).ToList();
            for (int i = 0; i < printed.Count; i++)
            {
                DocumentField field = printed[i];
                bool forged = plan.RecordTells.Any(t => t.Category == field.category && forms[t.Document].Fields.Contains(field));
                Assert.AreEqual(forged, field.isAnachronism, $"seed {seed}: {field.category} flagged as forged");
                if (!forged)
                {
                    Assert.AreEqual(honest[i].value, field.value, $"seed {seed}: {field.category} untouched");
                    continue;
                }

                Assert.IsTrue(Forgery.IsProvableCategory(field.category, null), $"seed {seed}: {field.category} is provable");
                RecordRow row = record.Groups.SelectMany(g => g.Rows).First(r => r.IsEvidence && r.Category == field.category);
                Assert.IsFalse(Values.Match(field.value, row.Value), $"seed {seed}: {field.category} '{field.value}' differs from the account's '{row.Value}'");
                Discrepancy proof = DiscrepancyLog.Prove(CompareEvidence.FromDocumentField(field, 0), CompareEvidence.ForRecordField(row.Category, row.Value, record.FullName), "egypt", "ancient", Traveller);
                Assert.AreEqual(DiscrepancyProof.RecordMismatch, proof?.provedBy, $"seed {seed}: {field.category} proves against the record");
            }
        }
    }

    [Test]
    public void TheSameSeed_GivesTheSamePlan()
    {
        foreach (LieKind kind in new[] { LieKind.PoorPosingAsRich, LieKind.DoctoredIdentity })
        {
            CitizenAccount a = Standard(), b = Standard();
            LiePlan x = Plan(kind, new SeededRandom(11), a, taken: Taken(a));
            LiePlan y = Plan(kind, new SeededRandom(11), b, taken: Taken(b));
            CollectionAssert.AreEqual(x.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray(), y.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray(), kind.ToString());
        }
    }

    [Test]
    public void NoStream_NoAccount_OrAPlaceLie_IsHonest_WithNoDraw()
    {
        var rng = new ScriptedRandom();
        Assert.AreEqual(LieOutcome.Honest, Plan(LieKind.DoctoredIdentity, null).Outcome);
        Assert.AreEqual(LieOutcome.Honest, RecordLies.Plan(LieKind.DoctoredIdentity, Forms(Standard()), null, Cover, YearMin, YearMax, Transponders(), Taken(Standard()), rng).Outcome);
        LiePlan place = Plan(LieKind.FalseOrigin, rng);
        Assert.AreEqual(LieOutcome.Honest, place.Outcome);
        Assert.AreEqual(LieKind.FalseOrigin, place.Kind);
        Assert.IsTrue(rng.Done);
    }

    // -----------------------------
    // The makers
    // -----------------------------

    [Test]
    public void FalseStatus_IsOneClassUp_AfterOneDraw_AndNothingAbovePremium()
    {
        var standard = new ScriptedRandom(R(0));
        Assert.AreEqual(CitizenStatus.Premium, RecordLies.FalseStatus(CitizenStatus.Standard, standard));
        Assert.IsTrue(standard.Done, "one draw even with one higher status");

        Assert.AreEqual(CitizenStatus.Standard, RecordLies.FalseStatus(CitizenStatus.Eligible, new ScriptedRandom(R(0))));
        Assert.AreEqual(CitizenStatus.Premium, RecordLies.FalseStatus(CitizenStatus.Eligible, new ScriptedRandom(R(1))));

        var premium = new ScriptedRandom();
        Assert.IsNull(RecordLies.FalseStatus(CitizenStatus.Premium, premium));
        Assert.IsTrue(premium.Done);
    }

    [Test]
    public void FalseTransponderClass_IsEconomyToPremium_Only()
    {
        Assert.AreEqual(TransponderClass.Premium, RecordLies.FalseTransponderClass(TransponderClass.Economy));
        Assert.IsNull(RecordLies.FalseTransponderClass(TransponderClass.Premium));
    }

    [Test]
    public void FreshCitizenId_IsNobodysToday_AndJoinsTheDaysNumbers()
    {
        var taken = new HashSet<string> { "001-0001-01" };
        var rng = new ScriptedRandom(R(1), R(1), R(1), R(2), R(2), R(2));
        Assert.AreEqual("002-0002-02", RecordLies.FreshCitizenId(taken, rng));
        Assert.IsTrue(rng.Done, "the taken number is redrawn");
        CollectionAssert.Contains(taken, "002-0002-02");
    }

    [Test]
    public void FalseTransponder_IsAnotherModelOfTheClassNeeded_WithAFreshSerial()
    {
        var taken = new HashSet<string> { "HP-00001" };
        var rng = new ScriptedRandom(V(0f), R(1), R(2));
        string forged = RecordLies.FalseTransponder(TransponderClass.Premium, "Tick-Tock Basic · TT-40718", Transponders(), taken, rng);
        Assert.AreEqual("Hopper Mk II · HP-00002", forged, "0 of weights 3, 2, 1 is the first Premium model; HP-00001 is taken, so the serial is redrawn");
        Assert.IsTrue(rng.Done);
        CollectionAssert.Contains(taken, "HP-00002");
    }

    [Test]
    public void FalseTransponder_NeverTheOwnModel_WhenAnotherExists_ElseTheSameModelWithAFreshSerial()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            string forged = RecordLies.FalseTransponder(TransponderClass.Premium, "Hopper Mk II · HP-40718", Transponders(), new HashSet<string>(), new SeededRandom(seed));
            StringAssert.DoesNotStartWith("Hopper Mk II", forged, $"seed {seed}");
        }

        var only = new List<TransponderModel> { Model("hopper2", TransponderClass.Premium, "Hopper Mk II", "HP", 3f) };
        string same = RecordLies.FalseTransponder(TransponderClass.Premium, "Hopper Mk II · HP-40718", only, new HashSet<string> { "HP-40718" }, new ScriptedRandom(V(0f), R(5)));
        Assert.AreEqual("Hopper Mk II · HP-00005", same);

        var none = new ScriptedRandom();
        Assert.IsNull(RecordLies.FalseTransponder(TransponderClass.Premium, "Tick-Tock Basic · TT-40718", Transponders().Where(t => t.transponderClass == TransponderClass.Economy).ToList(), new HashSet<string>(), none));
        Assert.IsTrue(none.Done);
    }
}
