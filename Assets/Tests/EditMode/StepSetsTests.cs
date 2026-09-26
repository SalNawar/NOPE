using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

/// <summary>
/// The steps checklist's content (the PC redesign ST3, §4.8; redesign phase
/// 21): world_source.json pc.steps read into the library's shape
/// (StepSets.Parse: the enum names, the jump), the rules Generate World and
/// the content validator share (StepSets.Problems), and today's authored
/// sets against them: the Displaced and RichTourist sets in play, the poor
/// tourists' and the labourers' data-only until their kinds are.
/// </summary>
public class StepSetsTests
{
    private const string SourcePath = "Assets/Data/World/world_source.json";

    private static readonly string[] Forms = { "TC-610", "TC-620", "TC-630" };

    /// <summary>The forms of the kinds in play today: the displaced's and the rich tourists'.</summary>
    private static readonly string[] FormsToday = { "TC-610", "TC-620", "TC-630", "TC-101", "TC-230" };

    private static StepSource Source(string id, string when, string link = "PrimaryName", string hint = null) => new StepSource
    {
        id = id,
        when = when,
        jump = hint == null ? new StepJumpSource { link = link } : new StepJumpSource(),
        hint = hint
    };

    private static StepSpec Spec(string id, StepWhen when) => new StepSpec { id = id, when = when, link = StepLink.Tab, tab = AppTab.Rules };

    private static StepSetData Data(params StepSet[] sets) => new StepSetData { sets = sets.ToList() };

    private static StepSet Set(string type, params StepSpec[] steps) => new StepSet { type = type, steps = steps.ToList() };

    private static HashSet<string> Keys(params string[] ids) =>
        new HashSet<string>(ids.Select(StepSets.LabelKey).Concat(StepSets.PanelKeys).Concat(new[] { "steps.hint.wheel" }));

    private static List<string> Problems(StepSetData data, params string[] ids) => StepSets.Problems(data, Keys(ids), Forms);

    // ---- Parse ----

    [Test]
    public void Parse_ReadsTheNamesAndTheJump()
    {
        var src = new StepsSource
        {
            sets = new[]
            {
                new StepSetSource
                {
                    type = "Displaced", inherit = "default", dataOnly = true,
                    steps = new[]
                    {
                        new StepSource { id = "identity", when = "Compared", categories = new[] { "Name" }, statement = "Field", truth = "Record", jump = new StepJumpSource { link = "PrimaryName" } },
                        new StepSource { id = "rules", when = "RulesViewed", jump = new StepJumpSource { source = "Rules" }, fromDay = 2 },
                        new StepSource { id = "questions", when = "Asked", hint = "steps.hint.wheel" },
                    }
                }
            }
        };
        var errors = new List<string>();

        StepSetData data = StepSets.Parse(src, errors);

        CollectionAssert.IsEmpty(errors);
        StepSet set = data.sets.Single();
        Assert.AreEqual(("Displaced", "default", true), (set.type, set.inherit, set.dataOnly));
        StepSpec identity = set.steps[0], rules = set.steps[1], questions = set.steps[2];
        Assert.AreEqual((StepWhen.Compared, StatementKind.Field, TruthKind.Record, StepLink.PrimaryName), (identity.when, identity.statement, identity.truth, identity.link));
        CollectionAssert.AreEqual(new[] { "Name" }, identity.categories);
        Assert.AreEqual((StepWhen.RulesViewed, StepLink.Tab, AppTab.Rules, 2), (rules.when, rules.link, rules.tab, rules.fromDay));
        Assert.AreEqual((StepLink.None, "steps.hint.wheel", StatementKind.Any, TruthKind.Any, 1), (questions.link, questions.hint, questions.statement, questions.truth, questions.fromDay));
    }

