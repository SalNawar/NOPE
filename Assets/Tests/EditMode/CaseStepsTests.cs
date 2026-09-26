using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// The optional steps checklist (the PC redesign ST1-ST4, §4.4; redesign phase
/// 21): which set a traveller gets (by kind, inherit and override by id, the
/// default for an unknown kind, a step's first day), when each kind of step
/// ticks (each StepWhen, with its parts: "Papers 1 of 3"), that a step ticks
/// when the player made the check and never on what it found (a MISMATCH ticks
/// like a MATCH), that a hand-set tick holds for the case, and where a click
/// on a step goes.
/// </summary>
public class CaseStepsTests
{
    // ---- fixtures ----

    private static StepSpec Step(string id, StepWhen when, StatementKind statement = StatementKind.Any, TruthKind truth = TruthKind.Any,
                                 string[] categories = null, string[] forms = null, int fromDay = 1) => new StepSpec
    {
        id = id,
        when = when,
        statement = statement,
        truth = truth,
        categories = (categories ?? new string[0]).ToList(),
        forms = (forms ?? new string[0]).ToList(),
        link = StepLink.Tab,
        tab = AppTab.Documents,
        fromDay = fromDay
    };

    private static StepSet Set(string type, params StepSpec[] steps) => new StepSet { type = type, steps = steps.ToList() };

    private static StepSetData Data(params StepSet[] sets) => new StepSetData { sets = sets.ToList() };

    private static DocumentField F(ClueCategory c, string value) => new DocumentField { category = c, label = c.ToString(), value = value };

    /// <summary>A displaced traveller's three papers: the certificate (primary, on arrival), the declaration and the return order (on request).</summary>
    private static List<StepPaper> DisplacedPapers() => new List<StepPaper>
    {
        new StepPaper("TC-610", false, true, new[] { F(ClueCategory.Name, "Iset Nefer"), F(ClueCategory.CitizenId, "DP-4471-02"), F(ClueCategory.BirthDate, "3 Mar 1480 BC"),
                                                     F(ClueCategory.Destination, "New Kingdom Egypt"), F(ClueCategory.Incident, "R-0311-07"), F(ClueCategory.Expiry, "20 Mar 2150") }),
        new StepPaper("TC-620", true, false, new[] { F(ClueCategory.Name, "Iset Nefer"), F(ClueCategory.CitizenId, "DP-4471-02"), F(ClueCategory.Currency, "deben"),
                                                     F(ClueCategory.Language, "Egyptian"), F(ClueCategory.Technology, "shaduf") }),
        new StepPaper("TC-630", true, false, new[] { F(ClueCategory.Name, "Iset Nefer"), F(ClueCategory.CitizenId, "DP-4471-02"), F(ClueCategory.Destination, "New Kingdom Egypt"),
                                                     F(ClueCategory.Incident, "R-0311-07"), F(ClueCategory.DepartureDate, "14 Mar 2150") }),
    };

    private static readonly ClueCategory[] Books =
        { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology, ClueCategory.Geography, ClueCategory.Politics, ClueCategory.Culture };

    private static CaseProgress Progress(params ClueCategory[] questions) =>
        new CaseProgress(DisplacedPapers(), questions.Length > 0 ? questions : new[] { ClueCategory.Currency, ClueCategory.Language, ClueCategory.BirthDate }, Books);

    private static StepState State(StepSpec step, CaseProgress p) => CaseSteps.Evaluate(new[] { step }, p).Single();

    private static CompareEvidence Field(ClueCategory c, string v) => CompareEvidence.FromDocumentField(F(c, v));

    private static CompareEvidence Book(ClueCategory c, string v) => CompareEvidence.ForReferenceEntry(c, v, "egypt", "ancient", "Egypt (Ancient)");

    private static CompareEvidence Record(ClueCategory c, string v) => CompareEvidence.ForRecordField(c, v, "Iset Nefer");

    // ---- Resolve ----

