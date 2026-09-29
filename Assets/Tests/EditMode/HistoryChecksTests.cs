using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// Authored history values keep every book value unique (piece-2 R13):
/// one problem per broken rule. Base world: Egypt and Greece (Ancient) with
/// Currency and Technology.
/// </summary>
public class HistoryChecksTests
{
    private static FactTable Base()
    {
        var t = new FactTable();
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Currency, "Deben");
        t.Add("egypt", "ancient", "New Kingdom Egypt (Ancient)", ClueCategory.Technology, "Papyrus");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Currency, "Drachma");
        t.Add("greece", "ancient", "Periclean Athens (Ancient)", ClueCategory.Technology, "Klepsydra");
        return t;
    }

    private static FactEdit E(string nation, ClueCategory category, string value, string source = "rule") =>
        new FactEdit(nation, "ancient", category, value, 2, EditCause.Rule, source);

    private static List<string> Problems(params FactEdit[] edits) => HistoryChecks.Problems(edits, Base());

    [Test]
    public void ACleanSet_HasNoProblems()
    {
        CollectionAssert.IsEmpty(Problems(E("egypt", ClueCategory.Currency, "Sterling"), E("greece", ClueCategory.Technology, "Steam engine")));
    }

    [Test]
    public void TwoEditsForTheSamePlace_MayShareAValue()
    {
        CollectionAssert.IsEmpty(Problems(E("egypt", ClueCategory.Currency, "Sterling", "a"), E("egypt", ClueCategory.Currency, "Sterling", "b")));
    }

    [Test]
    public void OneProblemPerCase_NamingTheSource()
    {
        void One(FactEdit edit, string what, params FactEdit[] others)
        {
            var all = new List<FactEdit>(others) { edit };
            List<string> problems = HistoryChecks.Problems(all, Base());
            Assert.AreEqual(1, problems.Count, $"{what}: [{string.Join(" | ", problems)}]");
            StringAssert.Contains("'bad'", problems[0], what);
        }

        One(E("egypt", ClueCategory.Currency, " ", "bad"), "blank");
        One(E("egypt", ClueCategory.Currency, new string('x', 29), "bad"), "29 characters");
        One(E("egypt", ClueCategory.Culture, "Linen", "bad"), "not editable");
        One(new FactEdit("atlantis", "ancient", ClueCategory.Currency, "Orichalcum", 2, EditCause.Rule, "bad"), "unknown place");
        One(E("egypt", ClueCategory.Currency, "deben ", "bad"), "equal to its own base");
        One(E("egypt", ClueCategory.Currency, "Drachma", "bad"), "equal to another place's base");
    }

    [Test]
    public void TwoPlacesGivenOneValue_AreBothReported()
    {
        List<string> problems = Problems(E("greece", ClueCategory.Currency, "Sterling", "first"), E("egypt", ClueCategory.Currency, "Sterling", "second"));
        Assert.AreEqual(2, problems.Count, string.Join(" | ", problems));
        StringAssert.Contains("'first'", problems[0]);
        StringAssert.Contains("'second'", problems[1]);
    }

    [Test]
    public void A28CharacterValue_Fits()
    {
        CollectionAssert.IsEmpty(Problems(E("egypt", ClueCategory.Currency, new string('x', 28))));
    }

    private static readonly string[] Premades = { "pell", "rook", "auditor" };

    [Test]
    public void RuleProblems_ARuleWithNoEditAndANewsLineIsValid()
    {
        CollectionAssert.IsEmpty(HistoryChecks.RuleProblems("drive_begins", 0, true, 1, new string[0], 0f, Premades));
        CollectionAssert.IsEmpty(HistoryChecks.RuleProblems("pell_departed", 0, true, 1, new[] { FlagKeys.PremadeVerdict("pell", true) }, 0f, Premades));
        CollectionAssert.IsEmpty(HistoryChecks.RuleProblems("gutenberg_press", 1, true, 1, new string[0], 0f, Premades), "a history rule proper");
    }

    [Test]
    public void RuleProblems_ARuleWithNoEditNeedsANewsLine()
    {
        List<string> problems = HistoryChecks.RuleProblems("silent", 0, false, 1, new string[0], 0f, Premades);
        Assert.AreEqual(1, problems.Count);
        StringAssert.Contains("'silent'", problems[0]);
        StringAssert.Contains("news", problems[0]);
        CollectionAssert.IsEmpty(HistoryChecks.RuleProblems("edit_only", 1, false, 1, new string[0], 0f, Premades), "an edit changes the books even without a line");
    }

    [Test]
    public void RuleProblems_ARuleWithNoEditNeedsACondition()
    {
        List<string> problems = HistoryChecks.RuleProblems("first_night", 0, true, 0, new string[0], 0f, Premades);
        Assert.AreEqual(1, problems.Count);
        StringAssert.Contains("condition", problems[0]);
        Assert.AreEqual(1, HistoryChecks.RuleProblems("edit_first_night", 1, true, 0, new string[0], 0f, Premades).Count, "every rule needs a condition");
    }

    [Test]
    public void RuleProblems_AVerdictFlagNamesAKnownPremade()
    {
        CollectionAssert.IsEmpty(HistoryChecks.RuleProblems("audit_rook", 0, true, 3, new[] { "bribe:rook:taken", FlagKeys.PremadeVerdict("rook", true) }, -3f, Premades), "a story flag that is no premade's is not read");
        List<string> problems = HistoryChecks.RuleProblems("typo", 0, true, 1, new[] { FlagKeys.PremadeVerdict("rooke", true) }, 0f, Premades);
        Assert.AreEqual(1, problems.Count);
        StringAssert.Contains("'rooke'", problems[0]);
        Assert.AreEqual(1, HistoryChecks.RuleProblems("met", 0, true, 1, new[] { FlagKeys.PremadeMet("nobody") }, 0f, Premades).Count);
    }

    [TestCase(-100f, 0)]
    [TestCase(100f, 0)]
    [TestCase(-3f, 0)]
    [TestCase(-100.5f, 1)]
    [TestCase(250f, 1)]
    public void RuleProblems_StabilityWithinAHundred(float stability, int expected)
    {
        Assert.AreEqual(expected, HistoryChecks.RuleProblems("rook_complaint", 0, true, 2, new string[0], stability, Premades).Count);
    }

    /// <summary>A Return rule (days 7-15 Q9) is never printed: its consequence reaches the player only through an appearance that reads its fired flag.</summary>
    [Test]
    public void ReturnProblems_AReturnRuleNoAppearanceReads_IsAWarning()
    {
        var rules = new[] { ("pell_turned_away", StorySection.Return), ("desk4_closes", StorySection.Desk), ("drive_begins", StorySection.News) };
        CollectionAssert.IsEmpty(HistoryChecks.ReturnProblems(rules, new[] { FlagKeys.HistoryRuleFired("pell_turned_away"), "premade:pell:accepted" }));
        List<string> warnings = HistoryChecks.ReturnProblems(rules, new[] { "premade:pell:denied" });
        Assert.AreEqual(1, warnings.Count);
        StringAssert.Contains("'pell_turned_away'", warnings[0]);
        CollectionAssert.IsEmpty(HistoryChecks.ReturnProblems(null, null));
    }

    [Test]
    public void NullInputs_AreSafe()
    {
        CollectionAssert.IsEmpty(HistoryChecks.Problems(null, Base()));
        Assert.AreEqual(1, HistoryChecks.Problems(new[] { E("egypt", ClueCategory.Currency, "Sterling") }, null).Count, "no base world: the place is unknown");
    }
}