    [Test]
    public void Parse_NamesEachUnknownName()
    {
        var src = new StepsSource
        {
            sets = new[]
            {
                new StepSetSource
                {
                    type = "Displaced",
                    steps = new[]
                    {
                        new StepSource { id = "a", when = "Seen", jump = new StepJumpSource { source = "Rules" } },
                        new StepSource { id = "b", when = "Compared", statement = "Paper", truth = "Garment", jump = new StepJumpSource { source = "Inbox" } },
                        new StepSource { id = "c", when = "Asked", jump = new StepJumpSource { link = "Tab" } },
                        new StepSource { id = "d", when = "Asked", jump = new StepJumpSource { link = "Nowhere" } },
                        new StepSource { id = "e", when = "Asked", jump = new StepJumpSource { source = "Rules", link = "PrimaryName" } },
                    }
                }
            }
        };
        var errors = new List<string>();

        StepSets.Parse(src, errors);

        Assert.AreEqual(7, errors.Count, string.Join("\n", errors));
        StringAssert.Contains("'Seen'", errors[0]);
        Assert.IsTrue(errors.Any(e => e.Contains("statement 'Paper'")) && errors.Any(e => e.Contains("truth 'Garment'")) && errors.Any(e => e.Contains("'Inbox'")));
        Assert.IsTrue(errors.Any(e => e.Contains("'c'") && e.Contains("jump.source")), "a tab is named by jump.source");
        Assert.IsTrue(errors.Any(e => e.Contains("'Nowhere'")) && errors.Any(e => e.Contains("'e'") && e.Contains("both")));
    }

    // ---- Problems ----

    [Test]
    public void Problems_ASoundSetHasNone()
    {
        CollectionAssert.IsEmpty(Problems(Data(Set(CaseSteps.DefaultType, Spec("rules", StepWhen.RulesViewed))), "rules"));
    }

    [Test]
    public void Problems_TheSetsThemselves()
    {
        Assert.IsTrue(Problems(Data(Set("Displaced", Spec("rules", StepWhen.RulesViewed))), "rules").Single().Contains("no 'default' set"));
        StepSet unknown = Set("Tourist", Spec("rules", StepWhen.RulesViewed));
        StepSet twice = Set(CaseSteps.DefaultType, Spec("rules", StepWhen.RulesViewed));
        StepSet orphan = Set("Labourer", Spec("rules", StepWhen.RulesViewed));
        orphan.inherit = "Miner";
        StepSet dataOnlyDefault = Set(CaseSteps.DefaultType, Spec("rules", StepWhen.RulesViewed));
        dataOnlyDefault.dataOnly = true;

        List<string> p = Problems(Data(dataOnlyDefault, twice, unknown, orphan), "rules");

        Assert.IsTrue(p.Any(x => x.Contains("'Tourist'") && x.Contains("is not")), string.Join("\n", p));
        Assert.IsTrue(p.Any(x => x.Contains("'default'") && x.Contains("twice")));
        Assert.IsTrue(p.Any(x => x.Contains("'Miner'")));
        Assert.IsTrue(p.Any(x => x.Contains("'default'") && x.Contains("data-only")));
    }

    [Test]
    public void Problems_AnInheritCycle()
    {
        StepSet a = Set("RichTourist", Spec("a", StepWhen.RulesViewed));
        StepSet b = Set("PoorTourist", Spec("b", StepWhen.RulesViewed));
        a.inherit = "PoorTourist";
        b.inherit = "RichTourist";

        Assert.IsTrue(Problems(Data(Set(CaseSteps.DefaultType), a, b), "a", "b").Any(x => x.Contains("cycle")));
    }

