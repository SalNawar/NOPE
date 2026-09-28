using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The lie roll (Lies.Roll: the roll, then the kind) and the place lie's
/// planning (Lies.Plan, after the roll). Today, in order: the claim New Kingdom Egypt; a twin (Greece)
/// whose values equal Egypt's under the scanner comparison and who has no
/// birth years; Babylonia (Iraq), different in every book category and born
/// 1460..1440 BCE (around the cover year 1450 BCE); Republican Rome (Italy),
/// different only in Currency and born exactly in the cover year. Iraq's
/// values and Italy's Currency appear nowhere else. The papers follow the real
/// templates (passport: Name, BirthDate, Currency, Language; permit:
/// Technology, Currency), so Iraq's eligible tells are BirthDate, Currency,
/// Language and Technology, in that order. The answer tests add capitals
/// (Egypt "Thebes", the twin " thebes ", Iraq "Babylon", Italy "Rome") and a
/// Geography book, and ask about Currency and Geography.
/// </summary>
public class LiesTests
{
    /// <summary>The traveller at the desk, whose Citizen Record proves a birth-date tell.</summary>
    private const string Traveller = "Ahmose";

    private const string Cover = "3 Jun 1450 BCE";
    private const int CoverYear = -1450;

    private static readonly HashSet<ClueCategory> Books = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology };

    /// <summary>No question may carry an Answer tell.</summary>
    private static readonly ClueCategory[] None = new ClueCategory[0];

    /// <summary>Piece 2's channel set: tells only on the papers.</summary>
    private static readonly TellChannel[] PapersOnly = { TellChannel.Papers };

    /// <summary>Tells only in answers.</summary>
    private static readonly TellChannel[] AnswerOnly = { TellChannel.Answer };

    /// <summary>Days 2-3: tells on the papers or in answers.</summary>
    private static readonly TellChannel[] Both = { TellChannel.Papers, TellChannel.Answer };

    private static readonly HomeCandidate Egypt = new HomeCandidate("egypt", "ancient", -1520, -1452);
    private static readonly HomeCandidate Twin = new HomeCandidate("greece", "ancient", 0, 0);
    private static readonly HomeCandidate Iraq = new HomeCandidate("iraq", "ancient", -1460, -1440);
    private static readonly HomeCandidate Italy = new HomeCandidate("italy", "ancient", -1450, -1450);
    private static readonly HomeCandidate[] Today4 = { Egypt, Twin, Iraq, Italy };

    private static FactTable Facts()
    {
        var t = new FactTable();
        Add(t, Egypt, "New Kingdom Egypt (Ancient)", "Deben", "Middle Egyptian", "Papyrus");
        Add(t, Twin, "Periclean Athens (Ancient)", " deben ", "MIDDLE EGYPTIAN", "papyrus");
        Add(t, Iraq, "Babylonia (Ancient)", "Silver shekel", "Old Babylonian", "Cylinder seal");
        Add(t, Italy, "Republican Rome (Ancient)", "Denarius", "Middle Egyptian", "Papyrus");
        return t;
    }

    private static void Add(FactTable t, HomeCandidate p, string label, string currency, string language, string technology)
    {
        t.Add(p.NationId, p.EraId, label, ClueCategory.Currency, currency);
        t.Add(p.NationId, p.EraId, label, ClueCategory.Language, language);
        t.Add(p.NationId, p.EraId, label, ClueCategory.Technology, technology);
    }

    private static DocumentField Field(ClueCategory category, string label, string value, int page) =>
        new DocumentField { category = category, label = label, value = value, page = page };

    /// <summary>Egypt's honest papers: passport, then (optionally) the permit.</summary>
    private static List<DocumentField> Papers(bool withPermit = true)
    {
        var papers = new List<DocumentField>
        {
            Field(ClueCategory.Name, "Full Name", "Nebamun", 0),
            Field(ClueCategory.BirthDate, "Date of Birth", Cover, 0),
            Field(ClueCategory.Currency, "Coin of Issue", "Deben", 0),
            Field(ClueCategory.Language, "Native Tongue", "Middle Egyptian", 0)
        };

        if (withPermit)
        {
            papers.Add(Field(ClueCategory.Technology, "Declared Device", "Papyrus", 0));
            papers.Add(Field(ClueCategory.Currency, "Bond Currency", "Deben", 1));
        }

        return papers;
    }

    private static LiePlan Plan(IRandomSource rng, int tellCount = 1, IReadOnlyList<HomeCandidate> todays = null,
                                List<DocumentField> papers = null, FactTable facts = null) =>
        Lies.Plan(tellCount, "egypt", "ancient", Cover, todays ?? Today4, papers ?? Papers(), None, PapersOnly, facts ?? Facts(), Books, rng);

    /// <summary>The papers as ApplyTo takes them: one paper per list.</summary>
    private static IReadOnlyList<IReadOnlyList<DocumentField>> Docs(params IReadOnlyList<DocumentField>[] papers) => papers;

    /// <summary>The lies enabled for a displaced traveller today: the false origin alone.</summary>
    private static readonly LieKind[] OneLie = { LieKind.FalseOrigin };

    private static ScriptedRandom Script(params ScriptStep[] steps) => new ScriptedRandom(steps);
    private static ScriptStep V(float roll) => ScriptStep.Value(roll);
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    // -----------------------------
    // Smuggling: the present as the only candidate, the category filter
    // -----------------------------

    /// <summary>The present (traveller types H1) as a home candidate: the neutral present's ids and its citizens' birth years.</summary>
    private static readonly HomeCandidate Present = new HomeCandidate("neutral", "future", 2080, 2132);

    /// <summary>The present's label, as its row in every book prints it.</summary>
    private const string PresentLabel = "Temporal Customs Zone (Future)";

    /// <summary>Facts() with the present's row last (Present.AddRow): Credits, Agency Standard English, Wrist comm.</summary>
    private static FactTable FactsWithPresent()
    {
        FactTable t = Facts();
        Add(t, Present, PresentLabel, "Credits", "Agency Standard English", "Wrist comm");
        return t;
    }

    /// <summary>A smuggler's plan: Egypt's honest claim, the present as the only candidate, Currency and Technology only.</summary>
    private static LiePlan Smuggle(IRandomSource rng, IReadOnlyList<TellChannel> channels, IReadOnlyList<ClueCategory> asked, int tellCount = 1,
                                   List<DocumentField> papers = null, FactTable facts = null, IReadOnlyList<HomeCandidate> candidates = null) =>
        Lies.Plan(tellCount, "egypt", "ancient", Cover, candidates ?? new[] { Present }, papers ?? Papers(), asked, channels,
                  facts ?? FactsWithPresent(), Books, rng, Lies.SmuggledCategories, LieKind.Smuggling);

    [Test]
    public void Smuggling_ThePresentIsTheOnlyCandidate_AndOnlyTheSmuggledCategoriesAreOptions()
    {
        // The papers print Name, BirthDate, Currency, Language, Technology, Currency: the filter leaves P:Currency, P:Technology.
        List<DocumentField> papers = Papers();
        LiePlan plan = Smuggle(Script(R(0), R(0), R(0)), PapersOnly, None, tellCount: 9, papers: papers);
        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(LieKind.Smuggling, plan.Kind);
        Assert.AreEqual(0, plan.HomeIndex, "the present, index 0 of the candidate list");
        CollectionAssert.AreEquivalent(new[] { ClueCategory.Currency, ClueCategory.Technology }, plan.Tells);
        Assert.AreEqual("Credits", plan.TellValue(ClueCategory.Currency));
        Assert.AreEqual("Wrist comm", plan.TellValue(ClueCategory.Technology));
        Assert.IsNull(plan.TellValue(ClueCategory.Language), "the tongue is never smuggled");
        Assert.IsNull(plan.TellValue(ClueCategory.BirthDate), "nor the birth year");

        plan.ApplyTo(Docs(papers));
        foreach (DocumentField f in papers)
            Assert.AreEqual(f.category == ClueCategory.Currency || f.category == ClueCategory.Technology, f.isAnachronism, f.label);
        Assert.AreEqual("Credits", papers[2].value, "Coin of Issue");
        Assert.AreEqual("Credits", papers[5].value, "Bond Currency (every field of the category)");
        Assert.AreEqual("Wrist comm", papers[4].value, "Declared Device");
    }

    [Test]
    public void Smuggling_GoldenOrder_TheHomeThenOneTell_TheFilterKeepsTheOptionsOrder()
    {
        // Options after the filter, in order: P:Currency, P:Technology, then the asked A:Technology, A:Currency; R(2) picks the spoken device.
        LiePlan plan = Smuggle(Script(R(0), R(2)), Both, new[] { ClueCategory.Technology, ClueCategory.Currency });
        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        CollectionAssert.AreEqual(new[] { ClueCategory.Technology }, plan.Tells);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.Technology));
        Assert.AreEqual("Wrist comm", plan.TellValue(ClueCategory.Technology));

        plan = Smuggle(Script(R(0), R(3)), Both, new[] { ClueCategory.Technology, ClueCategory.Currency });
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.Currency), "the fourth option is the spoken currency");
        Assert.AreEqual("Credits", plan.TellValue(ClueCategory.Currency));
    }

    [Test]
    public void Smuggling_NeverLeaksInDress_AndNeverFromAnotherPlace()
    {
        var leakable = new HomeCandidate("neutral", "future", 2080, 2132, appearanceLeakable: true);
        FactTable facts = FactsWithPresent();
        facts.Add("neutral", "future", PresentLabel, ClueCategory.Culture, "tech jacket");
        facts.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Culture, "pleated kilt");
        var books = new HashSet<ClueCategory>(Books) { ClueCategory.Culture };
        LiePlan plan = Lies.Plan(9, "egypt", "ancient", Cover, new[] { leakable }, Papers(), None, new[] { TellChannel.Papers, TellChannel.Appearance },
                                 facts, books, Script(R(0), R(0), R(0)), Lies.SmuggledCategories, LieKind.Smuggling);
        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        CollectionAssert.AreEquivalent(new[] { ClueCategory.Currency, ClueCategory.Technology }, plan.Tells, "no dress option: the filter has no Culture");

        // Given every candidate of the day, the filter still never picks a place whose Currency and Technology equal the claim's.
        plan = Smuggle(Script(R(0), R(0)), PapersOnly, None, candidates: new[] { Twin, Present });
        Assert.AreEqual(1, plan.HomeIndex, "the twin has no differing smuggled category; the present is the one candidate");
    }

    [Test]
    public void Smuggling_WithoutTheChannels_OrThePresentInTheTable_IsNoPossibleLie_CarryingTheKind()
    {
        LiePlan plan = Smuggle(Script(), AnswerOnly, None);
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome, "nothing asked, papers closed");
        Assert.AreEqual(LieKind.Smuggling, plan.Kind);

        plan = Smuggle(Script(), PapersOnly, None, facts: Facts());
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome, "the present has no row today");

        plan = Lies.Plan(1, "egypt", "ancient", Cover, new[] { Present }, Papers(), None, PapersOnly, FactsWithPresent(), Books, null, Lies.SmuggledCategories, LieKind.Smuggling);
        Assert.AreEqual(LieOutcome.Honest, plan.Outcome, "no stream: honest, no draw");
        Assert.AreEqual(LieKind.Smuggling, plan.Kind);
    }

    [Test]
    public void ASmuggledTell_ProvesAgainstTheClaimsRow_AndNamesThePresent()
    {
        FactTable facts = FactsWithPresent();
        List<DocumentField> papers = Papers();
        Smuggle(Script(R(0), R(0), R(0)), PapersOnly, None, tellCount: 9, papers: papers, facts: facts).ApplyTo(Docs(papers));

        foreach (DocumentField f in papers.Where(f => f.isAnachronism))
        {
            CompareEvidence doc = CompareEvidence.FromDocumentField(f, 0);
            foreach (FactRow row in facts.Rows(f.category))
            {
                Discrepancy d = DiscrepancyLog.Prove(doc, row.ToEvidence(), "egypt", "ancient", Traveller);
                string where = $"{f.label} vs {row.OriginLabel}";
                if (row.NationId == "egypt")
                {
                    Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d?.provedBy, where);
                }
                else if (row.NationId == "neutral")
                {
                    Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d?.provedBy, where);
                    Assert.AreEqual(PresentLabel, d.actualOrigin, where);
                }
                else
                {
                    Assert.IsNull(d, where);
                }
            }
        }
    }

    [Test]
    public void AFalseOrigin_WithNoFilter_PlansAsBefore_AndCarriesItsKind()
    {
        LiePlan plan = Plan(Script(R(0), R(1)));
        Assert.AreEqual(LieKind.FalseOrigin, plan.Kind);
        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells, "Iraq's second option");
    }

    [Test]
    public void MayLie_NotHonestPremades_WithAnAllowedClaimAndPapers()
    {
        Assert.IsTrue(Lies.MayLie(false, true, true), "an ordinary traveller, or a premade authored as a liar");
        Assert.IsFalse(Lies.MayLie(true, true, true), "an honest premade");
        Assert.IsFalse(Lies.MayLie(false, false, true), "forbidden claim");
        Assert.IsFalse(Lies.MayLie(false, true, false), "no papers");
    }

    // -----------------------------
    // The roll (redesign phase 7): the roll, then the kind when two or more lies are enabled
    // -----------------------------

    /// <summary>Day 1's lies for a rich tourist: poor posing as rich, then a doctored identity (LieKinds.For keeps the plan's order).</summary>
    private static readonly LieKind[] TwoLies = { LieKind.PoorPosingAsRich, LieKind.DoctoredIdentity };

    [Test]
    public void Roll_AtOrAboveTheChance_IsHonest_AfterExactlyOneDraw()
    {
        ScriptedRandom rng = Script(V(0.5f));
        Assert.IsNull(Lies.Roll(0.5f, OneLie, rng));
        Assert.IsTrue(rng.Done);

        ScriptedRandom two = Script(V(0.5f));
        Assert.IsNull(Lies.Roll(0.5f, TwoLies, two), "no kind draw for an honest traveller");
        Assert.IsTrue(two.Done);
    }

    [Test]
    public void Roll_OneLieEnabled_DrawsTheRollOnly_AsBefore()
    {
        ScriptedRandom rng = Script(V(0.2f));
        Assert.AreEqual(LieKind.FalseOrigin, Lies.Roll(0.5f, OneLie, rng));
        Assert.IsTrue(rng.Done, "the roll alone: today's displaced keep their draws");
    }

    [Test]
    public void Roll_TwoOrMoreLiesEnabled_DrawsTheKind_UniformlyInThePlansOrder()
    {
        ScriptedRandom first = Script(V(0.2f), R(0));
        Assert.AreEqual(LieKind.PoorPosingAsRich, Lies.Roll(0.5f, TwoLies, first));
        Assert.IsTrue(first.Done, "the roll, then the kind");

        ScriptedRandom second = Script(V(0.2f), R(1));
        Assert.AreEqual(LieKind.DoctoredIdentity, Lies.Roll(0.5f, TwoLies, second));
        Assert.IsTrue(second.Done);
    }

    [Test]
    public void Roll_ChanceZeroNeverLies_ChanceOneAlwaysLies()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            Assert.IsNull(Lies.Roll(0f, TwoLies, new SeededRandom(seed)), $"seed {seed}");
            Assert.NotNull(Lies.Roll(1f, TwoLies, new SeededRandom(seed)), $"seed {seed}");
            Assert.AreEqual(LieKind.FalseOrigin, Lies.Roll(1f, OneLie, new SeededRandom(seed)), $"seed {seed}");
        }
    }

    [Test]
    public void Roll_NoLieEnabled_OrNoStream_IsHonest_WithNoDraw()
    {
        ScriptedRandom rng = Script();
        Assert.IsNull(Lies.Roll(1f, new LieKind[0], rng));
        Assert.IsNull(Lies.Roll(1f, null, rng));
        Assert.IsTrue(rng.Done);
        Assert.IsNull(Lies.Roll(1f, TwoLies, null));
    }

    [Test]
    public void Roll_TheSameSeed_GivesTheSameKind()
    {
        for (int seed = 0; seed < 100; seed++)
            Assert.AreEqual(Lies.Roll(1f, TwoLies, new SeededRandom(seed)), Lies.Roll(1f, TwoLies, new SeededRandom(seed)), $"seed {seed}");
    }

    // -----------------------------
    // The place lie's plan (the roll already made)
    // -----------------------------

    [Test]
    public void Plan_WithoutAStream_IsHonest_WithNoDraw()
    {
        LiePlan plan = Plan(null);
        Assert.AreEqual(LieOutcome.Honest, plan.Outcome);
        Assert.AreEqual(LieKind.FalseOrigin, plan.Kind);
        Assert.AreEqual(-1, plan.HomeIndex);
        CollectionAssert.IsEmpty(plan.Tells);
        CollectionAssert.IsEmpty(plan.RecordTells);
    }

    [Test]
    public void Plan_AlwaysPlansTheLie_TheRollIsTheCallers()
    {
        for (int seed = 0; seed < 300; seed++)
            Assert.AreEqual(LieOutcome.Liar, Plan(new SeededRandom(seed)).Outcome, $"seed {seed}");
    }

    [Test]
    public void GoldenOrder_PlaceFact_RollThenHomeThenTell()
    {
        ScriptedRandom rng = Script(R(0), R(1));
        List<DocumentField> papers = Papers();
        LiePlan plan = Plan(rng, papers: papers);

        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(2, plan.HomeIndex, "an index into todays, not into the filtered candidates (the twin was dropped)");
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.IsTrue(rng.Done, "exactly two draws: the home, then the tell");

        plan.ApplyTo(Docs(papers));
        HomeCandidate home = Today4[plan.HomeIndex];
        foreach (DocumentField f in papers.Where(f => f.category == ClueCategory.Currency))
            Assert.AreEqual(Facts().Get(home.NationId, home.EraId, ClueCategory.Currency), f.value);
    }

    [Test]
    public void GoldenOrder_BirthDate_RollThenHomeThenTellThenYear()
    {
        ScriptedRandom rng = Script(R(0), R(0), R(2));
        List<DocumentField> papers = Papers();
        LiePlan plan = Plan(rng, papers: papers);

        CollectionAssert.AreEqual(new[] { ClueCategory.BirthDate }, plan.Tells);
        Assert.IsTrue(rng.Done, "exactly three draws: the home, the tell, the year");

        // Iraq's years without the cover year, ascending: 1460..1451 BCE, 1449..1440 BCE; index 2 is 1458 BCE.
        plan.ApplyTo(Docs(papers));
        Assert.AreEqual("3 Jun 1458 BCE", papers.Single(f => f.category == ClueCategory.BirthDate).value);
    }

    [Test]
    public void TellPicks_FollowFirstAppearanceOrder_WithCurrencyCountedOnce()
    {
        ClueCategory TellAt(int index)
        {
            ScriptedRandom rng = Script(R(0), R(index));
            LiePlan plan = Plan(rng);
            Assert.IsTrue(rng.Done);
            return plan.Tells.Single();
        }

        Assert.AreEqual(ClueCategory.Currency, TellAt(1));
        Assert.AreEqual(ClueCategory.Language, TellAt(2));
        Assert.AreEqual(ClueCategory.Technology, TellAt(3));
    }

    [Test]
    public void TellCount_IsCappedAtTheHomesEligibleCategories_WithoutRepeats()
    {
        ScriptedRandom rng = Script(R(0), R(0), R(0), R(0), R(0), R(0));
        LiePlan plan = Plan(rng, tellCount: 9);
        Assert.AreEqual(4, plan.Tells.Count);
        Assert.AreEqual(4, plan.Tells.Distinct().Count());
        Assert.AreEqual(1, plan.Tells.Count(t => t == ClueCategory.Currency));
        Assert.IsTrue(rng.Done, "home, four tells, birth year");
    }

    [Test]
    public void TellCountBelowOne_StillGivesOneTell()
    {
        ScriptedRandom rng = Script(R(0), R(1));
        LiePlan plan = Plan(rng, tellCount: 0);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void AnUnprintedCategory_IsNeverAPapersTell()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            LiePlan plan = Plan(new SeededRandom(seed), tellCount: 9, papers: Papers(withPermit: false));
            CollectionAssert.DoesNotContain(plan.Tells, ClueCategory.Technology, $"seed {seed}");
        }
    }

    [Test]
    public void TheTrueHome_IsNeverTheClaimOrTheTwin()
    {
        for (int seed = 0; seed < 500; seed++)
        {
            LiePlan plan = Plan(new SeededRandom(seed));
            Assert.AreEqual(LieOutcome.Liar, plan.Outcome, $"seed {seed}");
            Assert.That(plan.HomeIndex, Is.EqualTo(2).Or.EqualTo(3), $"seed {seed}");
        }
    }

    [Test]
    public void OnlyTheClaimAndTheTwinToday_IsNoPossibleLie_WithNoDraw()
    {
        ScriptedRandom rng = Script();
        LiePlan plan = Plan(rng, todays: new[] { Egypt, Twin });
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome);
        Assert.AreEqual(-1, plan.HomeIndex);
        CollectionAssert.IsEmpty(plan.Tells);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void EligibilityIsPerHome_ItalyOnlyGivesCurrency()
    {
        ScriptedRandom rng = Script(R(0), R(0));
        LiePlan plan = Plan(rng, tellCount: 9, todays: new[] { Egypt, Italy });
        Assert.AreEqual(1, plan.HomeIndex);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.IsTrue(rng.Done, "the home and the tell; no birth-year draw: Italy's only year is the cover year");
    }

    [Test]
    public void AHomeValueSharedWithAnotherPlace_IsNeverATell()
    {
        var claim = new HomeCandidate("egypt", "ancient", 0, 0);
        var iraq = new HomeCandidate("iraq", "ancient", 0, 0);
        var greece = new HomeCandidate("greece", "ancient", 0, 0);
        var facts = new FactTable();
        facts.Add("egypt", "ancient", "Egypt", ClueCategory.Currency, "Deben");
        facts.Add("iraq", "ancient", "Iraq", ClueCategory.Currency, "Silver shekel");
        facts.Add("greece", "ancient", "Greece", ClueCategory.Currency, "silver shekel");
        var papers = new List<DocumentField> { Field(ClueCategory.Currency, "Coin of Issue", "Deben", 0) };

        ScriptedRandom rng = Script();
        LiePlan plan = Lies.Plan(1, "egypt", "ancient", Cover, new[] { claim, iraq, greece }, papers, None, PapersOnly, facts, Books, rng);
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome);
        Assert.IsTrue(rng.Done);

        var italy = new HomeCandidate("italy", "ancient", 0, 0);
        facts.Add("italy", "ancient", "Italy", ClueCategory.Currency, "Denarius");
        for (int seed = 0; seed < 100; seed++)
            Assert.AreEqual(3, Lies.Plan(1, "egypt", "ancient", Cover, new[] { claim, iraq, greece, italy }, papers, None, PapersOnly, facts, Books, new SeededRandom(seed)).HomeIndex, $"seed {seed}");
    }

    [Test]
    public void TheSameSeed_GivesTheSamePlan()
    {
        LiePlan x = Plan(new SeededRandom(7), tellCount: 2);
        LiePlan y = Plan(new SeededRandom(7), tellCount: 2);
        Assert.AreEqual(x.Outcome, y.Outcome);
        Assert.AreEqual(x.HomeIndex, y.HomeIndex);
        CollectionAssert.AreEqual(x.Tells, y.Tells);

        List<DocumentField> px = Papers(), py = Papers();
        x.ApplyTo(Docs(px));
        y.ApplyTo(Docs(py));
        CollectionAssert.AreEqual(px.Select(f => f.value).ToList(), py.Select(f => f.value).ToList());
    }

    [Test]
    public void ApplyTo_RewritesAndFlagsEveryFieldOfTheTellCategory_AndNothingElse()
    {
        List<DocumentField> papers = Papers();
        Plan(Script(R(0), R(1))).ApplyTo(Docs(papers));

        List<DocumentField> honest = Papers();
        for (int i = 0; i < papers.Count; i++)
        {
            if (papers[i].category == ClueCategory.Currency)
            {
                Assert.AreEqual("Silver shekel", papers[i].value, papers[i].label);
                Assert.IsTrue(papers[i].isAnachronism, papers[i].label);
            }
            else
            {
                Assert.AreEqual(honest[i].value, papers[i].value, papers[i].label);
                Assert.IsFalse(papers[i].isAnachronism, papers[i].label);
            }
        }
    }

    [Test]
    public void ApplyTo_ABirthDateTell_KeepsDayAndMonth_TakesAHomeYear_NeverTheRecordYear()
    {
        for (int index = 0; index < 20; index++)
        {
            List<DocumentField> papers = Papers();
            Plan(Script(R(0), R(0), R(index))).ApplyTo(Docs(papers));
            DocumentField born = papers.Single(f => f.category == ClueCategory.BirthDate);
            Assert.IsTrue(born.isAnachronism);
            Assert.IsTrue(BirthDates.TryParse(born.value, out int d, out int m, out int y), born.value);
            Assert.AreEqual(3, d);
            Assert.AreEqual(5, m);
            Assert.That(y, Is.InRange(-1460, -1440));
            Assert.AreNotEqual(CoverYear, y);
        }
    }

    [Test]
    public void ApplyTo_AnHonestOrImpossiblePlan_ChangesNothing()
    {
        List<DocumentField> papers = Papers();
        Plan(null).ApplyTo(Docs(papers));
        Plan(Script(), todays: new[] { Egypt, Twin }).ApplyTo(Docs(papers));

        List<DocumentField> honest = Papers();
        for (int i = 0; i < papers.Count; i++)
        {
            Assert.AreEqual(honest[i].value, papers[i].value);
            Assert.IsFalse(papers[i].isAnachronism);
        }
    }

    [Test]
    public void EveryTell_RegistersAgainstTheClaimTheHomeAndTheRecord_AndNothingElse()
    {
        FactTable facts = Facts();
        List<DocumentField> papers = Papers();
        Plan(Script(R(0), R(0), R(0), R(0), R(0), R(0)), tellCount: 9, papers: papers, facts: facts).ApplyTo(Docs(papers));

        foreach (DocumentField f in papers.Where(f => f.isAnachronism))
        {
            CompareEvidence doc = CompareEvidence.FromDocumentField(f, 0);
            if (f.category == ClueCategory.BirthDate)
            {
                Discrepancy record = DiscrepancyLog.Prove(doc, CompareEvidence.ForRecordField(ClueCategory.BirthDate, Cover, Traveller), "egypt", "ancient", Traveller);
                Assert.AreEqual(DiscrepancyProof.RecordMismatch, record?.provedBy, f.label);
                continue;
            }

            foreach (FactRow row in facts.Rows(f.category))
            {
                Discrepancy d = DiscrepancyLog.Prove(doc, row.ToEvidence(), "egypt", "ancient", Traveller);
                string where = $"{f.label} vs {row.OriginLabel}";
                if (row.NationId == "egypt")
                {
                    Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d?.provedBy, where);
                }
                else if (row.NationId == "iraq")
                {
                    Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d?.provedBy, where);
                    Assert.AreEqual("Babylonia (Ancient)", d.actualOrigin, where);
                }
                else
                {
                    Assert.IsNull(d, where);
                }
            }
        }
    }

    // -----------------------------
    // Two channels: papers and answers
    // -----------------------------

    /// <summary>Books plus a Geography book (the Capitals Gazetteer).</summary>
    private static readonly HashSet<ClueCategory> AnswerBooks = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography };

    /// <summary>Facts() plus capitals: Egypt "Thebes", the twin " thebes ", Iraq "Babylon", Italy "Rome".</summary>
    private static FactTable AnswerFacts()
    {
        FactTable t = Facts();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Geography, "Thebes");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Geography, " thebes ");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Geography, "Babylon");
        t.Add("italy", "ancient", "Republican Rome (Ancient)", ClueCategory.Geography, "Rome");
        return t;
    }

    private static LiePlan PlanSpoken(IRandomSource rng, IReadOnlyList<ClueCategory> answerTellCategories, IReadOnlyList<TellChannel> channels,
                                      int tellCount = 1, IReadOnlyList<HomeCandidate> todays = null, List<DocumentField> papers = null,
                                      FactTable facts = null) =>
        Lies.Plan(tellCount, "egypt", "ancient", Cover, todays ?? Today4, papers ?? Papers(), answerTellCategories, channels,
                  facts ?? AnswerFacts(), AnswerBooks, rng);

    /// <summary>Every field still shows Egypt's honest value, unflagged.</summary>
    private static void AssertPapersAreTheCover(List<DocumentField> papers)
    {
        List<DocumentField> honest = Papers();
        for (int i = 0; i < papers.Count; i++)
        {
            Assert.AreEqual(honest[i].value, papers[i].value, papers[i].label);
            Assert.IsFalse(papers[i].isAnachronism, papers[i].label);
        }
    }

    [Test]
    public void GoldenOrder_Answer_TheOptionsArePapersThenAnswers()
    {
        // Iraq's options: P:BirthDate, P:Currency, P:Language, P:Technology, A:Currency, A:Geography.
        ScriptedRandom rng = Script(R(0), R(5));
        List<DocumentField> papers = Papers();
        LiePlan plan = PlanSpoken(rng, new[] { ClueCategory.Currency, ClueCategory.Geography }, Both, papers: papers);

        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(2, plan.HomeIndex);
        CollectionAssert.AreEqual(new[] { ClueCategory.Geography }, plan.Tells);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.Geography));
        Assert.AreEqual("Babylon", plan.TellValue(ClueCategory.Geography));
        Assert.IsNull(plan.ChannelOf(ClueCategory.Currency), "not a tell");
        Assert.IsNull(plan.TellValue(ClueCategory.Currency), "not a tell");
        Assert.IsTrue(rng.Done, "exactly two draws");

        plan.ApplyTo(Docs(papers));
        AssertPapersAreTheCover(papers);
    }

    [Test]
    public void AnAnswerTell_InAPrintedCategory_LeavesThePapersOnTheCover()
    {
        ScriptedRandom rng = Script(R(0), R(4));
        List<DocumentField> papers = Papers();
        LiePlan plan = PlanSpoken(rng, new[] { ClueCategory.Currency, ClueCategory.Geography }, Both, papers: papers);

        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.Currency));
        Assert.AreEqual("Silver shekel", plan.TellValue(ClueCategory.Currency));
        Assert.IsTrue(rng.Done);

        plan.ApplyTo(Docs(papers));
        AssertPapersAreTheCover(papers);
    }

    [Test]
    public void ACategoryLeaksOnOneChannelOnly()
    {
        ScriptedRandom rng = Script(R(0), R(0), R(0), R(0), R(0), R(0), R(0));
        LiePlan plan = PlanSpoken(rng, new[] { ClueCategory.Currency, ClueCategory.Geography }, Both, tellCount: 9);

        CollectionAssert.AreEqual(new[] { ClueCategory.BirthDate, ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography }, plan.Tells);
        CollectionAssert.AreEqual(
            new TellChannel?[] { TellChannel.Papers, TellChannel.Papers, TellChannel.Papers, TellChannel.Papers, TellChannel.Answer },
            plan.Tells.Select(t => plan.ChannelOf(t)).ToArray());
        Assert.IsTrue(rng.Done, "home, five tells, then the birth year: 7 draws");
    }

    [Test]
    public void PapersOnly_ReproducesPiece2_WhateverQuestionsAreAsked()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            List<DocumentField> asked = Papers(), unasked = Papers();
            LiePlan x = PlanSpoken(new SeededRandom(seed), new[] { ClueCategory.Currency, ClueCategory.Geography }, PapersOnly, papers: asked);
            LiePlan y = PlanSpoken(new SeededRandom(seed), None, PapersOnly, papers: unasked);

            Assert.AreEqual(y.Outcome, x.Outcome, $"seed {seed}");
            Assert.AreEqual(y.HomeIndex, x.HomeIndex, $"seed {seed}");
            CollectionAssert.AreEqual(y.Tells, x.Tells, $"seed {seed}");

            x.ApplyTo(Docs(asked));
            y.ApplyTo(Docs(unasked));
            CollectionAssert.AreEqual(unasked.Select(f => f.value).ToList(), asked.Select(f => f.value).ToList(), $"seed {seed}");
        }
    }

    [Test]
    public void AnswerOnly_WithNoTellCarryingQuestion_IsNoPossibleLie_WithNoDraw()
    {
        ScriptedRandom rng = Script();
        Assert.AreEqual(LieOutcome.NoPossibleLie, PlanSpoken(rng, None, AnswerOnly).Outcome);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void AHomeEligibleOnlyThroughAnAnswer_IsACandidate()
    {
        // Ugarit matches the claim in everything printed and has no birth years; only its capital differs.
        var syria = new HomeCandidate("syria", "ancient", 0, 0);
        FactTable facts = AnswerFacts();
        facts.Add("syria", "ancient", "Ugarit (Ancient)", ClueCategory.Currency, "DEBEN");
        facts.Add("syria", "ancient", "Ugarit (Ancient)", ClueCategory.Language, "middle egyptian");
        facts.Add("syria", "ancient", "Ugarit (Ancient)", ClueCategory.Technology, "Papyrus");
        facts.Add("syria", "ancient", "Ugarit (Ancient)", ClueCategory.Geography, "Ugarit");
        var todays = new[] { Egypt, syria };

        ScriptedRandom spoken = Script(R(0), R(0));
        LiePlan plan = PlanSpoken(spoken, new[] { ClueCategory.Geography }, Both, todays: todays, facts: facts);
        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(1, plan.HomeIndex);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.Geography));
        Assert.AreEqual("Ugarit", plan.TellValue(ClueCategory.Geography));
        Assert.IsTrue(spoken.Done);

        ScriptedRandom printed = Script();
        Assert.AreEqual(LieOutcome.NoPossibleLie, PlanSpoken(printed, new[] { ClueCategory.Geography }, PapersOnly, todays: todays, facts: facts).Outcome);
        Assert.IsTrue(printed.Done);
    }

    [Test]
    public void ABirthDateAnswerTell_KeepsThePapersOnTheCover_AndDrawsTheYearLast()
    {
        // Only Iraq has a birth year other than the cover's.
        ScriptedRandom rng = Script(R(0), R(0), R(2));
        List<DocumentField> papers = Papers();
        LiePlan plan = PlanSpoken(rng, new[] { ClueCategory.BirthDate }, AnswerOnly, papers: papers);

        Assert.AreEqual(2, plan.HomeIndex);
        CollectionAssert.AreEqual(new[] { ClueCategory.BirthDate }, plan.Tells);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.BirthDate));
        Assert.AreEqual("3 Jun 1458 BCE", plan.TellValue(ClueCategory.BirthDate));
        Assert.IsTrue(rng.Done, "home, tell, year");

        plan.ApplyTo(Docs(papers));
        AssertPapersAreTheCover(papers);
    }

    [Test]
    public void AValueTheHomeSharesWithTheClaim_IsNeverAnAnswerTell()
    {
        FactTable facts = AnswerFacts();
        facts.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Politics, "Sultan Mustafa II");
        facts.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Politics, "sultan mustafa ii");
        var books = new HashSet<ClueCategory>(AnswerBooks) { ClueCategory.Politics };

        ScriptedRandom rng = Script();
        LiePlan plan = Lies.Plan(1, "egypt", "ancient", Cover, new[] { Egypt, Iraq }, Papers(), new[] { ClueCategory.Politics }, AnswerOnly, facts, books, rng);
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void EveryAnswerTell_ProvesAgainstTheClaimAndTheHome_AndNothingElse()
    {
        FactTable facts = AnswerFacts();
        LiePlan plan = PlanSpoken(Script(R(0), R(0), R(0)), new[] { ClueCategory.Currency, ClueCategory.Geography }, AnswerOnly, tellCount: 9, facts: facts);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Geography }, plan.Tells);

        foreach (ClueCategory category in plan.Tells)
        {
            CompareEvidence said = CompareEvidence.ForAnswer(category, plan.TellValue(category), true);
            foreach (FactRow row in facts.Rows(category))
            {
                Discrepancy d = DiscrepancyLog.Prove(said, row.ToEvidence(), "egypt", "ancient", Traveller);
                string where = $"{category} vs {row.OriginLabel}";
                if (row.NationId == "egypt")
                {
                    Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d?.provedBy, where);
                    Assert.AreEqual(EvidenceKind.Answer, d.source, where);
                }
                else if (row.NationId == "iraq")
                {
                    Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d?.provedBy, where);
                    Assert.AreEqual("Babylonia (Ancient)", d.actualOrigin, where);
                }
                else
                {
                    Assert.IsNull(d, where);
                }
            }
        }

        LiePlan born = PlanSpoken(Script(R(0), R(0), R(2)), new[] { ClueCategory.BirthDate }, AnswerOnly);
        Discrepancy record = DiscrepancyLog.Prove(
            CompareEvidence.ForAnswer(ClueCategory.BirthDate, born.TellValue(ClueCategory.BirthDate), true),
            CompareEvidence.ForRecordField(ClueCategory.BirthDate, Cover, Traveller), "egypt", "ancient", Traveller);
        Assert.AreEqual(DiscrepancyProof.RecordMismatch, record?.provedBy);
    }

    // -----------------------------
    // Dress tells (the Appearance channel)
    // -----------------------------

    /// <summary>Every channel (day 3 on).</summary>
    private static readonly TellChannel[] All3 = { TellChannel.Papers, TellChannel.Answer, TellChannel.Appearance };

    /// <summary>The books plus the Costume Guide.</summary>
    private static readonly HashSet<ClueCategory> BooksAndDress = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Culture };

    /// <summary>The fixture's facts plus Culture values: the twin's equals Egypt's, Iraq's and Italy's are their own (or, with <paramref name="shared"/>, Iraq's equals Italy's).</summary>
    private static FactTable FactsWithDress(bool shared = false)
    {
        FactTable t = Facts();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Culture, "wesekh collar");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Culture, " WESEKH COLLAR ");
        t.Add("iraq", "ancient", "Babylonia (Ancient)", ClueCategory.Culture, shared ? "Caesar crop / nodus roll" : "curled beard / gold fillet");
        t.Add("italy", "ancient", "Republican Rome (Ancient)", ClueCategory.Culture, "Caesar crop / nodus roll");
        return t;
    }

    private static HomeCandidate Leakable(HomeCandidate p) => new HomeCandidate(p.NationId, p.EraId, p.BirthYearMin, p.BirthYearMax, true);

    private static LiePlan PlanDress(IRandomSource rng, IReadOnlyList<HomeCandidate> todays, IReadOnlyList<TellChannel> channels,
                                     FactTable facts = null, IReadOnlyList<ClueCategory> asked = null) =>
        Lies.Plan(1, "egypt", "ancient", Cover, todays, Papers(), asked ?? None, channels, facts ?? FactsWithDress(), BooksAndDress, rng);

    [Test]
    public void GoldenOrder_ADressTellIsTheLastOptionOfAHome()
    {
        // Iraq's options: Papers BirthDate, Currency, Language, Technology, then Appearance Culture.
        ScriptedRandom rng = Script(R(0), R(4));
        List<DocumentField> papers = Papers();
        LiePlan plan = Lies.Plan(1, "egypt", "ancient", Cover, new[] { Egypt, Twin, Leakable(Iraq), Italy }, papers, None, All3, FactsWithDress(), BooksAndDress, rng);

        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(2, plan.HomeIndex);
        CollectionAssert.AreEqual(new[] { ClueCategory.Culture }, plan.Tells);
        Assert.AreEqual(TellChannel.Appearance, plan.ChannelOf(ClueCategory.Culture));
        Assert.AreEqual("curled beard / gold fillet", plan.TellValue(ClueCategory.Culture));

        List<string> before = papers.Select(f => f.value).ToList();
        plan.ApplyTo(Docs(papers));
        CollectionAssert.AreEqual(before, papers.Select(f => f.value).ToList(), "a dress tell leaves the papers on the cover");
        Assert.IsFalse(papers.Any(f => f.isAnachronism));
        Assert.IsTrue(rng.Done, "home, one tell");
    }

    [Test]
    public void AppearanceAllowed_ButNotLeakable_GivesNoDressOption()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            LiePlan plan = PlanDress(new SeededRandom(seed), Today4, All3);
            Assert.IsNull(plan.ChannelOf(ClueCategory.Culture), $"seed {seed}");
        }
    }

    [Test]
    public void AHomeEligibleOnlyThroughDress_IsACandidateWithAppearance_AndNotWithout()
    {
        // A second twin of Egypt, born with no birth years, whose dress alone differs.
        var dressTwin = new HomeCandidate("japan", "ancient", 0, 0, true);
        FactTable facts = FactsWithDress();
        facts.Add("japan", "ancient", "Kofun Yamato (Ancient)", ClueCategory.Currency, "Deben");
        facts.Add("japan", "ancient", "Kofun Yamato (Ancient)", ClueCategory.Language, "Middle Egyptian");
        facts.Add("japan", "ancient", "Kofun Yamato (Ancient)", ClueCategory.Technology, "Papyrus");
        facts.Add("japan", "ancient", "Kofun Yamato (Ancient)", ClueCategory.Culture, "mizura / magatama beads");
        var todays = new[] { Egypt, dressTwin };

        Assert.AreEqual(LieOutcome.NoPossibleLie, PlanDress(Script(), todays, Both, facts).Outcome, "without Appearance");
        LiePlan plan = PlanDress(Script(R(0), R(0)), todays, All3, facts);
        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(1, plan.HomeIndex);
        Assert.AreEqual(TellChannel.Appearance, plan.ChannelOf(ClueCategory.Culture));
    }

    [Test]
    public void WithoutTheAppearanceChannel_PlansAreThoseOfPieceThree()
    {
        var leakable = new[] { Egypt, Leakable(Twin), Leakable(Iraq), Leakable(Italy) };
        for (int seed = 0; seed <= 300; seed++)
        {
            LiePlan before = Lies.Plan(2, "egypt", "ancient", Cover, Today4, Papers(), new[] { ClueCategory.Currency }, Both, Facts(), Books, new SeededRandom(seed));
            LiePlan after = Lies.Plan(2, "egypt", "ancient", Cover, leakable, Papers(), new[] { ClueCategory.Currency }, Both, FactsWithDress(), BooksAndDress, new SeededRandom(seed));
            Assert.AreEqual(before.Outcome, after.Outcome, $"seed {seed}");
            Assert.AreEqual(before.HomeIndex, after.HomeIndex, $"seed {seed}");
            CollectionAssert.AreEqual(before.Tells, after.Tells, $"seed {seed}");
            foreach (ClueCategory c in before.Tells)
            {
                Assert.AreEqual(before.ChannelOf(c), after.ChannelOf(c), $"seed {seed} {c}");
                Assert.AreEqual(before.TellValue(c), after.TellValue(c), $"seed {seed} {c}");
            }
        }
    }

    [Test]
    public void ASharedCultureValue_RemovesTheDressOption()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            LiePlan plan = PlanDress(new SeededRandom(seed), new[] { Egypt, Twin, Leakable(Iraq), Italy }, All3, FactsWithDress(shared: true));
            Assert.IsNull(plan.ChannelOf(ClueCategory.Culture), $"seed {seed}: Iraq's dress belongs to Rome too, so it cannot prove Iraq");
        }
    }

    [Test]
    public void APremadeLiar_OneCandidate_KeepsTheAnswerOptions()
    {
        // Iraq's options: Papers BirthDate, Currency, Language, Technology, then Answer Currency.
        ScriptedRandom rng = Script(R(0), R(4));
        LiePlan plan = Lies.Plan(1, "egypt", "ancient", Cover, new[] { Iraq }, Papers(), new[] { ClueCategory.Currency }, Both, Facts(), Books, rng);
        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(0, plan.HomeIndex);
        Assert.AreEqual(TellChannel.Answer, plan.ChannelOf(ClueCategory.Currency));
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void ADressTell_ProvesAgainstTheClaimAndTheHome_AndNothingElse()
    {
        FactTable facts = FactsWithDress();
        LiePlan plan = PlanDress(Script(R(0), R(4)), new[] { Egypt, Twin, Leakable(Iraq), Italy }, All3, facts);
        CompareEvidence worn = CompareEvidence.ForAppearance(ClueCategory.Culture, plan.TellValue(ClueCategory.Culture), true);
        foreach (FactRow row in facts.Rows(ClueCategory.Culture))
        {
            Discrepancy d = DiscrepancyLog.Prove(worn, row.ToEvidence(), "egypt", "ancient", Traveller);
            string where = $"Culture vs {row.OriginLabel}";
            if (row.NationId == "egypt")
            {
                Assert.AreEqual(DiscrepancyProof.ClaimMismatch, d?.provedBy, where);
                Assert.AreEqual(EvidenceKind.Appearance, d.source, where);
            }
            else if (row.NationId == "iraq")
            {
                Assert.AreEqual(DiscrepancyProof.ForeignOrigin, d?.provedBy, where);
                Assert.AreEqual("Babylonia (Ancient)", d.actualOrigin, where);
            }
            else
            {
                Assert.IsNull(d, where);
            }
        }
    }
}
