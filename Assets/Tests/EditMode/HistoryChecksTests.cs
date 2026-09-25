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

    [Test]
    public void NullInputs_AreSafe()
    {
        CollectionAssert.IsEmpty(HistoryChecks.Problems(null, Base()));
        Assert.AreEqual(1, HistoryChecks.Problems(new[] { E("egypt", ClueCategory.Currency, "Sterling") }, null).Count, "no base world: the place is unknown");
    }
}