    [Test]
    public void Resolve_PicksTheKindsSet_AndAnUnknownKindFallsBackToDefault()
    {
        StepSetData data = Data(Set(CaseSteps.DefaultType, Step("rules", StepWhen.RulesViewed)), Set("Displaced", Step("papers", StepWhen.PapersReceived)));

        CollectionAssert.AreEqual(new[] { "papers" }, CaseSteps.Resolve(data, "Displaced", 1).Select(s => s.id));
        CollectionAssert.AreEqual(new[] { "rules" }, CaseSteps.Resolve(data, "Labourer", 1).Select(s => s.id), "no Labourer set: the default");
        CollectionAssert.IsEmpty(CaseSteps.Resolve(Data(Set("Displaced")), "Labourer", 1), "no default either: no steps");
        CollectionAssert.IsEmpty(CaseSteps.Resolve(null, "Displaced", 1));
    }

    [Test]
    public void Resolve_Inherit_StartsFromTheParent_ReplacesASameIdStepInPlace_AndAppendsNewOnes()
    {
        StepSet rich = Set("RichTourist", Step("rules", StepWhen.RulesViewed), Step("paperSet", StepWhen.PaperRead, forms: new[] { "TC-101", "TC-230" }),
                           Step("dress", StepWhen.LookedAt));
        StepSet poor = Set("PoorTourist", Step("paperSet", StepWhen.PaperRead, forms: new[] { "TC-101", "TC-230", "TC-310" }), Step("means", StepWhen.Requested));
        poor.inherit = "RichTourist";

        IReadOnlyList<StepSpec> steps = CaseSteps.Resolve(Data(rich, poor), "PoorTourist", 1);

        CollectionAssert.AreEqual(new[] { "rules", "paperSet", "dress", "means" }, steps.Select(s => s.id));
        CollectionAssert.AreEqual(new[] { "TC-101", "TC-230", "TC-310" }, steps[1].forms, "the child's step replaces the parent's");
        CollectionAssert.AreEqual(new[] { "rules", "paperSet", "dress" }, CaseSteps.Resolve(Data(rich, poor), "RichTourist", 1).Select(s => s.id), "the parent is unchanged");
    }

    [Test]
    public void Resolve_AnInheritCycle_Ends()
    {
        StepSet a = Set("RichTourist", Step("a", StepWhen.RulesViewed));
        StepSet b = Set("PoorTourist", Step("b", StepWhen.RulesViewed));
        a.inherit = "PoorTourist";
        b.inherit = "RichTourist";

        CollectionAssert.AreEquivalent(new[] { "a", "b" }, CaseSteps.Resolve(Data(a, b), "RichTourist", 1).Select(s => s.id));
    }

    [Test]
    public void Resolve_AStepBeforeItsFirstDay_IsLeftOut()
    {
        StepSetData data = Data(Set("Displaced", Step("rules", StepWhen.RulesViewed), Step("dates", StepWhen.PaperRead, fromDay: 4)));

        CollectionAssert.AreEqual(new[] { "rules" }, CaseSteps.Resolve(data, "Displaced", 3).Select(s => s.id));
        CollectionAssert.AreEqual(new[] { "rules", "dates" }, CaseSteps.Resolve(data, "Displaced", 4).Select(s => s.id));
    }

    // ---- Each StepWhen ----

    [Test]
    public void PapersReceived_CountsEachPaper_OrThoseOfItsForms()
    {
        CaseProgress p = Progress();
        StepSpec all = Step("papers", StepWhen.PapersReceived);
        StepSpec returnOrder = Step("order", StepWhen.PapersReceived, forms: new[] { "TC-630" });

        Assert.AreEqual((0, 3, false), Parts(State(all, p)));
        Assert.IsTrue(p.Received(0));
        Assert.IsFalse(p.Received(0), "a paper is received once");
        Assert.AreEqual((1, 3, false), Parts(State(all, p)));
        Assert.AreEqual((0, 1, false), Parts(State(returnOrder, p)));
        p.Received(1);
        p.Received(2);
        Assert.AreEqual((3, 3, true), Parts(State(all, p)));
        Assert.AreEqual((1, 1, true), Parts(State(returnOrder, p)));
    }

