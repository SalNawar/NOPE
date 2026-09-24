using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// Lie planning. Today, in order: the claim New Kingdom Egypt; a twin (Greece)
/// whose values equal Egypt's under the scanner comparison and who has no
/// birth years; Babylonia (Iraq), different in every book category and born
/// 1460..1440 BCE (around the cover year 1450 BCE); Republican Rome (Italy),
/// different only in Currency and born exactly in the cover year. Iraq's
/// values and Italy's Currency appear nowhere else. The papers follow the real
/// templates (passport: Name, BirthDate, Currency, Language; permit:
/// Technology, Currency), so Iraq's eligible tells are BirthDate, Currency,
/// Language and Technology, in that order.
/// </summary>
public class LiesTests
{
    private const string Cover = "3 Jun 1450 BCE";
    private const int CoverYear = -1450;

    private static readonly HashSet<ClueCategory> Books = new HashSet<ClueCategory> { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology };

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
                                List<DocumentField> papers = null, float chance = 0.5f, FactTable facts = null) =>
        Lies.Plan(chance, tellCount, "egypt", "ancient", Cover, todays ?? Today4, papers ?? Papers(), facts ?? Facts(), Books, rng);

    private static ScriptedRandom Script(params ScriptStep[] steps) => new ScriptedRandom(steps);
    private static ScriptStep V(float roll) => ScriptStep.Value(roll);
    private static ScriptStep R(int offset) => ScriptStep.Range(offset);

    [Test]
    public void MayLie_OnlyNonLegendaryTravellersWithAnAllowedClaimAndPapers()
    {
        Assert.IsTrue(Lies.MayLie(false, true, Papers()));
        Assert.IsFalse(Lies.MayLie(true, true, Papers()), "legendary");
        Assert.IsFalse(Lies.MayLie(false, false, Papers()), "forbidden claim");
        Assert.IsFalse(Lies.MayLie(false, true, null), "no papers");
        Assert.IsFalse(Lies.MayLie(false, true, new List<DocumentField>()), "empty papers");
    }

    [Test]
    public void ARollAtOrAboveTheChance_IsHonest_AfterExactlyOneDraw()
    {
        ScriptedRandom rng = Script(V(0.5f));
        LiePlan plan = Plan(rng, chance: 0.5f);
        Assert.AreEqual(LieOutcome.Honest, plan.Outcome);
        Assert.AreEqual(-1, plan.HomeIndex);
        CollectionAssert.IsEmpty(plan.Tells);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void ChanceZero_NeverLies_ChanceOne_AlwaysLies()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            Assert.AreEqual(LieOutcome.Honest, Plan(new SeededRandom(seed), chance: 0f).Outcome, $"seed {seed}");
            Assert.AreEqual(LieOutcome.Liar, Plan(new SeededRandom(seed), chance: 1f).Outcome, $"seed {seed}");
        }
    }

    [Test]
    public void GoldenOrder_PlaceFact_RollThenHomeThenTell()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(1));
        List<DocumentField> papers = Papers();
        LiePlan plan = Plan(rng, papers: papers);

        Assert.AreEqual(LieOutcome.Liar, plan.Outcome);
        Assert.AreEqual(2, plan.HomeIndex, "an index into todays, not into the filtered candidates (the twin was dropped)");
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.IsTrue(rng.Done, "exactly three draws");

        plan.ApplyTo(papers);
        HomeCandidate home = Today4[plan.HomeIndex];
        foreach (DocumentField f in papers.Where(f => f.category == ClueCategory.Currency))
            Assert.AreEqual(Facts().Get(home.NationId, home.EraId, ClueCategory.Currency), f.value);
    }

    [Test]
    public void GoldenOrder_BirthDate_RollThenHomeThenTellThenYear()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(0), R(2));
        List<DocumentField> papers = Papers();
        LiePlan plan = Plan(rng, papers: papers);

        CollectionAssert.AreEqual(new[] { ClueCategory.BirthDate }, plan.Tells);
        Assert.IsTrue(rng.Done, "exactly four draws");

        // Iraq's years without the cover year, ascending: 1460..1451 BCE, 1449..1440 BCE; index 2 is 1458 BCE.
        plan.ApplyTo(papers);
        Assert.AreEqual("3 Jun 1458 BCE", papers.Single(f => f.category == ClueCategory.BirthDate).value);
    }

    [Test]
    public void TellPicks_FollowFirstAppearanceOrder_WithCurrencyCountedOnce()
    {
        ClueCategory TellAt(int index)
        {
            ScriptedRandom rng = Script(V(0f), R(0), R(index));
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
        ScriptedRandom rng = Script(V(0f), R(0), R(0), R(0), R(0), R(0), R(0));
        LiePlan plan = Plan(rng, tellCount: 9);
        Assert.AreEqual(4, plan.Tells.Count);
        Assert.AreEqual(4, plan.Tells.Distinct().Count());
        Assert.AreEqual(1, plan.Tells.Count(t => t == ClueCategory.Currency));
        Assert.IsTrue(rng.Done, "roll, home, four tells, birth year");
    }

    [Test]
    public void TellCountBelowOne_StillGivesOneTell()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(1));
        LiePlan plan = Plan(rng, tellCount: 0);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void AnUnprintedCategory_IsNeverATell()
    {
        for (int seed = 0; seed < 300; seed++)
        {
            LiePlan plan = Plan(new SeededRandom(seed), tellCount: 9, papers: Papers(withPermit: false), chance: 1f);
            CollectionAssert.DoesNotContain(plan.Tells, ClueCategory.Technology, $"seed {seed}");
        }
    }

    [Test]
    public void TheTrueHome_IsNeverTheClaimOrTheTwin()
    {
        for (int seed = 0; seed < 500; seed++)
        {
            LiePlan plan = Plan(new SeededRandom(seed), chance: 1f);
            Assert.AreEqual(LieOutcome.Liar, plan.Outcome, $"seed {seed}");
            Assert.That(plan.HomeIndex, Is.EqualTo(2).Or.EqualTo(3), $"seed {seed}");
        }
    }

    [Test]
    public void OnlyTheClaimAndTheTwinToday_IsNoPossibleLie_AfterOneDraw()
    {
        ScriptedRandom rng = Script(V(0f));
        LiePlan plan = Plan(rng, todays: new[] { Egypt, Twin });
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome);
        Assert.AreEqual(-1, plan.HomeIndex);
        CollectionAssert.IsEmpty(plan.Tells);
        Assert.IsTrue(rng.Done);
    }

    [Test]
    public void EligibilityIsPerHome_ItalyOnlyGivesCurrency()
    {
        ScriptedRandom rng = Script(V(0f), R(0), R(0));
        LiePlan plan = Plan(rng, tellCount: 9, todays: new[] { Egypt, Italy });
        Assert.AreEqual(1, plan.HomeIndex);
        CollectionAssert.AreEqual(new[] { ClueCategory.Currency }, plan.Tells);
        Assert.IsTrue(rng.Done, "no birth-year draw: Italy's only year is the cover year");
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

        ScriptedRandom rng = Script(V(0f));
        LiePlan plan = Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { claim, iraq, greece }, papers, facts, Books, rng);
        Assert.AreEqual(LieOutcome.NoPossibleLie, plan.Outcome);
        Assert.IsTrue(rng.Done);

        var italy = new HomeCandidate("italy", "ancient", 0, 0);
        facts.Add("italy", "ancient", "Italy", ClueCategory.Currency, "Denarius");
        for (int seed = 0; seed < 100; seed++)
            Assert.AreEqual(3, Lies.Plan(1f, 1, "egypt", "ancient", Cover, new[] { claim, iraq, greece, italy }, papers, facts, Books, new SeededRandom(seed)).HomeIndex, $"seed {seed}");
    }

    [Test]
    public void TheSameSeed_GivesTheSamePlan()
    {
        LiePlan x = Plan(new SeededRandom(7), tellCount: 2, chance: 1f);
        LiePlan y = Plan(new SeededRandom(7), tellCount: 2, chance: 1f);
        Assert.AreEqual(x.Outcome, y.Outcome);
        Assert.AreEqual(x.HomeIndex, y.HomeIndex);
        CollectionAssert.AreEqual(x.Tells, y.Tells);

        List<DocumentField> px = Papers(), py = Papers();
        x.ApplyTo(px);
        y.ApplyTo(py);
        CollectionAssert.AreEqual(px.Select(f => f.value).ToList(), py.Select(f => f.value).ToList());
    }

    [Test]
    public void ApplyTo_RewritesAndFlagsEveryFieldOfTheTellCategory_AndNothingElse()
    {
        List<DocumentField> papers = Papers();
        Plan(Script(V(0f), R(0), R(1))).ApplyTo(papers);

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
            Plan(Script(V(0f), R(0), R(0), R(index))).ApplyTo(papers);
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
        Plan(Script(V(0.9f))).ApplyTo(papers);
        Plan(Script(V(0f)), todays: new[] { Egypt, Twin }).ApplyTo(papers);

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
        Plan(Script(V(0f), R(0), R(0), R(0), R(0), R(0), R(0)), tellCount: 9, papers: papers, facts: facts).ApplyTo(papers);

        foreach (DocumentField f in papers.Where(f => f.isAnachronism))
        {
            CompareEvidence doc = CompareEvidence.FromDocumentField(f);
            if (f.category == ClueCategory.BirthDate)
            {
                Discrepancy record = new DiscrepancyLog().TryRegister(doc, CompareEvidence.ForRecordField(ClueCategory.BirthDate, Cover), "egypt", "ancient");
                Assert.AreEqual(DiscrepancyProof.RecordMismatch, record?.provedBy, f.label);
                continue;
            }

            foreach (FactRow row in facts.Rows(f.category))
            {
                Discrepancy d = new DiscrepancyLog().TryRegister(doc, row.ToEvidence(), "egypt", "ancient");
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
}
