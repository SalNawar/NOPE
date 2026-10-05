using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The record lies (traveller types L1-L5, L10, §6.2-6.3): each maker, the
/// variants and their draw order, that only the named fields are rewritten,
/// and that every forged field differs from the account and is provable.
/// The fixture is a Standard citizen ("TLZ-052", born 3 Jun 2101, on an
/// Economy Tick-Tock Basic) drawn from the rich entry, with the rich set:
/// TC-101 (Name, Citizen ID, Date of Birth, Destination, Visa Class, Valid
/// Until) and TC-230 (Citizen ID, Transponder, Transponder Class, Currency
/// Carried, Declared Effects, Departure), printed honestly.
/// </summary>
public class RecordLiesTests
{
    private const string Traveller = "Mara";
    private const string Id = "TLZ-052";
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

    /// <summary>The era's employers (the labourer's own first) and today's other open places.</summary>
    private static readonly string[] Employers = { "Tyburn Mills Consortium", "Ruhr Colliery Partners" };
    private static readonly string[] OpenPlaces = { "Meiji Osaka (Industrial)", "Bismarck Berlin (Industrial)" };

    private static RecordLieContext Context(CitizenAccount account, int yearMin = YearMin, int yearMax = YearMax, ISet<string> taken = null,
                                            IReadOnlyList<TransponderModel> transponders = null, CitizenStatus? posed = null) => new RecordLieContext
    {
        CoverBirthDate = Cover,
        BirthYearMin = yearMin,
        BirthYearMax = yearMax,
        Transponders = transponders ?? Transponders(),
        TakenToday = taken ?? Taken(account),
        PosedStatus = posed,
        Employers = Employers,
        OpenPlaces = OpenPlaces,
        WaiverPrefix = "SW",
        Proofs = Proofs(),
        Canon = Canon
    };

    /// <summary>The published fault canon (world_source.json agency.faults): the variants the lies draw from.</summary>
    private static readonly List<FaultEntry> Canon = ContentFixture.Faults();

    private static LiePlan Plan(LieKind kind, IRandomSource rng, CitizenAccount account = null, IReadOnlyList<RecordForm> forms = null,
                                int yearMin = YearMin, int yearMax = YearMax, ISet<string> taken = null, IReadOnlyList<TransponderModel> transponders = null,
                                CitizenStatus? posed = null)
    {
        account = account ?? Standard();
        return RecordLies.Plan(kind, forms ?? Forms(account), account, Context(account, yearMin, yearMax, taken, transponders, posed), rng);
    }

    /// <summary>An Eligible citizen ("TLZ-052", 212,000 cr in debt, on an Economy Tick-Tock Basic).</summary>
    private static CitizenAccount Eligible()
    {
        CitizenAccount a = Standard();
        a.Status = CitizenStatus.Eligible;
        a.Debt = 212_000;
        return a;
    }

    /// <summary>A labourer's account: Eligible, with a registered contract (Tyburn Mills Consortium, 180 days, 420 cr a day).</summary>
    private static CitizenAccount Labourer()
    {
        CitizenAccount a = Eligible();
        a.Employer = Employers[0];
        a.TermDays = 180;
        a.Wage = 420;
        return a;
    }

    private static List<DocumentField> ContractFields(CitizenAccount a) => new List<DocumentField>
    {
        F(ClueCategory.Name, Traveller),
        F(ClueCategory.CitizenId, a.CitizenId),
        F(ClueCategory.Employer, a.Employer),
        F(ClueCategory.Destination, "Victorian Britain (Industrial)"),
        F(ClueCategory.Term, AccountMaker.Term(a.TermDays)),
        F(ClueCategory.Wage, AccountMaker.Credits(a.Wage))
    };

    /// <summary>The labour set, honest, for <paramref name="a"/>: TC-520 then TC-230.</summary>
    private static List<RecordForm> LabourForms(CitizenAccount a) => new List<RecordForm>
    {
        new RecordForm(RecordLies.Contract, ContractFields(a)),
        new RecordForm(RecordLies.Manifest, ManifestFields(a))
    };

    /// <summary>Today's numbers: the account's own ID and serial (AccountMaker adds them) and a neighbour's.</summary>
    private static HashSet<string> Taken(CitizenAccount a) => new HashSet<string> { a.CitizenId, "TT-40718", "ZAZ-033", "HP-00000" };

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