    [Test]
    public void Problems_EachStep()
    {
        StepSpec blank = Spec(" ", StepWhen.RulesViewed);
        StepSpec dup1 = Spec("rules", StepWhen.RulesViewed), dup2 = Spec("rules", StepWhen.RulesViewed);
        StepSpec noLabel = Spec("unlabelled", StepWhen.RulesViewed);
        StepSpec categoriesOnRules = Spec("r2", StepWhen.RulesViewed);
        categoriesOnRules.categories.Add("Currency");
        StepSpec truthOnAsked = Spec("q", StepWhen.Asked);
        truthOnAsked.truth = TruthKind.Record;
        StepSpec formsOnCompared = Spec("f", StepWhen.Compared);
        formsOnCompared.truth = TruthKind.Reference;
        formsOnCompared.forms.Add("TC-610");
        StepSpec unknownForm = Spec("u", StepWhen.PaperRead);
        unknownForm.forms.Add("TC-101");
        StepSpec unknownCategory = Spec("c", StepWhen.Compared);
        unknownCategory.categories.Add("Horoscope");
        StepSpec derivedAgainstRecord = Spec("d", StepWhen.Compared);
        derivedAgainstRecord.truth = TruthKind.Record;
        StepSpec noJump = Spec("j", StepWhen.RulesViewed);
        noJump.link = StepLink.None;
        StepSpec both = Spec("b", StepWhen.RulesViewed);
        both.hint = "steps.hint.wheel";
        StepSpec unknownHint = Spec("h", StepWhen.RulesViewed);
        unknownHint.link = StepLink.None;
        unknownHint.hint = "steps.hint.nope";
        StepSpec day0 = Spec("z", StepWhen.RulesViewed);
        day0.fromDay = 0;

        List<string> p = Problems(Data(Set(CaseSteps.DefaultType, blank, dup1, dup2, noLabel, categoriesOnRules, truthOnAsked, formsOnCompared, unknownForm,
                                           unknownCategory, derivedAgainstRecord, noJump, both, unknownHint, day0)),
                                  "rules", "r2", "q", "f", "u", "c", "d", "j", "b", "h", "z");

        string all = string.Join("\n", p);
        Assert.IsTrue(p.Any(x => x.Contains("blank id")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'rules'") && x.Contains("twice")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'steps.unlabelled'")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'r2'") && x.Contains("categories")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'q'") && x.Contains("statement or truth")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'f'") && x.Contains("forms")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'u'") && x.Contains("'TC-101'")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'c'") && x.Contains("'Horoscope'")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'d'") && x.Contains("names its categories")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'j'") && x.Contains("no jump")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'b'") && x.Contains("both")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'steps.hint.nope'")), all);
        Assert.IsTrue(p.Any(x => x.Contains("'z'") && x.Contains("day")), all);
        Assert.AreEqual(13, p.Count, all);
    }

    [Test]
    public void Problems_ADataOnlySet_MayNameFormsAndCategoriesLaterPhasesAdd()
    {
        StepSpec transponder = Spec("transponder", StepWhen.Compared);
        transponder.categories.AddRange(new[] { "TransponderId", "Horoscope" });
        transponder.truth = TruthKind.Record;
        StepSpec paperSet = Spec("paperSet", StepWhen.PaperRead);
        paperSet.forms.AddRange(new[] { "TC-101", "TC-230" });
        StepSet rich = Set("RichTourist", transponder, paperSet);
        rich.dataOnly = true;

        CollectionAssert.IsEmpty(Problems(Data(Set(CaseSteps.DefaultType), rich), "transponder", "paperSet"));
        CollectionAssert.AreEqual(new[] { "RichTourist" }, StepSets.DataOnlyInPlay(Data(rich), new[] { TravellerKind.RichTourist, TravellerKind.Displaced }));
        CollectionAssert.IsEmpty(StepSets.DataOnlyInPlay(Data(rich), new[] { TravellerKind.Displaced }));
    }

    [Test]
    public void Problems_ThePanelsOwnKeys()
    {
        List<string> p = StepSets.Problems(Data(Set(CaseSteps.DefaultType, Spec("rules", StepWhen.RulesViewed))), new HashSet<string> { "steps.rules" }, Forms);

        CollectionAssert.AreEquivalent(StepSets.PanelKeys.Select(k => $"the UI strings have no '{k}', which the steps panel writes with"), p);
    }

    // ---- Today's content ----

    [Test]
    public void TodaysSets_ParseAndPassTheRules_TheDisplacedAndRichTouristsInPlay_TheOthersDataOnly()
    {
        ContentNode root = ContentJson.Parse(SourceText());
        var errors = new List<string>();
        StepSetData data = StepSets.Parse(StepsOf(root), errors);
        var keys = new HashSet<string>(root.Get("ui").Get("strings").Items.Select(s => s.Get("key").Text));

        CollectionAssert.IsEmpty(errors);
        CollectionAssert.IsEmpty(StepSets.Problems(data, keys, FormsToday));
        CollectionAssert.AreEqual(new[] { CaseSteps.DefaultType, "Displaced", "RichTourist", "PoorTourist", "Labourer" }, data.sets.Select(s => s.type));
        CollectionAssert.AreEqual(new[] { false, false, false, true, true }, data.sets.Select(s => s.dataOnly));

        CollectionAssert.AreEqual(new[] { "rules", "papers", "read", "identity", "facts", "questions", "answers", "dress", "returnOrder" },
                                  CaseSteps.Resolve(data, "Displaced", 1).Select(s => s.id));
        Assert.AreEqual("dates", CaseSteps.Resolve(data, "Displaced", 4).Last().id, "the dates directive's step from day 4");
        CollectionAssert.AreEqual(new[] { "rules", "papers", "read", "identity", "class", "transponder" }, CaseSteps.Resolve(data, "RichTourist", 1).Select(s => s.id),
                                  "a rich tourist's day-1 steps (the others start with their directives)");
        Assert.AreEqual("RichTourist", data.sets.Single(s => s.type == "PoorTourist").inherit);
        CollectionAssert.IsSubsetOf(CaseSteps.Resolve(data, "RichTourist", 6).Select(s => s.id), CaseSteps.Resolve(data, "PoorTourist", 6).Select(s => s.id),
                                    "the poor tourist's set inherits the rich tourist's");
    }

    [Test]
    public void TodaysDisplacedSet_OnARealCase_ListsEveryStep()
    {
        StepSetData data = StepSets.Parse(StepsOf(ContentJson.Parse(SourceText())), new List<string>());
        var papers = new List<StepPaper>
        {
            new StepPaper("TC-610", false, true, new[] { new DocumentField { category = ClueCategory.Name, value = "Iset" }, new DocumentField { category = ClueCategory.CitizenId, value = "DP-1" },
                                                         new DocumentField { category = ClueCategory.BirthDate, value = "1480 BC" }, new DocumentField { category = ClueCategory.Destination, value = "Egypt" },
                                                         new DocumentField { category = ClueCategory.Incident, value = "R-1" } }),
            new StepPaper("TC-620", true, false, new[] { new DocumentField { category = ClueCategory.Currency, value = "deben" } }),
            new StepPaper("TC-630", true, false, new[] { new DocumentField { category = ClueCategory.Destination, value = "Egypt" }, new DocumentField { category = ClueCategory.Incident, value = "R-1" } }),
        };
        var progress = new CaseProgress(papers, new[] { ClueCategory.Currency, ClueCategory.BirthDate }, new[] { ClueCategory.Currency, ClueCategory.Culture });

        List<StepState> states = CaseSteps.Evaluate(CaseSteps.Resolve(data, "Displaced", 5), progress);

        CollectionAssert.AreEqual(new[] { "rules", "papers", "read", "identity", "facts", "questions", "answers", "dress", "returnOrder", "dates" }, states.Select(s => s.Id));
        Assert.IsTrue(states.All(s => !s.Done), "nothing is ticked before the player does anything");
    }

    // ---- The source ----

    /// <summary>world_source.json's pc.steps as JsonUtility reads it in Generate World.</summary>
    private static StepsSource StepsOf(ContentNode root)
    {
        string[] Strings(ContentNode n) => n?.Items.Select(i => i.Text).ToArray();
        string Text(ContentNode n, string key) => n.Get(key)?.Text;
        return new StepsSource
        {
            sets = root.Get("pc").Get("steps").Get("sets").Items.Select(s => new StepSetSource
            {
                type = Text(s, "type"),
                inherit = Text(s, "inherit"),
                dataOnly = Text(s, "dataOnly") == "true",
                steps = s.Get("steps").Items.Select(x => new StepSource
                {
                    id = Text(x, "id"),
                    when = Text(x, "when"),
                    categories = Strings(x.Get("categories")),
                    statement = Text(x, "statement"),
                    truth = Text(x, "truth"),
                    forms = Strings(x.Get("forms")),
                    jump = x.Get("jump") == null ? new StepJumpSource() : new StepJumpSource { source = Text(x.Get("jump"), "source"), link = Text(x.Get("jump"), "link") },
                    hint = Text(x, "hint"),
                    fromDay = x.Get("fromDay") != null ? int.Parse(x.Get("fromDay").Text) : 1
                }).ToArray()
            }).ToArray()
        };
    }

    private static string SourceText([CallerFilePath] string here = "")
    {
        string path = File.Exists(SourcePath) ? SourcePath : Path.Combine(Path.GetDirectoryName(here), "..", "..", "..", SourcePath);
        return File.ReadAllText(path).Replace("\r\n", "\n");
    }
}