    [Test]
    public void AStepWithNoPartsInThisCase_IsNotListed()
    {
        CaseProgress p = Progress();
        StepSpec visa = Step("visa", StepWhen.PaperRead, forms: new[] { "TC-101" });

        CollectionAssert.IsEmpty(CaseSteps.Evaluate(new[] { visa }, p), "a displaced traveller has no visa");
    }

    [Test]
    public void PaperRead_CountsEachPaperRead_AtTheDeskOrInAPane()
    {
        CaseProgress p = Progress();
        StepSpec read = Step("read", StepWhen.PaperRead);

        p.Read(2);
        Assert.IsFalse(p.Read(2), "a paper is read once");
        Assert.AreEqual((1, 3, false), Parts(State(read, p)));
        Assert.IsFalse(p.Read(7), "no such paper");
        p.Read(0);
        p.Read(1);
        Assert.AreEqual((3, 3, true), Parts(State(read, p)));
    }

    [Test]
    public void Requested_WithForms_IsOneRequestGroup_AnyOfThemTicksIt()
    {
        CaseProgress p = new CaseProgress(new List<StepPaper>(), new ClueCategory[0], Books);
        StepSpec means = Step("askMeans", StepWhen.Requested, forms: new[] { "TC-415", "TC-416", "TC-417" });

        Assert.AreEqual((0, 1, false), Parts(State(means, p)), "listed although the case holds none of them: asking is the check, not what was handed over");
        p.Requested("TC-416");
        Assert.AreEqual((1, 1, true), Parts(State(means, p)));
    }

    [Test]
    public void Requested_WithoutForms_CountsEachPaperHandedOverOnRequest()
    {
        CaseProgress p = Progress();
        StepSpec ask = Step("ask", StepWhen.Requested);

        Assert.AreEqual((0, 2, false), Parts(State(ask, p)), "the certificate comes on arrival");
        Assert.IsTrue(p.Requested("TC-620"));
        Assert.IsFalse(p.Requested("TC-620"));
        Assert.AreEqual((1, 2, false), Parts(State(ask, p)));
        p.Requested("TC-630");
        Assert.AreEqual((2, 2, true), Parts(State(ask, p)));
    }

    [Test]
    public void RulesViewed_RecordViewed_AndLookedAt_AreOnePartEach()
    {
        CaseProgress p = Progress();
        StepSpec rules = Step("rules", StepWhen.RulesViewed);
        StepSpec record = Step("standing", StepWhen.RecordViewed);
        StepSpec look = Step("look", StepWhen.LookedAt);

        Assert.AreEqual((0, 1, false), Parts(State(rules, p)));
        Assert.IsTrue(p.RulesViewed());
        Assert.IsFalse(p.RulesViewed());
        Assert.AreEqual((1, 1, true), Parts(State(rules, p)));
        Assert.AreEqual((0, 1, false), Parts(State(record, p)));
        Assert.IsTrue(p.RecordViewed());
        Assert.AreEqual((1, 1, true), Parts(State(record, p)));
        Assert.IsTrue(p.LookedAt());
        Assert.AreEqual((1, 1, true), Parts(State(look, p)));
    }

    [Test]
    public void Compared_AMismatchTicksLikeAMatch()
    {
        StepSpec facts = Step("facts", StepWhen.Compared, StatementKind.Field, TruthKind.Reference, new[] { "Currency" });
        CaseProgress matched = Progress(), mismatched = Progress();

        Assert.IsTrue(matched.Compared(Field(ClueCategory.Currency, "deben"), Book(ClueCategory.Currency, "deben")));
        Assert.IsTrue(mismatched.Compared(Field(ClueCategory.Currency, "sestertius"), Book(ClueCategory.Currency, "deben")));

        Assert.AreEqual(State(facts, matched).Done, State(facts, mismatched).Done);
        Assert.IsTrue(State(facts, mismatched).Done, "the player made the check; what it found is not the checklist's business");
    }