    /// <summary>Lesson D7: on day 1 the visa travels alone; poor posing as rich forges its class only (the borrowed variant needs a manifest, so no variant draw).</summary>
    [Test]
    public void PoorPosingAsRich_OnTheVisaAlone_ForgesItsClass_WithNoVariantDraw()
    {
        CitizenAccount account = Standard();
        var visaOnly = new List<RecordForm> { new RecordForm(RecordLies.Visa, VisaFields(account, true)) };
        var rng = new ScriptedRandom(R(0));
        LiePlan plan = Plan(LieKind.PoorPosingAsRich, rng, account, visaOnly);
        Assert.IsTrue(rng.Done, "the status's draw only");
        Assert.AreEqual(LieOutcome.Forger, plan.Outcome);
        CollectionAssert.AreEqual(new[] { (0, ClueCategory.AccountStatus, "Premium") }, plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray());
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
        Assert.AreEqual((1, ClueCategory.CitizenId, "AHZ-007"), (id.Document, id.Category, id.Value), "a rich citizen's number, fresh today");
        Assert.AreEqual((1, ClueCategory.TransponderId, "Chronos Elite · CE-12345"), (transponder.Document, transponder.Category, transponder.Value), "0.9 of weights 3, 2, 1 falls on the third Premium model");
        Assert.AreEqual((1, ClueCategory.TransponderClass, "Premium"), (grade.Document, grade.Category, grade.Value));
        CollectionAssert.Contains(taken, "AHZ-007", "the borrowed number is nobody's today");
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
        Assert.AreEqual("AHZ-007", manifestId.value);
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
        Assert.IsTrue(rng.Done, "the variant; ZAZ-033 is taken today, so a second number is drawn");
        CollectionAssert.AreEqual(new[] { (0, ClueCategory.CitizenId, "AKZ-009") }, plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray());

        plan.ApplyTo(Docs(forms));
        Assert.AreEqual("AKZ-009", forms[0].Fields.Single(f => f.category == ClueCategory.CitizenId).value);
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
    // L4 a debtor posing as a tourist
    // -----------------------------

    [Test]
    public void DebtorPosingAsTourist_PrintsThePosedStatusAndItsClass_WithNoDraw()
    {
        CitizenAccount account = Eligible();
        var rng = new ScriptedRandom();
        LiePlan plan = Plan(LieKind.DebtorPosingAsTourist, rng, account, posed: CitizenStatus.Premium);
        Assert.IsTrue(rng.Done, "one variant, and the posed status draws nothing");
        Assert.AreEqual(LieOutcome.Forger, plan.Outcome);
        CollectionAssert.AreEqual(
            new[] { (0, ClueCategory.AccountStatus, "Premium"), (1, ClueCategory.TransponderClass, "Premium") },
            plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray());
    }

    [Test]
    public void DebtorPosingAsTourist_WithoutAPosedStatus_OrPosingAsTheirOwnClass_CannotShow()
    {
        var rng = new ScriptedRandom();
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.DebtorPosingAsTourist, rng, Eligible()).Outcome, "no posed status");
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.DebtorPosingAsTourist, rng, Eligible(), posed: CitizenStatus.Standard).Outcome, "a Standard visa rides an Economy unit like the account's, and the rich set carries no waiver for the poor variant");
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.DebtorPosingAsTourist, rng, Eligible(), posed: CitizenStatus.Eligible).Outcome, "posing as oneself");
        Assert.IsTrue(rng.Done);
    }

    // -----------------------------
    // The poor set (phase 8's waiver and proofs of means)
    // -----------------------------

    private const string WaiverNo = "SW-2048";

    /// <summary>The agency's proofs of means: a credit line (TC-415), savings (TC-416) and a policy ("TI", TC-417).</summary>
    private static List<ProofOfMeans> Proofs() => new List<ProofOfMeans>
    {
        new ProofOfMeans { form = RecordLies.CreditAgreement, category = ClueCategory.Credit, amountMin = 4_000, amountMax = 12_000 },
        new ProofOfMeans { form = RecordLies.ProofOfFunds, category = ClueCategory.Funds, amountMin = 3_000, amountMax = 15_000 },
        new ProofOfMeans { form = RecordLies.Insurance, category = ClueCategory.PolicyNo, prefix = "TI" }
    };

    private static readonly ClueCategory[] ProofKinds = { ClueCategory.Credit, ClueCategory.Funds, ClueCategory.PolicyNo };

    private static string ProofFormOf(ClueCategory proof) =>
        proof == ClueCategory.Credit ? RecordLies.CreditAgreement : proof == ClueCategory.Funds ? RecordLies.ProofOfFunds : RecordLies.Insurance;

    /// <summary>Gives <paramref name="a"/> a proof of <paramref name="proof"/> (a credit line of 9,400 cr, savings of 6,200 cr or policy TI-5519); <paramref name="forged"/> when it is not on file (L4's).</summary>
    private static CitizenAccount WithProof(CitizenAccount a, ClueCategory proof, bool forged = false)
    {
        a.ProofCategory = proof;
        a.ProofForm = ProofFormOf(proof);
        a.ProofAmount = proof == ClueCategory.Credit ? 9_400 : proof == ClueCategory.Funds ? 6_200 : 0;
        a.ProofValue = proof == ClueCategory.PolicyNo ? "TI-5519" : AccountMaker.Credits(a.ProofAmount);
        a.ProofForged = forged;
        return a;
    }

    /// <summary>A poor tourist's account: Standard, the waiver SW-2048 on file, and one proof of means.</summary>
    private static CitizenAccount Poor(ClueCategory proof = ClueCategory.Credit)
    {
        CitizenAccount a = Standard();
        a.WaiverNo = WaiverNo;
        return WithProof(a, proof);
    }

    /// <summary>A debtor posing as a poor tourist (L4): Eligible, 212,000 cr in debt, the waiver on file, and a proof of means they carry but do not hold.</summary>
    private static CitizenAccount DebtorAsPoor(ClueCategory proof = ClueCategory.Credit)
    {
        CitizenAccount a = Eligible();
        a.WaiverNo = WaiverNo;
        return WithProof(a, proof, forged: true);
    }

    /// <summary>The TC-310 Stranding Waiver as printed: Signatory, Citizen ID, Transponder, Debt Passed to Kin, Waiver No., Signature.</summary>
    private static List<DocumentField> WaiverFields(CitizenAccount a) => new List<DocumentField>
    {
        F(ClueCategory.Name, Traveller),
        F(ClueCategory.CitizenId, a.CitizenId),
        F(ClueCategory.TransponderId, a.Transponder),
        F(ClueCategory.Debt, AccountMaker.Credits(a.Debt)),
        F(ClueCategory.WaiverNo, a.WaiverNo),
        F(ClueCategory.Signature, Traveller)
    };

    /// <summary>The proof of means as its form prints it: TC-415 (Borrower, Citizen ID, Destination, Account Class, Credit Line, Valid Until), TC-416 (Account Holder, Citizen ID, Account Class, Funds Held, Valid Until) or TC-417 (Insured, Citizen ID, Destination, Policy No., Valid Until).</summary>
    private static List<DocumentField> ProofFields(CitizenAccount a)
    {
        var fields = new List<DocumentField> { F(ClueCategory.Name, Traveller), F(ClueCategory.CitizenId, a.CitizenId) };
        if (a.ProofCategory != ClueCategory.Funds)
            fields.Add(F(ClueCategory.Destination, "New Kingdom Egypt (Ancient)"));
        if (a.ProofCategory != ClueCategory.PolicyNo)
            fields.Add(F(ClueCategory.AccountStatus, a.Status.ToString()));
        fields.Add(F(a.ProofCategory, a.ProofValue));
        fields.Add(F(ClueCategory.Expiry, "30 Mar 2150"));
        return fields;
    }

    /// <summary>The poor set, honest, for <paramref name="a"/>: TC-101, TC-230, TC-310, then the proof the traveller carries (none without one).</summary>
    private static List<RecordForm> PoorForms(CitizenAccount a)
    {
        List<RecordForm> forms = Forms(a);
        forms.Add(new RecordForm(RecordLies.Waiver, WaiverFields(a)));
        if (a.ProofForm != null)
            forms.Add(new RecordForm(a.ProofForm, ProofFields(a)));
        return forms;
    }

    /// <summary>The labour set with its waiver (phase 8): TC-520, TC-230, TC-310.</summary>
    private static List<RecordForm> LabourFormsWithWaiver(CitizenAccount a)
    {
        List<RecordForm> forms = LabourForms(a);
        forms.Add(new RecordForm(RecordLies.Waiver, WaiverFields(a)));
        return forms;
    }

    private static (int, ClueCategory, string)[] Tells(LiePlan plan) => plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray();

    [Test]
    public void DebtorPosingAsTourist_AsPoor_PrintsTheStandardClass_ASliverOfTheDebt_AndTheCarriedProofsClass()
    {
        CitizenAccount account = DebtorAsPoor();
        List<RecordForm> forms = PoorForms(account);
        var rng = new ScriptedRandom(V(0f));
        LiePlan plan = Plan(LieKind.DebtorPosingAsTourist, rng, account, forms, posed: CitizenStatus.Standard);
        Assert.IsTrue(rng.Done, "the rich variant cannot show (a Standard visa rides the account's Economy class), so no variant draw; the posed status draws nothing, the sliver one Value");
        CollectionAssert.AreEqual(
            new[] { (0, ClueCategory.AccountStatus, "Standard"), (2, ClueCategory.Debt, "10,600 cr"), (3, ClueCategory.AccountStatus, "Standard") },
            Tells(plan), "212,000 x 0.05 on the waiver; the credit agreement's class made once with the visa's");

        plan.ApplyTo(Docs(forms));
        CollectionAssert.IsEmpty(PaperChecks.Contradictions(Docs(forms)), "the forger's papers agree with each other: the record proves them");
        CitizenRecord record = AccountRecords.Record(Traveller, Cover, "New Kingdom Egypt (Ancient)", account, key => key);
        Assert.IsFalse(record.Groups.SelectMany(g => g.Rows).Any(r => r.IsEvidence && AccountMaker.IsProofCategory(r.Category)), "the carried proof is not on file");

        CitizenAccount policy = DebtorAsPoor(ClueCategory.PolicyNo);
        LiePlan insured = Plan(LieKind.DebtorPosingAsTourist, new ScriptedRandom(V(1f)), policy, PoorForms(policy), posed: CitizenStatus.Standard);
        CollectionAssert.AreEqual(new[] { (0, ClueCategory.AccountStatus, "Standard"), (2, ClueCategory.Debt, "42,400 cr") }, Tells(insured),
                                  "an insurance certificate prints no class: the optional field is skipped, with no draw");
    }

    // -----------------------------
    // L3 a fake waiver
    // -----------------------------

    [Test]
    public void FakeWaiver_TheNumber_DrawsTheVariantThenAFreshNumber_TheAccountNeverRegistered()
    {
        CitizenAccount account = Poor();
        List<RecordForm> forms = PoorForms(account);
        HashSet<string> taken = Taken(account);
        taken.Add(WaiverNo);
        taken.Add("SW-0001");
        var rng = new ScriptedRandom(R(0), R(1), R(2));
        LiePlan plan = Plan(LieKind.FakeWaiver, rng, account, forms, taken: taken);
        Assert.IsTrue(rng.Done, "the variant between two, then the number (SW-0001 is taken today, so it is redrawn)");
        CollectionAssert.AreEqual(new[] { (2, ClueCategory.WaiverNo, "SW-0002") }, Tells(plan));
        CollectionAssert.Contains(taken, "SW-0002");

        plan.ApplyTo(Docs(forms));
        Assert.AreNotEqual(account.WaiverNo, forms[2].Fields.Single(f => f.category == ClueCategory.WaiverNo).value, "a number the account never registered");
        Assert.IsTrue(Directives.IsSigned(forms[2].Fields), "signed: a forgery (the lie), not a paper-set fault");
        CollectionAssert.IsEmpty(PaperChecks.Contradictions(Docs(forms)), "no other paper prints the waiver's number: the record proves it");
    }

    [Test]
    public void FakeWaiver_TheTransponder_IsAnotherUnitOfTheAccountsClass_AndContradictsTheManifest()
    {
        CitizenAccount account = Poor();
        List<RecordForm> forms = PoorForms(account);
        var rng = new ScriptedRandom(R(1), V(0f), R(3));
        LiePlan plan = Plan(LieKind.FakeWaiver, rng, account, forms);
        Assert.IsTrue(rng.Done, "the variant, then the model among the account's other Economy models (weights 2, 1), then its serial");
        CollectionAssert.AreEqual(new[] { (2, ClueCategory.TransponderId, "Skip Lite · SL-00003") }, Tells(plan));

        plan.ApplyTo(Docs(forms));
        List<PaperContradiction> cross = PaperChecks.Contradictions(Docs(forms));
        Assert.AreEqual(1, cross.Count, "the waiver's transponder disagrees with the manifest's (paper vs paper)");
        Assert.AreEqual(ClueCategory.TransponderId, cross[0].Category);
        Assert.AreEqual(account.WaiverNo, forms[2].Fields.Single(f => f.category == ClueCategory.WaiverNo).value, "its number is the registered one");
    }

    [Test]
    public void FakeWaiver_WithoutAWaiverCarried_CannotShow_ALabourersCanBeFaked()
    {
        var none = new ScriptedRandom();
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.FakeWaiver, none, Standard()).Outcome, "the rich set carries no waiver");
        CitizenAccount unregistered = Poor();
        unregistered.WaiverNo = null;
        List<RecordForm> forms = PoorForms(unregistered);
        Assert.AreEqual(LieOutcome.NoPossibleLie, RecordLies.Plan(LieKind.FakeWaiver, forms, unregistered, NoTransponders(unregistered), none).Outcome,
                        "no waiver on file and no model to name: nothing to fake");
        Assert.IsTrue(none.Done);

        CitizenAccount labourer = Labourer();
        labourer.WaiverNo = WaiverNo;
        var rng = new ScriptedRandom(R(0), R(7));
        LiePlan plan = Plan(LieKind.FakeWaiver, rng, labourer, LabourFormsWithWaiver(labourer));
        Assert.IsTrue(rng.Done);
        CollectionAssert.AreEqual(new[] { (2, ClueCategory.WaiverNo, "SW-0007") }, Tells(plan));
    }

    private static RecordLieContext NoTransponders(CitizenAccount account)
    {
        RecordLieContext context = Context(account);
        context.Transponders = new TransponderModel[0];
        return context;
    }

    // -----------------------------
    // L10 a forged proof of means
    // -----------------------------

    [TestCase(ClueCategory.Credit, 0f, "28,200 cr")]
    [TestCase(ClueCategory.Credit, 1f, "94,000 cr")]
    [TestCase(ClueCategory.Funds, 0.5f, "40,300 cr")]
    public void ForgedProof_AnAmount_IsThreeToTenTimesTheOneOnFile_WithNoVariantDraw(ClueCategory proof, float roll, string forged)
    {
        CitizenAccount account = Poor(proof);
        List<RecordForm> forms = PoorForms(account);
        var rng = new ScriptedRandom(V(roll));
        LiePlan plan = Plan(LieKind.ForgedProof, rng, account, forms);
        Assert.IsTrue(rng.Done, "the traveller carries one proof, so one variant shows: the amount's one draw");
        CollectionAssert.AreEqual(new[] { (3, proof, forged) }, Tells(plan));
    }

    [Test]
    public void ForgedProof_APolicy_IsAFreshNumberWithThePolicysPrefix()
    {
        CitizenAccount account = Poor(ClueCategory.PolicyNo);
        HashSet<string> taken = Taken(account);
        taken.Add("TI-5519");
        var rng = new ScriptedRandom(R(5519), R(88));
        LiePlan plan = Plan(LieKind.ForgedProof, rng, account, PoorForms(account), taken: taken);
        Assert.IsTrue(rng.Done, "the policy on file is taken today, so the number is redrawn");
        CollectionAssert.AreEqual(new[] { (3, ClueCategory.PolicyNo, "TI-0088") }, Tells(plan));
    }

    [Test]
    public void ForgedProof_WithoutAProofOnFile_CannotShow()
    {
        var rng = new ScriptedRandom();
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.ForgedProof, rng, Standard()).Outcome, "no proof carried");
        CitizenAccount debtor = DebtorAsPoor();
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.ForgedProof, rng, debtor, PoorForms(debtor)).Outcome, "a proof not on file (L4's) has nothing to be forged against");
        Assert.IsTrue(rng.Done);
    }

    // -----------------------------
    // L5 a forged contract
    // -----------------------------

    [Test]
    public void ForgedContract_DrawsTheVariantThenTheValue_EachOnItsOwnBox()
    {
        CitizenAccount account = Labourer();
        List<RecordForm> forms = LabourForms(account);

        var wage = new ScriptedRandom(R(0), V(0f));
        LiePlan plan = Plan(LieKind.ForgedContract, wage, account, forms);
        Assert.IsTrue(wage.Done, "the variant among four, then the wage's one draw");
        CollectionAssert.AreEqual(new[] { (0, ClueCategory.Wage, "630 cr") }, plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray(), "420 x 1.5");

        var term = new ScriptedRandom(R(1), V(1f));
        plan = Plan(LieKind.ForgedContract, term, account, forms);
        Assert.IsTrue(term.Done);
        CollectionAssert.AreEqual(new[] { (0, ClueCategory.Term, "120 days") }, plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray(), "180 x 0.6 = 108, rounded to whole months");

        var employer = new ScriptedRandom(R(2), R(0));
        plan = Plan(LieKind.ForgedContract, employer, account, forms);
        Assert.IsTrue(employer.Done);
        CollectionAssert.AreEqual(new[] { (0, ClueCategory.Employer, "Ruhr Colliery Partners") }, plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray(), "another employer of the era");

        var worksite = new ScriptedRandom(R(3), R(1));
        plan = Plan(LieKind.ForgedContract, worksite, account, forms);
        Assert.IsTrue(worksite.Done);
        CollectionAssert.AreEqual(new[] { (0, ClueCategory.Destination, "Bismarck Berlin (Industrial)") }, plan.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray(), "another open place");

        plan.ApplyTo(Docs(forms));
        Assert.AreEqual("Bismarck Berlin (Industrial)", forms[0].Fields.Single(f => f.category == ClueCategory.Destination).value);
        Assert.IsTrue(forms[0].Fields.Single(f => f.category == ClueCategory.Destination).isAnachronism);
        CollectionAssert.IsEmpty(PaperChecks.Contradictions(Docs(forms)), "the manifest prints no worksite: the record proves it");
    }

    [Test]
    public void ForgedContract_VariantsThatCannotShow_AreNeverDrawn()
    {
        CitizenAccount account = Labourer();
        List<RecordForm> forms = LabourForms(account);
        RecordLieContext alone = Context(account);
        alone.Employers = new[] { account.Employer };
        alone.OpenPlaces = new string[0];
        var rng = new ScriptedRandom(R(1), V(0.5f));
        LiePlan plan = RecordLies.Plan(LieKind.ForgedContract, forms, account, alone, rng);
        Assert.IsTrue(rng.Done, "the variant among the wage and the term only");
        Assert.AreEqual(ClueCategory.Term, plan.RecordTells.Single().Category);

        var none = new ScriptedRandom();
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.ForgedContract, none, Eligible(), LabourForms(Eligible())).Outcome, "no contract registered: nothing to forge against");
        Assert.AreEqual(LieOutcome.NoPossibleLie, Plan(LieKind.ForgedContract, none, account, Forms(account)).Outcome, "no contract printed");
        Assert.IsTrue(none.Done);
    }

    // -----------------------------
    // Every forged field differs and is provable
    // -----------------------------

    [TestCase(LieKind.PoorPosingAsRich, false)]
    [TestCase(LieKind.DoctoredIdentity, false)]
    [TestCase(LieKind.DoctoredIdentity, true)]
    [TestCase(LieKind.DebtorPosingAsTourist, false)]
    [TestCase(LieKind.DebtorPosingAsTourist, true)]
    [TestCase(LieKind.ForgedContract, false)]
    [TestCase(LieKind.FakeWaiver, true)]
    [TestCase(LieKind.FakeWaiver, false)]
    [TestCase(LieKind.ForgedProof, true)]
    public void EveryForgedField_DiffersFromTheAccount_IsProvable_AndProvesAgainstTheRecord(LieKind kind, bool poor)
    {
        for (int seed = 0; seed < 200; seed++)
        {
            ClueCategory means = ProofKinds[seed % ProofKinds.Length];
            CitizenAccount account = kind == LieKind.ForgedContract ? Labourer()
                : kind == LieKind.DebtorPosingAsTourist ? (poor ? DebtorAsPoor(means) : Eligible())
                : kind == LieKind.FakeWaiver && !poor ? Labourer()
                : poor ? Poor(means) : Standard();
            if (kind == LieKind.FakeWaiver && !poor)
                account.WaiverNo = WaiverNo;
            List<RecordForm> forms = kind == LieKind.ForgedContract ? LabourForms(account)
                : kind == LieKind.FakeWaiver && !poor ? LabourFormsWithWaiver(account)
                : poor ? PoorForms(account) : Forms(account);
            List<DocumentField> honest = forms.SelectMany(f => f.Fields).Select(f => new DocumentField { category = f.category, value = f.value }).ToList();
            CitizenStatus? posed = kind != LieKind.DebtorPosingAsTourist ? (CitizenStatus?)null : poor ? CitizenStatus.Standard : CitizenStatus.Premium;
            LiePlan plan = Plan(kind, new SeededRandom(seed), account, forms, posed: posed);
            Assert.AreEqual(LieOutcome.Forger, plan.Outcome, $"seed {seed}");
            Assert.IsNotEmpty(plan.RecordTells, $"seed {seed}");

            CitizenRecord record = AccountRecords.Record(Traveller, Cover, kind == LieKind.ForgedContract ? "Victorian Britain (Industrial)" : "New Kingdom Egypt (Ancient)", account, key => key);
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
        foreach (LieKind kind in new[] { LieKind.PoorPosingAsRich, LieKind.DoctoredIdentity, LieKind.ForgedContract, LieKind.FakeWaiver, LieKind.ForgedProof })
        {
            bool poor = kind == LieKind.FakeWaiver || kind == LieKind.ForgedProof;
            CitizenAccount a = kind == LieKind.ForgedContract ? Labourer() : poor ? Poor() : Standard(), b = kind == LieKind.ForgedContract ? Labourer() : poor ? Poor() : Standard();
            LiePlan x = Plan(kind, new SeededRandom(11), a, kind == LieKind.ForgedContract ? LabourForms(a) : poor ? PoorForms(a) : null, taken: Taken(a));
            LiePlan y = Plan(kind, new SeededRandom(11), b, kind == LieKind.ForgedContract ? LabourForms(b) : poor ? PoorForms(b) : null, taken: Taken(b));
            CollectionAssert.AreEqual(x.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray(), y.RecordTells.Select(t => (t.Document, t.Category, t.Value)).ToArray(), kind.ToString());
        }
    }

    [Test]
    public void NoStream_NoAccount_OrAPlaceLie_IsHonest_WithNoDraw()
    {
        var rng = new ScriptedRandom();
        Assert.AreEqual(LieOutcome.Honest, Plan(LieKind.DoctoredIdentity, null).Outcome);
        Assert.AreEqual(LieOutcome.Honest, RecordLies.Plan(LieKind.DoctoredIdentity, Forms(Standard()), null, Context(Standard()), rng).Outcome);
        Assert.AreEqual(LieOutcome.Honest, RecordLies.Plan(LieKind.DoctoredIdentity, Forms(Standard()), Standard(), null, rng).Outcome, "no context");
        LiePlan place = Plan(LieKind.FalseOrigin, rng);
        Assert.AreEqual(LieOutcome.Honest, place.Outcome);
        Assert.AreEqual(LieKind.FalseOrigin, place.Kind);
        Assert.IsTrue(rng.Done);
    }

    // -----------------------------
    // The makers
    // -----------------------------

    [Test]
    public void FalseMeans_IsThreeToTenTimes_RoundedToAHundred_AlwaysAbove()
    {
        Assert.AreEqual(28_200, RecordLies.FalseMeans(9_400, new ScriptedRandom(V(0f))));
        Assert.AreEqual(94_000, RecordLies.FalseMeans(9_400, new ScriptedRandom(V(1f))));
        Assert.AreEqual(100, RecordLies.FalseMeans(0, new ScriptedRandom(V(0.5f))), "nothing on file still forges an amount above it");
        for (int seed = 0; seed < 200; seed++)
        {
            int amount = 3_000 + 60 * seed;
            int forged = RecordLies.FalseMeans(amount, new SeededRandom(seed));
            Assert.Greater(forged, amount, $"seed {seed}");
            Assert.AreEqual(0, forged % 100, $"seed {seed}");
        }
    }

    [Test]
    public void DebtSliver_IsATwentiethToAFifth_RoundedToAHundred_AtLeastAHundred_NeverTheDebt()
    {
        Assert.AreEqual(10_600, RecordLies.DebtSliver(212_000, new ScriptedRandom(V(0f))));
        Assert.AreEqual(42_400, RecordLies.DebtSliver(212_000, new ScriptedRandom(V(1f))));
        Assert.AreEqual(100, RecordLies.DebtSliver(400, new ScriptedRandom(V(0f))), "at least 100 cr");
        Assert.AreEqual(0, RecordLies.DebtSliver(100, new ScriptedRandom(V(0f))), "100 cr less when the sliver lands on the debt");
        for (int seed = 0; seed < 200; seed++)
        {
            int debt = 40_000 + 1_400 * seed;
            int sliver = RecordLies.DebtSliver(debt, new SeededRandom(seed));
            Assert.That(sliver, Is.InRange(debt / 20 - 50, debt / 5 + 50), $"seed {seed}");
            Assert.AreEqual(0, sliver % 100, $"seed {seed}");
        }
    }

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
    public void FalseWage_IsOneAndAHalfToThreeTimes_RoundedToTen_NeverEqual()
    {
        Assert.AreEqual(630, RecordLies.FalseWage(420, new ScriptedRandom(V(0f))));
        Assert.AreEqual(1260, RecordLies.FalseWage(420, new ScriptedRandom(V(1f))));
        Assert.AreEqual(950, RecordLies.FalseWage(420, new ScriptedRandom(V(0.5f))), "420 x 2.25 = 945, rounded to 950");
        Assert.AreEqual(20, RecordLies.FalseWage(10, new ScriptedRandom(V(0f))), "10 x 1.5 = 15 rounds to 20");
        Assert.AreEqual(10, RecordLies.FalseWage(0, new ScriptedRandom(V(0f))), "a wage of 0 forges one step up rather than itself");
        for (int seed = 0; seed < 200; seed++)
        {
            int forged = RecordLies.FalseWage(180, new SeededRandom(seed));
            Assert.AreNotEqual(180, forged, $"seed {seed}");
            Assert.AreEqual(0, forged % 10, $"seed {seed}");
            Assert.That(forged, Is.InRange(270, 540), $"seed {seed}");
        }
    }

    [Test]
    public void FalseTerm_IsAQuarterToSixTenths_InWholeMonths_AtLeastOne_NeverEqual()
    {
        Assert.AreEqual(60, RecordLies.FalseTerm(180, new ScriptedRandom(V(0f))), "180 x 0.25 = 45, rounded to 60");
        Assert.AreEqual(120, RecordLies.FalseTerm(180, new ScriptedRandom(V(1f))), "180 x 0.6 = 108, rounded to 120");
        Assert.AreEqual(30, RecordLies.FalseTerm(90, new ScriptedRandom(V(0f))), "at least one month");
        Assert.AreEqual(60, RecordLies.FalseTerm(30, new ScriptedRandom(V(1f))), "30 x 0.6 rounds to 30, the registered term: one month up when a month down would be none");
        for (int seed = 0; seed < 200; seed++)
        {
            int forged = RecordLies.FalseTerm(720, new SeededRandom(seed));
            Assert.AreNotEqual(720, forged, $"seed {seed}");
            Assert.AreEqual(0, forged % AccountMaker.MonthDays, $"seed {seed}");
            Assert.That(forged, Is.InRange(180, 450), $"seed {seed}");
        }
    }

    [Test]
    public void OtherOf_IsAnotherEntry_AfterOneDraw_OrNullWithoutOne()
    {
        var rng = new ScriptedRandom(R(0));
        Assert.AreEqual("Ruhr Colliery Partners", RecordLies.OtherOf(Employers, "Tyburn Mills Consortium", rng));
        Assert.IsTrue(rng.Done);
        var none = new ScriptedRandom();
        Assert.IsNull(RecordLies.OtherOf(new[] { "Tyburn Mills Consortium" }, "tyburn mills consortium", none), "the same value under Values.Match");
        Assert.IsNull(RecordLies.OtherOf(null, "x", none));
        Assert.IsTrue(none.Done);
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
        var taken = new HashSet<string> { "ABB-001" };
        var rng = new ScriptedRandom(R(1), R(1), R(1), R(2), R(2), R(2));
        Assert.AreEqual("ACC-002", RecordLies.FreshCitizenId(taken, rng));
        Assert.IsTrue(rng.Done, "the taken number is redrawn");
        CollectionAssert.Contains(taken, "ACC-002");
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

    /// <summary>While a recall stands (days 7-15 §6), the forged Economy unit is drawn from the models left after it (Directives.Unrecalled): nobody holds a recalled unit except through the recall's maker (one fault per traveller).</summary>
    [Test]
    public void FalseTransponder_SkipsARecalledModel()
    {
        List<TransponderModel> left = Directives.Unrecalled(Transponders(), new[] { "driftbox3" });
        for (int seed = 0; seed < 200; seed++)
        {
            string forged = RecordLies.FalseTransponder(TransponderClass.Economy, "Hopper Mk II · HP-40718", left, new HashSet<string>(), new SeededRandom(seed));
            StringAssert.DoesNotStartWith("Driftbox 3", forged, $"seed {seed}");
        }
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