    [Test]
    public void Compared_CountsOnlyAPairOfOneCategory_InEitherOrder()
    {
        CaseProgress p = Progress();
        StepSpec facts = Step("facts", StepWhen.Compared, StatementKind.Field, TruthKind.Reference);

        Assert.IsFalse(p.Compared(Field(ClueCategory.Currency, "deben"), Book(ClueCategory.Language, "Egyptian")), "a currency against the tongues book checks nothing");
        Assert.IsFalse(p.Compared(Book(ClueCategory.Currency, "deben"), Book(ClueCategory.Currency, "deben")), "two book rows are no check");
        Assert.IsFalse(p.Compared(default, Book(ClueCategory.Currency, "deben")), "a plain value carries no evidence");
        Assert.AreEqual((0, 3, false), Parts(State(facts, p)));
        Assert.IsTrue(p.Compared(Book(ClueCategory.Language, "Egyptian"), Field(ClueCategory.Language, "Egyptian")));
        Assert.IsFalse(p.Compared(Field(ClueCategory.Language, "Latin"), Book(ClueCategory.Language, "Egyptian")), "the same check again changes nothing");
        Assert.AreEqual((1, 3, false), Parts(State(facts, p)));
    }

    [Test]
    public void Compared_DerivedCategories_AreThePapersPlaceFactsThatHaveABook()
    {
        CaseProgress p = Progress();
        StepSpec facts = Step("facts", StepWhen.Compared, StatementKind.Field, TruthKind.Reference);

        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.Language, ClueCategory.Technology }, CaseSteps.Categories(facts, p));
    }

    [Test]
    public void Compared_TheStatementAndTruthMustBeTheSteps()
    {
        CaseProgress p = Progress();
        StepSpec identity = Step("identity", StepWhen.Compared, StatementKind.Field, TruthKind.Record, new[] { "Name", "CitizenId", "BirthDate" });

        p.Compared(Field(ClueCategory.BirthDate, "3 Mar 1480 BC"), Field(ClueCategory.BirthDate, "3 Mar 1480 BC"));
        p.Compared(CompareEvidence.ForAnswer(ClueCategory.BirthDate, "1480 BC", false), Record(ClueCategory.BirthDate, "3 Mar 1480 BC"));
        Assert.AreEqual((0, 3, false), Parts(State(identity, p)), "paper against paper, or an answer against the record, is not this step's check");

        p.Compared(Record(ClueCategory.BirthDate, "3 Mar 1480 BC"), Field(ClueCategory.BirthDate, "3 Mar 1480 BC"));
        p.Compared(Field(ClueCategory.CitizenId, "DP-4471-02"), Record(ClueCategory.CitizenId, "DP-9999-99"));
        Assert.AreEqual((2, 3, false), Parts(State(identity, p)));
    }

    [Test]
    public void Compared_TwoPapers_AreAPaperCheck()
    {
        CaseProgress p = Progress();
        StepSpec order = Step("returnOrder", StepWhen.Compared, StatementKind.Field, TruthKind.Paper, new[] { "Destination", "Incident" });

        Assert.IsTrue(p.Compared(Field(ClueCategory.Destination, "New Kingdom Egypt"), Field(ClueCategory.Destination, "Weimar Berlin")));
        Assert.AreEqual((1, 2, false), Parts(State(order, p)));
    }

    [Test]
    public void Compared_AnAnswerStep_CountsTodaysQuestionsAgainstAnyTruth()
    {
        CaseProgress p = Progress(ClueCategory.Currency, ClueCategory.BirthDate);
        StepSpec answers = Step("answers", StepWhen.Compared, StatementKind.Answer);

        CollectionAssert.AreEqual(new[] { ClueCategory.Currency, ClueCategory.BirthDate }, CaseSteps.Categories(answers, p));
        p.Compared(Record(ClueCategory.BirthDate, "3 Mar 1480 BC"), CompareEvidence.ForAnswer(ClueCategory.BirthDate, "1480 BC", true));
        Assert.AreEqual((1, 2, false), Parts(State(answers, p)), "a birth date answer's truth is the record");
    }

    [Test]
    public void Compared_TheDressStep_TicksOnAGarmentAgainstTheCostumeGuide()
    {
        CaseProgress p = Progress();
        StepSpec dress = Step("dress", StepWhen.Compared, StatementKind.Garment, TruthKind.Reference, new[] { "Culture" });

        Assert.AreEqual((0, 1, false), Parts(State(dress, p)));
        p.Compared(CompareEvidence.ForAppearance(ClueCategory.Culture, "tech jacket / coat-dress", true), Book(ClueCategory.Culture, "linen kilt / sheath dress"));
        Assert.AreEqual((1, 1, true), Parts(State(dress, p)));
    }

    [Test]
    public void Compared_ANamedCategoryTheCaseCannotState_OrAnUnknownName_IsNoPart()
    {
        CaseProgress p = Progress();
        StepSpec transponder = Step("transponder", StepWhen.Compared, StatementKind.Field, TruthKind.Record, new[] { "TransponderId", "Name", "Politics" });

        CollectionAssert.AreEqual(new[] { ClueCategory.Name }, CaseSteps.Categories(transponder, p),
                                  "a category a later phase appends matches nothing yet; none of the papers states a politics field");
    }

    [Test]
    public void Classify_NamesTheStatementAndTheTruth()
    {
        Assert.IsTrue(CaseSteps.Classify(Book(ClueCategory.Currency, "a"), Field(ClueCategory.Currency, "b"), out StatementKind s, out ClueCategory c, out TruthKind t));
        Assert.AreEqual((StatementKind.Field, ClueCategory.Currency, TruthKind.Reference), (s, c, t));
        Assert.IsTrue(CaseSteps.Classify(CompareEvidence.ForAppearance(ClueCategory.Culture, "a", false), Field(ClueCategory.Culture, "b"), out s, out c, out t));
        Assert.AreEqual((StatementKind.Garment, ClueCategory.Culture, TruthKind.Paper), (s, c, t), "a statement against a paper's field: the paper is the truth");
        Assert.IsFalse(CaseSteps.Classify(CompareEvidence.ForAnswer(ClueCategory.Currency, "a", false), CompareEvidence.ForAnswer(ClueCategory.Currency, "b", false), out _, out _, out _),
                       "two answers: no truth");
        Assert.IsFalse(CaseSteps.Classify(Record(ClueCategory.Name, "a"), Book(ClueCategory.Name, "a"), out _, out _, out _), "two truths: no statement");
    }

    [Test]
    public void Asked_CountsTodaysQuestionCategories_OrTheNamedOnesAmongThem()
    {
        CaseProgress p = Progress(ClueCategory.Currency, ClueCategory.Language, ClueCategory.BirthDate);
        StepSpec questions = Step("questions", StepWhen.Asked);
        StepSpec coin = Step("coin", StepWhen.Asked, categories: new[] { "Currency", "Politics" });

        Assert.AreEqual((0, 3, false), Parts(State(questions, p)));
        Assert.AreEqual((0, 1, false), Parts(State(coin, p)), "no politics question today");
        Assert.IsTrue(p.Asked(ClueCategory.Currency));
        Assert.IsFalse(p.Asked(ClueCategory.Currency));
        Assert.AreEqual((1, 3, false), Parts(State(questions, p)));
        Assert.AreEqual((1, 1, true), Parts(State(coin, p)));
    }

    [Test]
    public void AHandSetTick_HoldsForTheCase_EitherWay()
    {
        CaseProgress p = Progress();
        StepSpec rules = Step("rules", StepWhen.RulesViewed);

        p.SetManual("rules", true);
        StepState ticked = State(rules, p);
        Assert.IsTrue(ticked.Done && ticked.Manual && ticked.Have == 0, "ticked by hand before the check");

        p.SetManual("rules", false);
        p.RulesViewed();
        StepState unticked = State(rules, p);
        Assert.IsFalse(unticked.Done, "unticked by hand: the check made later does not tick it again");
        Assert.IsTrue(unticked.Manual && unticked.Have == 1);
    }

    // ---- Where a click goes ----

    [Test]
    public void Target_AHint_ATab_AndTheCostumeGuide()
    {
        CaseProgress p = Progress();
        StepSpec questions = Step("questions", StepWhen.Asked);
        questions.link = StepLink.None;
        questions.hint = "steps.hint.wheel";
        StepSpec dress = Step("dress", StepWhen.Compared, StatementKind.Garment, TruthKind.Reference, new[] { "Culture" });
        dress.link = StepLink.CostumeClaimed;

        StepTarget hint = CaseSteps.Target(questions, p);
        Assert.AreEqual(StepTargetKind.Hint, hint.Kind);
        Assert.AreEqual("steps.hint.wheel", hint.HintKey);
        StepTarget tab = CaseSteps.Target(Step("rules", StepWhen.RulesViewed), p);
        Assert.AreEqual((StepTargetKind.Tab, AppTab.Documents), (tab.Kind, tab.Tab));
        StepTarget guide = CaseSteps.Target(dress, p);
        Assert.AreEqual((StepTargetKind.Book, AppTab.Reference, ClueCategory.Culture), (guide.Kind, guide.Tab, guide.Book));
    }

    [Test]
    public void Target_PrimaryName_LooksThePrimaryPapersNumberUp_ElseItsName()
    {
        StepSpec identity = Step("identity", StepWhen.Compared, StatementKind.Field, TruthKind.Record, new[] { "Name" });
        identity.link = StepLink.PrimaryName;

        StepTarget byNumber = CaseSteps.Target(identity, Progress());
        Assert.AreEqual((StepTargetKind.Record, AppTab.Records, "DP-4471-02"), (byNumber.Kind, byNumber.Tab, byNumber.Query));

        var noNumber = new CaseProgress(new List<StepPaper> { new StepPaper("TC-999", false, false, new[] { F(ClueCategory.Name, "Aster Vale") }) }, new ClueCategory[0], Books);
        Assert.AreEqual("Aster Vale", CaseSteps.Target(identity, noNumber).Query, "no primary paper: the first; no number: the name");

        var none = new CaseProgress(new List<StepPaper>(), new ClueCategory[0], Books);
        Assert.AreEqual((StepTargetKind.Tab, AppTab.Records), (CaseSteps.Target(identity, none).Kind, CaseSteps.Target(identity, none).Tab));
    }

    [Test]
    public void Target_FirstUncheckedField_IsTheBookOfTheFirstFieldNotComparedYet()
    {
        CaseProgress p = Progress();
        StepSpec facts = Step("facts", StepWhen.Compared, StatementKind.Field, TruthKind.Reference);
        facts.link = StepLink.FirstUncheckedField;

        Assert.AreEqual((StepTargetKind.Book, ClueCategory.Currency), (CaseSteps.Target(facts, p).Kind, CaseSteps.Target(facts, p).Book));
        p.Compared(Field(ClueCategory.Currency, "deben"), Book(ClueCategory.Currency, "deben"));
        Assert.AreEqual(ClueCategory.Language, CaseSteps.Target(facts, p).Book);
        p.Compared(Field(ClueCategory.Language, "x"), Book(ClueCategory.Language, "y"));
        p.Compared(Field(ClueCategory.Technology, "x"), Book(ClueCategory.Technology, "y"));
        Assert.AreEqual(ClueCategory.Currency, CaseSteps.Target(facts, p).Book, "all checked: the first category's book again");

        StepSpec order = Step("returnOrder", StepWhen.Compared, StatementKind.Field, TruthKind.Paper, new[] { "Destination" });
        order.link = StepLink.FirstUncheckedField;
        Assert.AreEqual((StepTargetKind.Tab, AppTab.Documents), (CaseSteps.Target(order, p).Kind, CaseSteps.Target(order, p).Tab), "a paper check is done in Documents");
    }

    private static (int have, int need, bool done) Parts(StepState s) => (s.Have, s.Need, s.Done);
}
